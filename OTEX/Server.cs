using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Marzersoft;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using OTEX.Packets;
using System.Text;

namespace OTEX
{
    /// <summary>
    /// Server class for the OTEX framework.
    /// </summary>
    public sealed class Server : Node, IDisposable
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
        /// The default port used by the OTEX system for connections between client and server.
        /// </summary>
        public const ushort DefaultPort = 55550;

        /// <summary>
        /// The port range used to annouce the presence of public servers.
        /// </summary>
        public static readonly PortRange AnnouncePorts = new PortRange(55561, 55564);

        /// <summary>
        /// Bundle of parameters for Server.Start (so the parameter list isn't enormous).
        /// </summary>
        [Serializable]
        public sealed class StartParams
        {           
            /// <summary>
            /// Path to the text file you'd like clients to edit/create.
            /// </summary>
            public string FilePath = "";

            /// <summary>
            /// How to handle the file given by Path if it already exists.
            /// True: reads the file in and uses it's contents as the starting point for the session document;
            /// False: session document is empty, and the existing file is overwritten when the server syncs to disk.
            /// </summary>
            public bool EditMode = true;

            /// <summary>
            /// Listening for new client connections will bind to this port (supports IPv4 and IPv6).
            /// </summary>
            public ushort Port = DefaultPort;

            /// <summary>
            /// A password required for clients to connect to this session (null == no password required).
            /// </summary>
            public Password Password = null;

            /// <summary>
            /// Advertise the presence of this server so it shows up in local server browsers.
            /// </summary>
            public bool Public = false;

            /// <summary>
            /// The friendly name of the server.
            /// </summary>
            public string Name = "";

            /// <summary>
            /// How many clients are allowed to be connected at once?
            /// Setting it to 0 means "no limit" (there is an internal maximum limit of 100)
            /// </summary>
            public uint MaxClients = 10;

            /// <summary>
            /// When loading file, how many spaces should tab characters be replaced with?
            /// Leave as 0 to skip replacement and leave them as-is.
            /// Useful if paired with a client editor which does not support \t characters
            /// (e.g. FastColoredTextBox).
            /// </summary>
            public uint ReplaceTabsWithSpaces = 0;

            /// <summary>
            /// Path to plain text file containing list of clients who have been banned from the server.
            /// If no path is provided, bans will not be read from or written to disk.
            /// </summary>
            public string BanListPath = null;

            /// <summary>
            /// Initial collection of bans. Can be used in tandem with BanListPath; if a file exists, the contents
            /// will be merged into this set.
            /// </summary>
            public readonly HashSet<Guid> BanList = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public StartParams()
            {
                //
            }

            /// <summary>
            /// Internal copy constructor.
            /// </summary>
            internal StartParams(StartParams p)
            {
                FilePath = p.FilePath;
                EditMode = p.EditMode;
                Port = p.Port;
                Password = p.Password;
                Public = p.Public;
                Name = p.Name;
                MaxClients = p.MaxClients;
                ReplaceTabsWithSpaces = p.ReplaceTabsWithSpaces.Clamp(0,8);
                BanListPath = p.BanListPath;
                BanList = p.BanList == null ? new HashSet<Guid>() : new HashSet<Guid>(p.BanList);
            }
        }
        private volatile StartParams startParams = null;

