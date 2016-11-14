using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Marzersoft;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using OTEX.Packets;

namespace OTEX
{
    /*
     * COMP7722: The class implemented below is the Server side of the OTEX framework.
     * It is responsible for maintaining the master document and periodically synchronizing it
     * to disk, as well as listening for client operations and propagating them accordingly.
     * The server-side of the SLOT algorithm from the NICE approach is applied within.
     */

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
            /// Replace tabs in loaded files with spaces?
            /// Useful if paired with a client editor which does not support \t characters
            /// (e.g. FastColoredTextBox).
            /// </summary>
            public uint ReplaceTabsWithSpaces = 0;

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
                                int crlfCount = Text.REGEX_CRLF.Split(fileContents).Length;
                                string fileContentsNoCRLF = Text.REGEX_CRLF.Replace(fileContents, "");
                                int crCount = Text.REGEX_CR.Split(fileContentsNoCRLF).Length;
                                int lfCount = Text.REGEX_LF.Split(fileContentsNoCRLF).Length;
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
                                fileContents = fileContents.Replace("\r\n", "\n").Replace("\r", "\n");
                                if (!Environment.NewLine.Equals("\n"))
                                    fileContents = fileContents.Replace("\n", Environment.NewLine);
                               
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

                /*
                 * COMP7722: Incoming connection requests are dispatched to their own managing thread.
                 */
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

                /*
                 * COMP7722: File contents is synchronized to disk periodically (every 15 seconds)
                 */
                //flush file contents to disk periodically
                if (startParams.FilePath.Length > 0 && flushTimer.Seconds >= 15.0)
                {
                    lock (stateLock)
                    {
                        CaptureException(() => { SyncFileContents(); });
                    }
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

            //final flush to disk (don't need a lock this time, all client threads have stopped)
            if (startParams.FilePath.Length > 0)
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
            bool clientSideDisconnect = false;
            ClientData clientData = null;
            while (running && stream.Connected)
            {
                //if we're connected already, send any pending metadata
                if (clientData != null && clientData.OutgoingMetadata.Count > 0)
                {
                    lock (stateLock)
                    {
                        if (clientData.OutgoingMetadata.Count > 0)
                        {
                            if (CaptureException(() => { stream.Write(ID,
                                new ClientMetadata(clientData.OutgoingMetadata)); }))
                                break;
                            clientData.OutgoingMetadata.Clear();
                        }
                    }
                }
                
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
                    break;

                //is this the first packet from a new client?
                if (clientData == null)
                {
                    //check packet type
                    //if it's not a connection or disconnection request, abort since the client
                    //is not funtioning correctly
                    if (packet.PayloadType != ConnectionRequest.PayloadType
                        && packet.PayloadType != DisconnectionRequest.PayloadType)
                    {
                        //let client know we're cutting them off
                        CaptureException(() =>
                        {
                            stream.Write(ID,
                                new ConnectionResponse(ConnectionResponse.ResponseCode.InvalidState));
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
                            stream.Write(ID,
                                new ConnectionResponse(ConnectionResponse.ResponseCode.IncorrectPassword));
                        });
                        break;
                    }

                    lock (stateLock)
                    {
                        //duplicate id (already connected... shouldn't happen?)
                        ClientData cl = null;
                        if (connectedClients.TryGetValue(packet.SenderID, out cl))
                        {
                            CaptureException(() =>
                            {
                                stream.Write(ID,
                                    new ConnectionResponse(ConnectionResponse.ResponseCode.DuplicateGUID));
                            });
                            break;
                        }

                        //too many connections already
                        if (connectedClients.Count >= startParams.MaxClients)
                        {
                            CaptureException(() =>
                            {
                                stream.Write(ID,
                                    new ConnectionResponse(ConnectionResponse.ResponseCode.SessionFull));
                            });
                            break;
                        }

                        /*
                        * COMP7722: Master operation list is sent to new clients as part of the server's response
                        * to their initial connection request, to bring them up to speed without further handshaking.
                        */

                        //get outgoing set of metadata for other clients for initial sync
                        //(doubles as client list)
                        Dictionary<Guid, byte[]> metadata = new Dictionary<Guid, byte[]>();
                        foreach (var kvp in connectedClients)
                            metadata[kvp.Key] = kvp.Value.Metadata;

                        //send response with initial sync list and metadata for other clients
                        if (!CaptureException(() => { stream.Write(ID,
                            new ConnectionResponse(startParams.FilePath, startParams.Name,
                                masterOperations.Count == 0 ? null : masterOperations, metadata)); }))
                        {
                            //add metadata to outgoing list of other clients
                            //(doubles as connection notification)
                            foreach (var kvp in connectedClients)
                                kvp.Value.OutgoingMetadata[packet.SenderID] = request.Metadata;

                            //create the internal data for this client (includes list of staged operations)
                            connectedClients[packet.SenderID] = clientData = new ClientData(packet.SenderID);


                        }
                        else
                            break; //sending ConnectionResponse failed (connection broken)
                    }

                    //notify
                    OnClientConnected?.Invoke(this, clientData.ID);
                }
                else //initial handshake sync has been performed, handle normal requests
                {
                    //check guid
                    if (!packet.SenderID.Equals(clientData.ID))
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

                                /*
                                 * COMP7722: step 3 of HOB: receive new operations from the client,
                                 * transform them using SLOT, respond with new operations from other clients,
                                 * and append the new operations to the outgoing lists of other connected clients.
                                 */

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

                                    //send response
                                    CaptureException(() => { stream.Write(ID,
                                        new OperationList(clientData.OutgoingOperations.Count > 0 ? clientData.OutgoingOperations : null)); });

                                    //clear outgoing packet list (3d)
                                    clientData.OutgoingOperations.Clear();
                                }
                            }
                            break;

                        case ClientMetadata.PayloadType: //client is updating it's metadata
                            {
                                //deserialize metadata packet
                                ClientMetadata metadataPacket = null;
                                if (CaptureException(() => { metadataPacket = packet.Payload.Deserialize<ClientMetadata>(); })
                                    || metadataPacket.Metadata == null || metadataPacket.Metadata.Count == 0)
                                    break;

                                //get metadata array
                                byte[] metadata;
                                if (!metadataPacket.Metadata.TryGetValue(clientData.ID, out metadata))
                                    break; //shouldn't happen; clients only send their own

                                //compare it to existing metadata
                                if ((metadata == null && clientData.Metadata != null)
                                    || (metadata != null && (clientData.Metadata == null
                                        || !metadata.MemoryEquals(clientData.Metadata))))
                                {
                                    //update client metadata
                                    clientData.Metadata = metadata;

                                    //add to staging lists for other clients
                                    lock (stateLock)
                                    {
                                        foreach (var kvp in connectedClients)
                                            if (!kvp.Key.Equals(clientData.ID))
                                                kvp.Value.OutgoingMetadata[clientData.ID] = clientData.Metadata;
                                    }
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
                        CaptureException(() => { stream.Write(ID, new DisconnectionRequest()); });
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
        private void SyncFileContents()
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
        }
    }
}
