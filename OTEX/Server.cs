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
        /// Bundle of parameters for Server.Start (so the parameter list isn't enormous).
        /// </summary>
        [Serializable]
        public sealed class StartParams
        {           
            /// <summary>
            /// Path to the text file you'd like clients to edit/create.
            /// </summary>
            public string Path = "";

            /// <summary>
            /// How to handle the file given by Path if it already exists.
            /// True: reads the file in and uses it's contents as the starting point for the session document;
            /// False: session document is empty, and the existing file is overwritten when the server syncs to disk.
            /// </summary>
            public bool EditMode = true;

            /// <summary>
            /// Listening for new client connections will bind to this port (supports IPv4 and IPv6).
            /// </summary>
            public ushort Port = 55555;

            /// <summary>
            /// A password required for clients to connect to this session (null == no password required).
            /// </summary>
            public Password Password = null;

            /// <summary>
            /// Advertise the presence of this server so it shows up in local server browsers.
            /// </summary>
            public bool Announce = false;

            /// <summary>
            /// The friendly name of the server.
            /// </summary>
            public string Name = "";
        }
        private volatile StartParams startParams = null;

        /// <summary>
        /// Path to the local file this server is resposible for editing.
        /// </summary>
        public string FilePath
        {
            get { return startParams == null ? "" : startParams.Path; }
        }

        /// <summary>
        /// Port to listen on.
        /// </summary>
        public ushort Port
        {
            get { return startParams == null ? (ushort)55555 : startParams.Port; }
        }

        /// <summary>
        /// Is this server broadcasting its presence?
        /// </summary>
        public bool Announce
        {
            get { return startParams == null ? false : startParams.Announce; }
        }

        /// <summary>
        /// Does this server require a password?
        /// </summary>
        public bool RequiresPassword
        {
            get { return startParams == null ? false : startParams.Password != null; }
        }

        /// <summary>
        /// The friendly name of the server.
        /// </summary>
        public string Name
        {
            get { return startParams == null ? "" : startParams.Name; }
        }

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
        /// <param name="startParams">Configuration of this session.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="IOException" />
        public void Start(StartParams startParams)
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
                        if (startParams == null)
                            throw new ArgumentNullException("startParams");

                        //session state
                        if ((startParams.Path = (startParams.Path ?? "").Trim()).Length == 0)
                            throw new ArgumentException("startParams.Path cannot be empty", "startParams.Path");
                        if (startParams.Port < 1024)
                            throw new ArgumentOutOfRangeException("startParams.Port", "Port must be between 1024 and 65535");
                        this.startParams = startParams;
                        if ((this.startParams.Name = (this.startParams.Name ?? "").Trim()).Length > 32)
                            this.startParams.Name = this.startParams.Name.Substring(0, 32);

                        //session initialization
                        TcpListener listener = null;
                        UdpClient announcer = null;
                        try
                        {
                            //read file
                            if (startParams.EditMode && File.Exists(FilePath))
                            {
                                masterOperations.Add(new Operation(ID, 0,
                                    fileContents = File.ReadAllText(FilePath, FilePath.DetectEncoding())
                                        .Replace("\t", new string(' ', 4))
                                        .Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n")
                                        ));
                                ++fileSyncIndex;
                            }

                            //create tcp listener
                            listener = new TcpListener(IPAddress.IPv6Any, Port);
                            listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                            listener.AllowNatTraversal(true);
                            listener.Start();

                            if (startParams.Announce)
                            {
                                //create udp client
                                announcer = new UdpClient();
                                announcer.EnableBroadcast = true;
                                announcer.AllowNatTraversal(true);
                            }
                        }
                        catch (Exception)
                        {
                            if (listener != null)
                                listener.Stop();
                            if (announcer != null)
                                announcer.Close();
                            ClearRunningState();
                            throw;
                        }
                        running = true;

                        //create thread
                        thread = new Thread(ControlThread);
                        thread.IsBackground = false;
                        thread.Start(new object[] { listener, announcer });

                        //fire event
                        OnStarted?.Invoke(this);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONTROL (LOOP) THREAD
        /////////////////////////////////////////////////////////////////////

        private void ControlThread(object nobjs)
        {
            var networkObjects = nobjs as object[];
            var listener = networkObjects[0] as TcpListener;
            var announcer = networkObjects[1] as UdpClient;
            var clientThreads = new List<Thread>();
            var flushTimer = new Marzersoft.Timer();
            var announceTimer = new Marzersoft.Timer();
            var announceData = (new ServerAnnounce(ID, startParams.Name, startParams.Port, startParams.Password != null))
                .Serialize();

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

                //announce
                if (startParams.Announce && announceTimer.Seconds >= 1.0)
                {
                    CaptureException(() =>
                    {
                        announcer.Send(announceData, announceData.Length, new IPEndPoint(IPAddress.Broadcast, 55555));
                    });
                    announceTimer.Reset();
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
                    if ((startParams.Password != null && (request.Password == null || !startParams.Password.Matches(request.Password))) //requires password
                        || (startParams.Password == null && request.Password != null)) //no password required (reject incoming requests with passwords)
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
                        if (!CaptureException(() => { stream.Write(ID, new ConnectionResponse(startParams.Path, masterOperations)); }))
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
            startParams = null;
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