        /// <summary>
        /// Path to the local file this server is resposible for editing.
        /// </summary>
        public string FilePath
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return startParams == null ? "" : startParams.FilePath;
            }
        }

        /// <summary>
        /// Port to listen on.
        /// </summary>
        public ushort Port
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return startParams == null ? (ushort)DefaultPort : startParams.Port;
            }
        }

        /// <summary>
        /// Is this server broadcasting its presence?
        /// </summary>
        public bool Public
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return startParams == null ? false : startParams.Public;
            }
        }

        /// <summary>
        /// Does this server require a password?
        /// </summary>
        public bool RequiresPassword
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return startParams == null ? false : startParams.Password != null;
            }
        }

        /// <summary>
        /// The friendly name of the server.
        /// </summary>
        public string Name
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return startParams == null ? "" : startParams.Name;
            }
        }

        /// <summary>
        /// Master list of operations, used for synchronizing newly-connected clients.
        /// </summary>
        private readonly List<Operation> masterOperations = new List<Operation>();

        /// <summary>
        /// How many clients are currently connected?
        /// </summary>
        public uint ClientCount
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return (uint)connectedClients.Count;
            }
        }

        /// <summary>
        /// How many clients are allowed to be connected at once?
        /// </summary>
        public uint MaxClients
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return startParams == null ? 10u : startParams.MaxClients;
            }
        }

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
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return running;
            }
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
        /// Line ending scheme used by loaded file (defaults to CRLF on windows).
        /// </summary>
        public string FileLineEndings
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return fileLineEnding;
            }
        }
        private volatile string fileLineEnding = Environment.NewLine;

        /// <summary>
        /// Data for each client connected to the server.
        /// </summary>
        private class ClientData
        {
            /// <summary>
            /// Client's ID.
            /// </summary>
            public readonly Guid ID;

            /// <summary>
            /// Staging list for storing outgoing operations.
            /// </summary>
            public readonly List<Operation> OutgoingOperations = new List<Operation>();

            /// <summary>
            /// Metadata attached to this client, if any.
            /// </summary>
            public byte[] Metadata = null;

            /// <summary>
            /// Staging list for storing outgoing metadata updates.
            /// </summary>
            public readonly Dictionary<Guid, byte[]> OutgoingMetadata
                = new Dictionary<Guid, byte[]>();

            /// <summary>
            /// Has this client been kicked from the server? This will be true if the server calls Ban()
            /// or Kick() with this client's ID while the server is running.
            /// </summary>
            public volatile bool Kicked = false;

            public ClientData(Guid id)
            {
                ID = id;
            }
        };

        /// <summary>
        /// All currently connected clients.
        /// </summary>
        private readonly Dictionary<Guid, ClientData> connectedClients = new Dictionary<Guid, ClientData>();

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

                        //copy params (do not keep reference to input)
                        StartParams tempParams = new StartParams(startParams);

                        //validate params
                        tempParams.FilePath = (tempParams.FilePath ?? "").Trim();
                        if (tempParams.Port < 1024 || AnnouncePorts.Contains(tempParams.Port))
                            throw new ArgumentOutOfRangeException("startParams.Port",
                                string.Format("Port must be between 1024-{0} and {1}-65535.",
                                    AnnouncePorts.First-1, AnnouncePorts.Last+1));
                        if ((tempParams.Name = (tempParams.Name ?? "").Trim()).Length > 32)
                            tempParams.Name = tempParams.Name.Substring(0, 32);
                        if (tempParams.MaxClients == 0 || tempParams.MaxClients > 100u)
                            tempParams.MaxClients = 100u;
                        if ((tempParams.BanListPath = (tempParams.BanListPath ?? "").Trim()).Length > 0)
                        {
                            if (!File.Exists(tempParams.BanListPath) && Directory.Exists(tempParams.BanListPath))
                                throw new FileNotFoundException("startParams.BanListPath", "Given path is a directory");
                            bool immediateFlush = tempParams.BanList.Count > 0;
                            if (File.Exists(tempParams.BanListPath))
                            {
                                var lines = File.ReadAllLines(tempParams.BanListPath, tempParams.BanListPath.DetectEncoding());
                                for (int i = 0; i < lines.Length; ++i)
                                {
                                    if ((lines[i] = lines[i].Trim()).Length == 0)
                                        continue;
                                    Guid guid;
                                    if (lines[i].TryParse(out guid))
                                        tempParams.BanList.Add(guid);
                                }
                            }
                            if (immediateFlush)
                                FlushBanList();
                        }
                        this.startParams = tempParams;

                        //session initialization
                        TcpListener tcpListener = null;
                        UdpClient announcer = null;
                        try
                        {
                            
                            if (this.startParams.FilePath.Length > 0 && this.startParams.EditMode && File.Exists(FilePath))
                            {
                                //read file
                                fileContents = File.ReadAllText(FilePath, FilePath.DetectEncoding());

                                //replace tabs with spaces
                                if (this.startParams.ReplaceTabsWithSpaces > 0)
                                    fileContents = fileContents.Replace("\t",
                                        new string(' ', (int)this.startParams.ReplaceTabsWithSpaces));

                                //detect line ending type
                                int crlfCount = RegularExpressions.CrLf.Split(fileContents).Length;
                                string fileContentsNoCRLF = RegularExpressions.CrLf.Replace(fileContents, "");
                                int crCount = RegularExpressions.Cr.Split(fileContentsNoCRLF).Length;
                                int lfCount = RegularExpressions.CrLf.Split(fileContentsNoCRLF).Length;
                                if (crlfCount > crCount && crlfCount > lfCount)
                                    fileLineEnding = "\r\n";
                                else if (crCount > crlfCount && crCount > lfCount)
                                    fileLineEnding = "\r";
                                else if (lfCount > crlfCount && lfCount > crCount)
                                    fileLineEnding = "\n";
                                else //??
                                    fileLineEnding = Environment.NewLine;
                                fileContentsNoCRLF = null;

                                //normalize line endings
                                fileContents = fileContents.NormalizeLineEndings();
                               
                                //add initial operation
                                masterOperations.Add(new Operation(ID, 0, fileContents));
                                ++fileSyncIndex;
                            }

                            //create tcp listener
                            tcpListener = new TcpListener(IPAddress.IPv6Any, Port);
                            tcpListener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                            tcpListener.AllowNatTraversal(true);
                            tcpListener.Start();

                            if (this.startParams.Public)
                            {
                                //create udp client
                                announcer = new UdpClient();
                                announcer.EnableBroadcast = true;
                                announcer.AllowNatTraversal(true);
                            }
                        }
                        catch (Exception)
                        {
                            if (tcpListener != null)
                            {
                                try { tcpListener.Stop(); } catch (Exception) { };
                            }
                            if (announcer != null)
                            {
                                try { announcer.Close(); } catch (Exception) { };
                            }
                            ClearRunningState();
                            throw;
                        }
                        running = true;

                        //create thread
                        thread = new Thread(ControlThread);
                        thread.Name = "OTEX Server ControlThread";
                        thread.IsBackground = false;
                        thread.Start(new object[] { tcpListener, announcer });

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
            var tcpListener = networkObjects[0] as TcpListener;
            var announcer = networkObjects[1] as UdpClient;
            var clientThreads = new List<Thread>();
            var flushTimer = new Marzersoft.Timer();
            var announceTimer = new Marzersoft.Timer();
            var announceEndpoints = new List<IPEndPoint>();

            while (running)
            {
                Thread.Sleep(1);

                //accept the connection
                while (tcpListener.Pending())
                {
                    //get client
                    TcpClient tcpClient = null;
                    if (CaptureException(() => { tcpClient = tcpListener.AcceptTcpClient(); }))
                        continue;

                    //create client thread
                    clientThreads.Add(new Thread(ClientThread));
                    clientThreads.Last().Name = "OTEX Server ClientThread";
                    clientThreads.Last().IsBackground = false;
                    clientThreads.Last().Start(tcpClient);
                }

                //announce
                if (startParams.Public && announceTimer.Seconds >= 1.0)
                {
                    CaptureException(() =>
                    {
                        //create endpoints if they don't exist
                        if (announceEndpoints.Count == 0)
                            for (int i = AnnouncePorts.First; i <= AnnouncePorts.Last; ++i)
                                announceEndpoints.Add(new IPEndPoint(IPAddress.Broadcast, i));

                        //serialize a server description of the current state
                        var announceData = (new ServerDescription(this)).Serialize();

                        //broadcast to the port range
                        foreach (IPEndPoint ep in announceEndpoints)
                            announcer.Send(announceData, announceData.Length, ep);
                    });
                    announceTimer.Reset();
                }

                //flush file contents to disk periodically
                if (startParams.FilePath.Length > 0 && flushTimer.Seconds >= 15.0)
                {
                    lock (stateLock)
                        CaptureException(() => { FlushDocument(); });
                    flushTimer.Reset();
                }
            }

            //stop listeners and announcer
            CaptureException(() => { tcpListener.Stop(); });
            if (announcer != null)
                CaptureException(() => { announcer.Close(); });

            //wait for client threads to close
            foreach (Thread thread in clientThreads)
                thread.Join();

            //final flush to disk
            if (startParams.FilePath.Length > 0)
                CaptureException(() => { FlushDocument(); });
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
            bool clientSideDisconnect = false;
            ClientData clientData = null;
            while (running && stream.Connected)
            {
                //check banned flag
                if (clientData != null && clientData.Kicked)
                    break;
                
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

                //check banned flag again (in case it changed during the packet read)
                if (clientData != null && clientData.Kicked)
                    break;

                //is this the first packet from a new client?
                if (clientData == null)
                {
                    //check packet type
                    //if it's not a connection request, abort (the client is in a bad state)
                    if (packet.PayloadType != ConnectionRequest.PayloadType)
                    {
                        //let client know we're cutting them off
                        CaptureException(() =>
                        {
                            stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.InvalidState));
                        });
                        break;
                    }

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
                            stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.IncorrectPassword));
                        });
                        break;
                    }

                    lock (stateLock)
                    {
                        //check ban list
                        if (startParams.BanList.Contains(request.ClientID))
                        {
                            CaptureException(() =>
                            {
                                stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.Banned));
                            });
                            break;
                        }

                        //duplicate id (already connected... shouldn't happen?)
                        if (connectedClients.TryGetValue(request.ClientID, out var cl))
                        {
                            CaptureException(() =>
                            {
                                stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.DuplicateGUID));
                            });
                            break;
                        }

                        //too many connections already
                        if (connectedClients.Count >= startParams.MaxClients)
                        {
                            CaptureException(() =>
                            {
                                stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.SessionFull));
                            });
                            break;
                        }

                        //get outgoing set of metadata for other clients for initial sync
                        //(doubles as client list)
                        Dictionary<Guid, byte[]> metadata = new Dictionary<Guid, byte[]>();
                        foreach (var kvp in connectedClients)
                            metadata[kvp.Key] = kvp.Value.Metadata;

                        //send response with initial sync list and metadata for other clients
                        if (!CaptureException(() => { stream.Write(new ConnectionResponse(ID,startParams.FilePath,
                            startParams.Name, masterOperations, metadata)); }))
                        {
                            //add metadata to outgoing list of other clients
                            //(doubles as connection notification)
                            foreach (var kvp in connectedClients)
                                kvp.Value.OutgoingMetadata[request.ClientID] = request.Metadata;

                            //create the internal data for this client (includes list of staged operations)
                            connectedClients[request.ClientID] = clientData = new ClientData(request.ClientID);
                        }
                        else
                            break; //sending ConnectionResponse failed (connection broken)
                    }

                    //notify
                    OnClientConnected?.Invoke(this, clientData.ID);
                }
                else //initial handshake sync has been performed, handle normal requests
                {
                    switch (packet.PayloadType)
                    {
                        case DisconnectionRequest.PayloadType: //disconnection request from client
                            clientSideDisconnect = true;
                            break;

                        case ClientUpdate.PayloadType: //normal update request
                            {
                                //deserialize operation request
                                ClientUpdate incoming = null;
                                if (CaptureException(() => { incoming = packet.Payload.Deserialize<ClientUpdate>(); }))
                                    break;

                                //lock operation lists (3a)
                                lock (stateLock)
                                {
                                    //if this oplist is not an empty request
                                    if (incoming.Operations != null && incoming.Operations.Count > 0)
                                    {
                                        //perform SLOT(OB,SIB) (3b)
                                        if (clientData.OutgoingOperations.Count > 0)
                                            Operation.SymmetricLinearTransform(incoming.Operations, clientData.OutgoingOperations);

                                        //append incoming ops to master and to all other outgoing (3c)
                                        masterOperations.AddRange(incoming.Operations);
                                        foreach (var kvp in connectedClients)
                                            if (!kvp.Key.Equals(clientData.ID))
                                                kvp.Value.OutgoingOperations.AddRange(incoming.Operations);
                                    }

                                    //handle incoming metadata
                                    if (incoming.Metadata != null && incoming.Metadata.Count > 0)
                                    {
                                        //get metadata array
                                        if (!incoming.Metadata.TryGetValue(clientData.ID, out var metadata))
                                            break; //shouldn't happen; clients only send their own

                                        //compare it to existing metadata
                                        if ((metadata == null && clientData.Metadata != null)
                                            || (metadata != null && (clientData.Metadata == null
                                                || !metadata.MemoryEquals(clientData.Metadata))))
                                        {
                                            //update client metadata
                                            clientData.Metadata = metadata;

                                            //add to staging lists for other clients
                                            foreach (var kvp in connectedClients)
                                                if (!kvp.Key.Equals(clientData.ID))
                                                    kvp.Value.OutgoingMetadata[clientData.ID] = clientData.Metadata;
                                        }
                                    }

                                    //send response
                                    CaptureException(() => { stream.Write(
                                        new ClientUpdate(clientData.OutgoingOperations, clientData.OutgoingMetadata)); });

                                    //clear outgoing packet list (3d)
                                    clientData.OutgoingOperations.Clear();

                                    //clear outgoing metadata list
                                    clientData.OutgoingMetadata.Clear();
                                }
                            }
                            break;
                    }

                    if (clientSideDisconnect)
                        break;
                }
            }

            //remove the internal data for this client
            if (clientData != null)
            {
                bool disconnected = false;
                lock (stateLock)
                {
                    disconnected = connectedClients.Remove(clientData.ID);
                }
                if (disconnected)
                {
                    //if the client has not requested a disconnection themselves, send them one
                    if (!clientSideDisconnect)
                        CaptureException(() => { stream.Write(new DisconnectionRequest()); });
                    OnClientDisconnected?.Invoke(this, clientData.ID);
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
        private void FlushDocument()
        {
            //flush pending operations to the file contents
            while (fileSyncIndex < masterOperations.Count)
            {
                if (!masterOperations[fileSyncIndex].IsNoop)
                    fileContents = masterOperations[fileSyncIndex].Execute(fileContents);
                ++fileSyncIndex;
            }

            //check line ending normalization
            var fileOutput = fileContents;
            if (!fileLineEnding.Equals(Environment.NewLine))
                fileOutput = fileContents.Replace(Environment.NewLine, fileLineEnding);

            //write contents to disk
            File.WriteAllText(FilePath, fileOutput);

            //trigger event
            OnFileSynchronized?.Invoke(this);
        }

        /////////////////////////////////////////////////////////////////////
        // BANNING/KICKING CLIENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Kicks a client from the server, optionally banning them.
        /// </summary>
        /// <param name="id">The id of the client to kick</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ObjectDisposedException" />
        public void Kick(Guid id, bool ban = false)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Server");
            if (id.Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
            if (!running)
                throw new InvalidOperationException("Server is not running.");

            lock (stateLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                if (!running)
                    throw new InvalidOperationException("Server is not running.");

                if (connectedClients.TryGetValue(id, out ClientData client))
                    client.Kicked = true; //will cause it to be disconnected by the main thread

                if (ban)
                {
                    startParams.BanList.Add(id);
                    if (startParams.BanListPath.Length > 0)
                        FlushBanList();
                }
            }
        }

        /// <summary>
        /// Writes the list of bans to disk.
        /// </summary>
        private void FlushBanList()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ban in startParams.BanList)
                sb.AppendLine(ban.ToString());
            File.WriteAllText(startParams.BanListPath, sb.ToString());
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
            connectedClients.Clear();
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
            ClearEventListeners();
        }

        /// <summary>
        /// Clears all subscriptions to event listeners
        /// </summary>
        protected override void ClearEventListeners()
        {
            base.ClearEventListeners();
            OnClientConnected = null;
            OnClientDisconnected = null;
            OnFileSynchronized = null;
            OnStarted = null;
            OnStopped = null;
        }
    }
}
