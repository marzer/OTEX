using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Marzersoft;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace OTEX
{
    /// <summary>
    /// Server class for the OTEX framework.
    /// </summary>
    public class OTEXServer : IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Path to the local file this server is resposible for editing.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// Port to listen on.
        /// </summary>
        public readonly ushort ListenPort;

        /// <summary>
        /// Master list of operations, used for synchronizing newly-connected clients.
        /// </summary>
        private readonly List<Operation> masterOperations = new List<Operation>();

        /// <summary>
        /// Staging lists for storing outgoing operations.
        /// </summary>
        private readonly Dictionary<Guid, List<Operation>> outgoingOperations = new Dictionary<Guid,List<Operation>>();

        /// <summary>
        /// Lock object for all operations lists.
        /// </summary>
        private readonly object operationsLock = new object();

        /// <summary>
        /// has this server been disposed?
        /// </summary>
        private volatile bool isDisposed = false;

        /// <summary>
        /// TCP listener for dispatching clients.
        /// </summary>
        private TcpListener listener = null;

        /// <summary>
        /// Master thread for listening for new clients and controlling sub-threads.
        /// </summary>
        private Thread thread = null;

        /// <summary>
        /// Is the server currently running?
        /// </summary>
        private volatile bool running = false;

        /// <summary>
        /// Lock object for running state.
        /// </summary>
        private readonly object runningLock = new object();
        
        /// <summary>
        /// Contents of the file at last sync.
        /// </summary>
        private volatile string fileContents = "";

        /// <summary>
        /// Starting operation index for next sync.
        /// </summary>
        private volatile int fileSyncIndex = 0;

        /// <summary>
        /// Container for storing exceptions thrown on threads.
        /// </summary>
        private readonly List<OTEXInternalException> threadExceptions = new List<OTEXInternalException>();

        /// <summary>
        /// Lock object for thread exceptions.
        /// </summary>
        private readonly object threadExceptionsLock = new object();

        /// <summary>
        /// Gets the collection of exceptions thrown on child threads. Accessing this property clears the
        /// internal exception cache, so check it periodically in a loop on your main thread.
        /// </summary>
        public List<OTEXInternalException> ThreadExceptions
        {
            get
            {
                List<OTEXInternalException> output = null;
                lock (threadExceptionsLock)
                {
                    output = new List<OTEXInternalException>(threadExceptions);
                    threadExceptions.Clear();
                }
                return output;
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX server.
        /// </summary>
        /// <param name="path">Path to the text file the server will edit/create.</param>
        /// <param name="port">Port to listen on. Must be between 1024 and 65535.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public OTEXServer(string path, ushort port = 55555)
        {
            if ((path = (path ?? "").Trim()).Length == 0)
                throw new ArgumentException("Path cannot be empty");
            FilePath = path;

            if (port < 1024)
                throw new ArgumentOutOfRangeException("Port must be between 1024 and 65535");
            ListenPort = port;
        }

        /////////////////////////////////////////////////////////////////////
        // STARTING THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Starts the server. Does nothing if the server is already running.
        /// Throws OTEXInternalException if an internal exception is thrown during initialization;
        /// view the InnerException property of the exception to learn more about the actual cause of the problem.
        /// </summary>
        /// <exception cref="OTEXInternalException"></exception>
        public void Start()
        {
            if (!running && !isDisposed)
            {
                lock (runningLock)
                {
                    if (!running && !isDisposed)
                    {
                        running = true;
                        try
                        {
                            OTEXInternalException.Rethrow(() =>
                            {

                                //read file
                                if (File.Exists(FilePath))
                                {
                                    fileContents = File.ReadAllText(FilePath, FilePath.DetectEncoding());
                                    for (int i = 0; i < fileContents.Length; i += 2048, ++fileSyncIndex)
                                        masterOperations.Add(new Operation(Guid.Empty, i, fileContents.Substring(i, Math.Min(2048, fileContents.Length - i))));
                                }

                                //create tcplistener
                                listener = new TcpListener(IPAddress.Any, ListenPort);
                                listener.Start();
                            });
                        }
                        catch (Exception)
                        {
                            StopInternal();
                            throw;
                        }

                        //clear thread exceptions
                        lock (threadExceptionsLock)
                        {
                            threadExceptions.Clear();
                        }

                        //create master thread
                        thread = new Thread(() =>
                        {
                            List<Thread> clientThreads = new List<Thread>();
                            int flushTimeout = 60000;
                            while (running)
                            {
                                //listen for new connections
                                while (listener.Pending())
                                {
                                    //create client thread
                                    clientThreads.Add(new Thread((cl) =>
                                    {
                                        TcpClient client = cl as TcpClient;

                                        //get network stream
                                        NetworkStream stream = null;
                                        if (CaptureException(() => { stream = client.GetStream(); }))
                                        {
                                            client.Close();
                                            return;
                                        }

                                        //read request blocks
                                        byte[] buffer = new byte[65535];
                                        while (running)
                                        {
                                            //check if data is waiting to be read
                                            if (!stream.DataAvailable)
                                            {
                                                Thread.Sleep(1);
                                                continue;
                                            }

                                            //read request packet in from client
                                            byte[] data = null;
                                            if (CaptureException(() =>
                                            {
                                                using (MemoryStream ms = new MemoryStream())
                                                {
                                                    while (true)
                                                    {
                                                        int read = stream.Read(buffer, 0, buffer.Length);
                                                        if (read <= 0)
                                                        {
                                                            data = ms.ToArray();
                                                            break;
                                                        }
                                                        ms.Write(buffer, 0, read);
                                                    }
                                                }
                                            }))
                                                break;

                                            //deserialize operation request
                                            OperationRequest incoming = null;
                                            if (CaptureException(() => { incoming = data.Deserialize<OperationRequest>(); }))
                                                break;

                                            //lock operation lists (3a)
                                            lock (operationsLock)
                                            {
                                                //get the list of staged operations for the sender
                                                List<Operation> outgoing = null;
                                                if (!outgoingOperations.TryGetValue(incoming.Sender, out outgoing))
                                                    outgoingOperations[incoming.Sender] = outgoing = new List<Operation>();

                                                //if this oplist is not an empty request
                                                if (incoming.Operations != null && incoming.Operations.Count > 0)
                                                {
                                                    //transform incoming ops against outgoing ops (3b)
                                                    if (outgoing.Count > 0)
                                                        Operation.SymmetricLinearTransform(incoming.Operations, outgoing);

                                                    //append incoming ops to master and to all other outgoing (3c)
                                                    masterOperations.AddRange(incoming.Operations);
                                                    foreach (var kvp in outgoingOperations)
                                                        if (kvp.Key.CompareTo(incoming.Sender) != 0)
                                                            kvp.Value.AddRange(incoming.Operations);
                                                }

                                                //move all outgoing into our response packet (3d)
                                                OperationRequest response = new OperationRequest(Guid.Empty,
                                                    outgoing.Count > 0 ? new List<Operation>(outgoing) : null);
                                                outgoing.Clear();

                                                //send response packet
                                                var responseBytes = response.Serialize();
                                                if (CaptureException(() => { stream.Write(responseBytes, 0, responseBytes.Length); } ))
                                                    break;
                                            }
                                        }

                                        stream.Dispose();
                                        client.Close();
                                    }));
                                    clientThreads.Last().IsBackground = false;
                                    clientThreads.Last().Start(listener.AcceptTcpClient());
                                }
                                
                                //sleep a bit (not raw spin-waiting)
                                Thread.Sleep(100);

                                //flush file contents to disk periodically
                                if ((flushTimeout -= 100) <= 0)
                                {
                                    flushTimeout = 60000;
                                    lock (operationsLock)
                                    {
                                        CaptureException(() => { SyncOperations(); });
                                    }
                                }
                            }

                            //wait for client threads to close
                            foreach (Thread thread in clientThreads)
                                thread.Join();

                            //final flush to disk (don't need a lock this time, all client threads have stopped)
                            CaptureException(() => { SyncOperations(); });
                        });
                        thread.IsBackground = false;
                        thread.Start();
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // SYNC OPERATIONS TO FILE
        /////////////////////////////////////////////////////////////////////

        private void SyncOperations()
        {
            //flush pending operations to the file contents
            while (fileSyncIndex < masterOperations.Count)
            {
                if (!masterOperations[fileSyncIndex].IsNoop)
                    fileContents = masterOperations[fileSyncIndex].Execute(fileContents);
                ++fileSyncIndex;
            }

            //write contents to disk
            File.WriteAllText(FilePath, fileContents);
        }

        /////////////////////////////////////////////////////////////////////
        // STOPPING THE SERVER
        /////////////////////////////////////////////////////////////////////

        private void StopInternal()
        {
            running = false;

            //stop listening for new connections
            if (thread != null)
            {
                thread.Join();
                thread = null;
            }
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }

            //clear lists of operations (should have been flushed to disk by thread anyways)
            outgoingOperations.Clear();
            masterOperations.Clear();
            fileContents = "";
        }

        public void Stop()
        {
            if (running)
            {
                lock (runningLock)
                {
                    if (running)
                        StopInternal();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            Stop();
            lock (threadExceptionsLock)
            {
                threadExceptions.Clear();
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CAPTURING THREAD EXCEPTIONS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Capture thrown exceptions and store them in the threadexceptions collection.
        /// Returns true if an exception was caught.
        /// </summary>
        private bool CaptureException(Action func)
        {
            OTEXInternalException exception = OTEXInternalException.Capture(func);
            if (exception != null)
            {
                lock (threadExceptionsLock)
                {
                    threadExceptions.Add(exception);
                }
                return true;
            }
            return false;
        }
    }
}
