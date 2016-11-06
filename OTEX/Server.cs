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
        public ushort Port
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
        private readonly HashSet<Guid> connectedClients = new HashSet<Guid>();

        /// <summary>
        /// Lock object for connected clients collection.
        /// </summary>
        private readonly object connectedClientsLock = new object();

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

        /// <summary>
        /// Password required by the current session, if any.
        /// </summary>
        private Password password;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX server.
        /// </summary>
        public Server() : base(Guid.Empty) //server id is always 0
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
        /// <param name="password">Password required to connect to this server. Leave as null for none.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="IOException" />
        public void Start(string path, ushort port = 55555, Password password = null)
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
                        //session state
                        if ((path = (path ?? "").Trim()).Length == 0)
                            throw new ArgumentException("Path cannot be empty");
                        filePath = path;
                        if (port < 1024)
                            throw new ArgumentOutOfRangeException("Port must be between 1024 and 65535");
                        listenPort = port;
                        this.password = password;

                        //session initialization
                        TcpListener listener = null;
                        try
                        {
                            //read file
                            if (File.Exists(FilePath))
                                masterOperations.Add(new Operation(Guid.Empty, 0, fileContents = File.ReadAllText(FilePath, FilePath.DetectEncoding())));

                            //create tcplistener
                            listener = new TcpListener(IPAddress.Any, Port);
                            listener.Start();
                        }
                        catch (Exception)
                        {
                            ClearRunningState();
                            throw;
                        }
                        running = true;

                        //create thread
                        thread = new Thread((list) =>
                        {
                            List<Thread> clientThreads = new List<Thread>();
                            TcpListener tcpListener = list as TcpListener;
                            int flushTimeout = 60000;
                            while (running)
                            {
                                //listen for new connections
                                while (tcpListener.Pending())
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

                                        //read from stream
                                        Guid clientGUID = Guid.Empty;
                                        while (running)
                                        {
                                            //check if data is waiting to be read
                                            if (!stream.DataAvailable)
                                            {
                                                Thread.Sleep(1);
                                                continue;
                                            }

                                            //read incoming packet sequence
                                            PacketSequence packetSequence = null;
                                            if (CaptureException(() => { packetSequence = new PacketSequence(stream); }))
                                                break;

                                            //check guid
                                            if (packetSequence.Sender.Equals(Guid.Empty))
                                                break; //do not accept packets from other servers

                                            //is this the first packet from a new client?
                                            if (clientGUID.Equals(Guid.Empty))
                                            {
                                                //check if packet is a request type
                                                if (packetSequence.PayloadType != ConnectionRequest.PayloadType)
                                                    continue; //ignore

                                                //deserialize packet
                                                ConnectionRequest request = null;
                                                if (CaptureException(() => { request = packetSequence.Payload.Deserialize<ConnectionRequest>(); }))
                                                    break;

                                                //check password
                                                if ((password != null && (request.Password == null || !password.Matches(password))) //requires password
                                                    || (password == null && request.Password != null)) //no password required (reject incoming requests with passwords)
                                                {
                                                    CaptureException(() =>
                                                    {
                                                        PacketSequence.Send(stream, GUID,
                                                            new ConnectionResponse(ConnectionResponse.ResponseCode.IncorrectPassword));
                                                    });
                                                    break;
                                                }

                                                //check client id
                                                lock (connectedClientsLock)
                                                {
                                                    //duplicate id (already connected)
                                                    if (connectedClients.Contains(packetSequence.Sender))
                                                    {
                                                        CaptureException(() =>
                                                        {
                                                            PacketSequence.Send(stream, GUID,
                                                                new ConnectionResponse(ConnectionResponse.ResponseCode.DuplicateGUID));
                                                        });
                                                        break;
                                                    }

                                                    //id ok
                                                    connectedClients.Add(clientGUID = packetSequence.Sender);
                                                }

                                                //request OK, send response with initial sync
                                                lock (operationsLock)
                                                {
                                                    //create the list of staged operations for this client
                                                    outgoingOperations[clientGUID] = new List<Operation>();

                                                    //send response
                                                    if (CaptureException(() =>
                                                        { PacketSequence.Send(stream, GUID, new ConnectionResponse(filePath, masterOperations)); }))
                                                            break;
                                                }
                                            }
                                            else //initial handshake sync has been performed, handle normal requests
                                            {
                                                //check guid
                                                if (!packetSequence.Sender.Equals(clientGUID))
                                                    continue; //ignore (shouldn't happen?)

                                                switch (packetSequence.PayloadType)
                                                {
                                                    case OperationList.PayloadType: //normal update request
                                                    {
                                                        //deserialize operation request
                                                        OperationList incoming = null;
                                                        if (CaptureException(() => { incoming = packetSequence.Payload.Deserialize<OperationList>(); }))
                                                            break;

                                                        //lock operation lists (3a)
                                                        lock (operationsLock)
                                                        {
                                                            //get the list of staged operations for the sender
                                                            List<Operation> outgoing = outgoingOperations[clientGUID];

                                                            //if this oplist is not an empty request
                                                            if (incoming.Operations != null && incoming.Operations.Count > 0)
                                                            {
                                                                //transform incoming ops against outgoing ops (3b)
                                                                if (outgoing.Count > 0)
                                                                    Operation.SymmetricLinearTransform(incoming.Operations, outgoing);

                                                                //append incoming ops to master and to all other outgoing (3c)
                                                                masterOperations.AddRange(incoming.Operations);
                                                                foreach (var kvp in outgoingOperations)
                                                                    if (!kvp.Key.Equals(clientGUID))
                                                                        kvp.Value.AddRange(incoming.Operations);
                                                            }

                                                            //move all outgoing into our response packet (3d)
                                                            OperationList response = new OperationList(outgoing.Count > 0 ? new List<Operation>(outgoing) : null);
                                                            outgoing.Clear();

                                                            //send response
                                                            CaptureException(() => { PacketSequence.Send(stream, GUID, response); });
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                        }

                                        //remove this client's outgoing operation set and connected clients set
                                        if (!clientGUID.Equals(Guid.Empty))
                                        {
                                            lock (operationsLock)
                                            {
                                                outgoingOperations.Remove(clientGUID);
                                            }

                                            lock (connectedClientsLock)
                                            {
                                                connectedClients.Remove(clientGUID);
                                            }
                                        }

                                        //close stream and tcp client
                                        stream.Dispose();
                                        client.Close();
                                    }));
                                    clientThreads.Last().IsBackground = false;
                                    clientThreads.Last().Start(tcpListener.AcceptTcpClient());
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

                            //stop tcp listener
                            CaptureException(() => { tcpListener.Stop(); });

                            //wait for client threads to close
                            foreach (Thread thread in clientThreads)
                                thread.Join();

                            //final flush to disk (don't need a lock this time, all client threads have stopped)
                            CaptureException(() => { SyncFileContents(); });
                        });
                        thread.IsBackground = false;
                        thread.Start(listener);

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

            //clear session state
            outgoingOperations.Clear();
            masterOperations.Clear();
            fileContents = "";
            fileSyncIndex = 0;
            password = null;
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
