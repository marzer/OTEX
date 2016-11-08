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
using OTEX.Packets;

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
        /// Triggered when a new client connects.
        /// </summary>
        public event Action<Server, Guid> OnClientConnected;

        /// <summary>
        /// Triggered when a client disconnects.
        /// </summary>
        public event Action<Server, Guid> OnClientDisconnected;

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
        /// How many clients are currently connected?
        /// </summary>
        public int ClientCount
        {
            get { return connectedClients.Count; }
        }

        /// <summary>
        /// Staging lists for storing outgoing operations.
        /// </summary>
        private readonly Dictionary<Guid, List<Operation>> outgoingOperations = new Dictionary<Guid,List<Operation>>();

        /// <summary>
        /// Lock object for operations and clients state;
        /// </summary>
        private readonly object stateLock = new object();

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
        /// <param name="guid">Session ID for this server. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public Server(Guid? guid = null) : base(guid)
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
        /// <param name="editMode">If a file at the given path already exists, set this to true
        /// to read it and apply changes, or false to overwrite it with a blank file (new file mode).</param>
        /// <param name="port">Port to listen on. Must be between 1024 and 65535.</param>
        /// <param name="password">Password required to connect to this server. Leave as null for none.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="IOException" />
        public void Start(string path, bool editMode = true, ushort port = 55555, Password password = null)
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
                        TcpListener listener6 = null;
                        try
                        {
                            //read file
                            if (editMode && File.Exists(FilePath))
                            {
                                masterOperations.Add(new Operation(ID, 0,
                                    fileContents = File.ReadAllText(FilePath, FilePath.DetectEncoding())
                                        .Replace("\t", new string(' ', 4))
                                        .Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n")
                                        ));
                                ++fileSyncIndex;
                            }

                            //create tcplistener
                            listener6 = new TcpListener(IPAddress.IPv6Any, Port);
                            listener6.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                            listener6.AllowNatTraversal(true);
                            listener6.Start();
                        }
                        catch (Exception)
                        {
                            ClearRunningState();
                            throw;
                        }
                        running = true;

                        //create thread
                        thread = new Thread(ListenThread);
                        thread.IsBackground = false;
                        thread.Start( listener6 );

                        //fire event
                        OnStarted?.Invoke(this);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // MAIN LISTENING THREAD
        /////////////////////////////////////////////////////////////////////

        private void ListenThread(object listenerObject)
        {
            TcpListener listener = listenerObject as TcpListener;
            List<Thread> clientThreads = new List<Thread>();
            var flushTimer = new Marzersoft.Timer();
            while (running)
            {
                Thread.Sleep(1);

                //accept the connection
                while (listener.Pending())
                {
                    //get client
                    TcpClient tcpClient = null;
                    if (CaptureException(() => { tcpClient = listener.AcceptTcpClient(); }))
                        continue;

                    //create client thread
                    clientThreads.Add(new Thread(ClientThread));
                    clientThreads.Last().IsBackground = false;
                    clientThreads.Last().Start(tcpClient);
                }

                //flush file contents to disk periodically
                if (flushTimer.Seconds >= 15.0)
                {
                    lock (stateLock)
                    {
                        CaptureException(() => { SyncFileContents(); });
                    }
                    flushTimer.Reset();
                }
            }

            //stop tcp listener
            CaptureException(() => { listener.Stop(); });

            //wait for client threads to close
            foreach (Thread thread in clientThreads)
                thread.Join();

            //final flush to disk (don't need a lock this time, all client threads have stopped)
            CaptureException(() => { SyncFileContents(); });
        }

        /////////////////////////////////////////////////////////////////////
        // PER-CLIENT THREAD
        /////////////////////////////////////////////////////////////////////

        private void ClientThread(object tcpClientObject)
        {
            TcpClient client = tcpClientObject as TcpClient;

            //create packet stream
            PacketStream stream = null;
            if (CaptureException(() => { stream = new PacketStream(client); }))
                return;

            //read from stream
            Guid clientGUID = Guid.Empty;
            bool clientSideDisconnect = false;
            while (running && stream.Connected)
            {
                //check if client has sent data
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(1);
                    continue;
                }

                //read incoming packet
                Packet packet = null;
                if (CaptureException(() => { packet = stream.Read(); }))
                    break;

                //check if guid matches server's (shouldn't happen?)
                if (packet.SenderID.Equals(ID))
                    continue; //ignore 

                //is this the first packet from a new client?
                if (clientGUID.Equals(Guid.Empty))
                {
                    //check if packet is a request type
                    if (packet.PayloadType != ConnectionRequest.PayloadType)
                        continue; //ignore

                    //deserialize packet
                    ConnectionRequest request = null;
                    if (CaptureException(() => { request = packet.Payload.Deserialize<ConnectionRequest>(); }))
                        break;

                    //check password
                    if ((password != null && (request.Password == null || !password.Matches(password))) //requires password
                        || (password == null && request.Password != null)) //no password required (reject incoming requests with passwords)
                    {
                        CaptureException(() =>
                        {
                            stream.Write(ID,
                                new ConnectionResponse(ConnectionResponse.ResponseCode.IncorrectPassword));
                        });
                        break;
                    }

                    lock (stateLock)
                    {
                        //duplicate id (already connected)
                        if (connectedClients.Contains(packet.SenderID))
                        {
                            CaptureException(() =>
                            {
                                stream.Write(ID,
                                    new ConnectionResponse(ConnectionResponse.ResponseCode.DuplicateGUID));
                            });
                            break;
                        }

                        //send response with initial sync list
                        if (!CaptureException(() => { stream.Write(ID, new ConnectionResponse(filePath, masterOperations)); }))
                        {
                            //create the list of staged operations for this client
                            outgoingOperations[packet.SenderID] = new List<Operation>();

                            //add to list of connected clients
                            connectedClients.Add(clientGUID = packet.SenderID);
                        }
                        else break;
                    }

                    //notify
                    OnClientConnected?.Invoke(this, clientGUID);
                }
                else //initial handshake sync has been performed, handle normal requests
                {
                    //check guid
                    if (!packet.SenderID.Equals(clientGUID))
                        continue; //ignore (shouldn't happen?)

                    switch (packet.PayloadType)
                    {
                        case DisconnectionRequest.PayloadType: //disconnection request from client
                            clientSideDisconnect = true;
                            break;

                        case OperationList.PayloadType: //normal update request
                            {
                                //deserialize operation request
                                OperationList incoming = null;
                                if (CaptureException(() => { incoming = packet.Payload.Deserialize<OperationList>(); }))
                                    break;

                                //lock operation lists (3a)
                                lock (stateLock)
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

                                    //send response
                                    CaptureException(() => { stream.Write(ID, new OperationList(outgoing.Count > 0 ? outgoing : null)); });

                                    //clear outgoing packet list (3d)
                                    outgoing.Clear();
                                }
                            }
                            break;
                    }

                    if (clientSideDisconnect)
                        break;
                }
            }

            //remove this client's outgoing operation set and connected clients set
            if (!clientGUID.Equals(Guid.Empty))
            {
                bool disconnected = false;
                lock (stateLock)
                {
                    outgoingOperations.Remove(clientGUID);
                    disconnected = connectedClients.Remove(clientGUID);
                }
                if (disconnected)
                {
                    //if the client has not requested a disconnection themselves, send them one
                    if (!clientSideDisconnect)
                        CaptureException(() => { stream.Write(ID, new DisconnectionRequest()); });
                    OnClientDisconnected?.Invoke(this, clientGUID);
                }
            }

            //close stream and tcp client
            stream.Dispose();
            client.Close();
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
