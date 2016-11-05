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
    public class Server : Node, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when the server is successfully started.
        /// </summary>
        public event Action<Server> OnStarted;

        /// <summary>
        /// Triggered when the server is stopped.
        /// </summary>
        public event Action<Server> OnStopped;

        /// <summary>
        /// Triggered when the master set of operations is synchronized with the
        /// internal file buffer and flushed to disk.
        /// </summary>
        public event Action<Server> OnFileSynchronized;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Path to the local file this server is resposible for editing.
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
        }
        private volatile string filePath;

        /// <summary>
        /// Port to listen on.
        /// </summary>
        public ushort ListenPort
        {
            get { return listenPort; }
        }
        private volatile ushort listenPort;

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
        public bool IsDisposed
        {
            get { return isDisposed; }
        }
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
        public bool Running
        {
            get { return running; }
        }
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

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX server.
        /// </summary>
        public Server()
        {
            //
        }

        /////////////////////////////////////////////////////////////////////
        // STARTING THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Starts the server. Does nothing if the server is already running.
        /// </summary>
        /// <param name="path">Path to the text file the server will edit/create.</param>
        /// <param name="port">Port to listen on. Must be between 1024 and 65535.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InternalException" />
        public void Start(string path, ushort port = 55555)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Server");

            if (!running)
            {
                lock (runningLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Server");

                    if (!running)
                    {
                        if ((path = (path ?? "").Trim()).Length == 0)
                            throw new ArgumentException("Path cannot be empty");
                        filePath = path;

                        if (port < 1024)
                            throw new ArgumentOutOfRangeException("Port must be between 1024 and 65535");
                        listenPort = port;

                        running = true;
                        try
                        {
                            InternalException.Rethrow(() =>
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
                            running = false;
                            ClearRunningState();
                            throw;
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
                                        bool handshaken = false;
                                        while (running)
                                        {
                                            //check if data is waiting to be read
                                            if (!stream.DataAvailable)
                                            {
                                                Thread.Sleep(1);
                                                continue;
                                            }

                                            //read packet in from client
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

                                            //is this the first packet from a new client?
                                            if (!handshaken)
                                            {
                                                //deserialize guid
                                                Guid guid = Guid.Empty;
                                                if (CaptureException(() => { guid = data.Deserialize<Guid>(); })
                                                    || guid.CompareTo(Guid.Empty) == 0)
                                                    break;

                                                //perform initial synchronization
                                                lock (operationsLock)
                                                {
                                                    //create a list of operations containing all ops from the master
                                                    OperationRequest operations = new OperationRequest(Guid.Empty, masterOperations);

                                                    //send operations
                                                    var operationsBytes = operations.Serialize();
                                                    if (CaptureException(() => { stream.Write(operationsBytes, 0, operationsBytes.Length); }))
                                                        break;

                                                    //create the list of staged operations for this client
                                                    outgoingOperations[guid] = new List<Operation>();
                                                }

                                                handshaken = true;
                                            }
                                            else //initial handshake sync has been performed, handle normal requests
                                            {
                                                //deserialize operation request
                                                OperationRequest incoming = null;
                                                if (CaptureException(() => { incoming = data.Deserialize<OperationRequest>(); }))
                                                    break;

                                                //lock operation lists (3a)
                                                lock (operationsLock)
                                                {
                                                    //get the list of staged operations for the sender
                                                    List<Operation> outgoing = outgoingOperations[incoming.Sender];

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
                                                    if (CaptureException(() => { stream.Write(responseBytes, 0, responseBytes.Length); }))
                                                        break;
                                                }
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
                                        CaptureException(() => { SyncFileContents(); });
                                    }
                                }
                            }

                            //wait for client threads to close
                            foreach (Thread thread in clientThreads)
                                thread.Join();

                            //final flush to disk (don't need a lock this time, all client threads have stopped)
                            CaptureException(() => { SyncFileContents(); });
                        });
                        thread.IsBackground = false;
                        thread.Start();

                        OnStarted?.Invoke(this);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // SYNC OPERATIONS TO FILE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Applies the output of all new operations to the internal file contents string, then
        /// writes the new contents to disk.
        /// </summary>
        private void SyncFileContents()
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

            OnFileSynchronized?.Invoke(this);
        }

        /////////////////////////////////////////////////////////////////////
        // STOPPING THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears internal running state.
        /// </summary>
        private void ClearRunningState()
        {
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
            fileSyncIndex = 0;
        }

        /// <summary>
        /// Stops the server. 
        /// </summary>
        public void Stop()
        {
            if (running)
            {
                lock (runningLock)
                {
                    if (running)
                    {
                        running = false;
                        ClearRunningState();
                        OnStopped?.Invoke(this);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes this server, stopping it (if it was running) and releasing resources.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            Stop();
        }
    }
}
