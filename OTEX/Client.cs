using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Marzersoft;
using OTEX.Packets;

namespace OTEX
{
    /*
     * COMP7722: The class implemented below is the Client side of the OTEX framework.
     * It is responsible for collecting local operations and periodically sending them
     * off to the server, and then listening for the response. The client-side of the SLOT
     * algorithm from the NICE approach is applied within.
     * 
     * Unlike the server, the client does not maintain it's own internal copy of the document;
     * instead all new incoming operations are handled using callbacks which advertise the
     * operations. This allows applications to use the Client as an intermediary.
     */

    /// <summary>
    /// Client class for the OTEX framework. Manages connection to a server, and provides methods to
    /// notify the server of local replica insertions and deletions. If you want a simpler, less flexible
    /// implementation, one that lets you read/write the whole document and have the operations
    /// determined for you automatically, use BufferedClient instead.
    /// </summary>
    public sealed class Client : Node, IDisposable, IClient
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when the client successfully connects to an OTEX server.
        /// </summary>
        public event Action<IClient> OnConnected;

        /// <summary>
        /// Triggered when a remote client updates it's metadata.
        /// Do not call any of this object's methods from this callback or you may deadlock!
        /// </summary>
        public event Action<IClient, Guid, byte[]> OnRemoteMetadata;

        /// <summary>
        /// Triggered when the client is disconnected from an OTEX server.
        /// Boolean parameter is true if the disconnection was forced by the server.
        /// </summary>
        public event Action<IClient, bool> OnDisconnected;

        /// <summary>
        /// Triggered when the client receives remote operations from the server.
        /// Do not call any of this object's methods from this callback or you may deadlock!
        /// </summary>
        public event Action<Client, IEnumerable<Operation>> OnRemoteOperations;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maximum size of a client's metadata.
        /// </summary>
        public const uint MaxMetadataSize = 2048;

        /// <summary>
        /// Has this client been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return isDisposed; }
        }
        private volatile bool isDisposed = false;

        /// <summary>
        /// Is the client currently connected to a server?
        /// </summary>
        public bool Connected
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return connected;
            }
        }
        private volatile bool connected = false;
        private volatile bool clientSideDisconnection = false;

        /// <summary>
        /// Lock object for connected state.
        /// </summary>
        private readonly object connectedLock = new object();

        /// <summary>
        /// IP Address of server.
        /// </summary>
        public IPAddress ServerAddress
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return serverAddress;
            }
        }
        private volatile IPAddress serverAddress;

        /// <summary>
        /// Server's listen port.
        /// </summary>
        public ushort ServerPort
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return serverPort;
            }
        }
        private volatile ushort serverPort;

        /// <summary>
        /// Path of the file being edited on the server.
        /// </summary>
        public string ServerFilePath
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return serverFilePath;
            }
        }
        private volatile string serverFilePath;

        /// <summary>
        /// The friendly name of the server.
        /// </summary>
        public string ServerName
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return serverName;
            }
        }
        private volatile string serverName;

        /// <summary>
        /// ID of server.
        /// </summary>
        public Guid ServerID
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return serverID;
            }
        }
        private Guid serverID = Guid.Empty;

        /// <summary>
        /// Thread for managing client connection.
        /// </summary>
        private Thread thread = null;

        /// <summary>
        /// Have we sent a request for operations to the server?
        /// </summary>
        private volatile bool awaitingOperationList = false;

        /// <summary>
        /// Staging list for storing outgoing operations.
        /// </summary>
        private readonly List<Operation> outgoingOperations = new List<Operation>();

        /// <summary>
        /// Staging list for storing incoming operations.
        /// </summary>
        private readonly List<Operation> incomingOperations = new List<Operation>();

        /// <summary>
        /// Lock object for operations;
        /// </summary>
        private readonly object operationsLock = new object();

        /// <summary>
        /// Time, in seconds, between each request for updates sent to the server
        /// (clamped between 0.5 and 5.0).
        /// </summary>
        public float UpdateInterval
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return updateInterval;
            }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                updateInterval = value.Clamp(0.5f,5.0f);
            }
        }
        private volatile float updateInterval = 0.5f;

        /// <summary>
        /// This client's metadata. Setting this value causes it to be sent to the server with the next update.
        /// The server then ensures the new version is received by all other clients during their next updates.
        /// May be set when the client is not connected; this means "send this when I next connect to a server".
        /// 
        /// The getter returns a copy of the internal metadata buffer (if not null); it does not keep a reference
        /// to the original set value.
        /// </summary>
        public byte[] Metadata
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");

                lock (metadataLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Client");

                    if (metadata == null || metadata.Length == 0)
                        return null;
                    byte[] md = new byte[metadata.Length];
                    metadata.CopyTo(md,0);
                    return md;
                }
            }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");

                lock (metadataLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Client");

                    if (value != null && value.LongLength >= MaxMetadataSize)
                        throw new ArgumentOutOfRangeException("metadata",
                            string.Format("metadata byte arrays may not be longer than {0} bytes", MaxMetadataSize));

                    byte[] md = (value == null || value.Length == 0) ? null : value;
                    if ((metadata == null && md != null)
                        || (metadata != null && (md == null
                            || !metadata.MemoryEquals(md))))
                    {
                        if (md == null)
                            metadata = null;
                        else
                        {
                            metadata = new byte[md.Length];
                            md.CopyTo(metadata, 0);
                            metadataChanged = true;
                        }
                    }
                }
                
            }
        }
        private volatile byte[] metadata = null;
        private volatile bool metadataChanged = false;
        private readonly object metadataLock = new object();

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX client.
        /// </summary>
        /// <param name="guid">Session ID for this client. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public Client(Guid? guid = null) : base(guid)
        {
            //
        }

        /////////////////////////////////////////////////////////////////////
        // CONNECTING TO THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="address">IP Address of the OTEX server.</param>
        /// <param name="port">Listen port of the OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="SocketException" />
        /// <exception cref="InvalidDataException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        /// <exception cref="InvalidOperationException" />
        public void Connect(IPAddress address, ushort port = Server.DefaultPort, Password password = null)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            Connect(new IPEndPoint(address, port), password);
        }

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="serverDescription">ServerDescription for an OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        /// <param name="metadata">Client-specific application data to send to the server.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="SocketException" />
        /// <exception cref="InvalidDataException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        /// <exception cref="InvalidOperationException" />
        public void Connect(ServerDescription serverDescription, Password password = null)
        {
            if (serverDescription == null)
                throw new ArgumentNullException("serverDescription");
            Connect(serverDescription.EndPoint, password);
        }

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="endpoint">IP Endpoint (address and port) of the OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        /// <param name="metadata">Client-specific application data to send to the server.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="SocketException" />
        /// <exception cref="InvalidDataException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        /// <exception cref="InvalidOperationException" />
        public void Connect(IPEndPoint endpoint, Password password = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");

            if (!connected)
            {
                lock (connectedLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Client");
                    if (connected)
                        throw new InvalidOperationException("Client is already connected to a server. Call Disconnect() first.");

                    if (!connected)
                    {
                        //check endpoint
                        if (endpoint == null)
                            throw new ArgumentNullException("endpoint");
                        if (endpoint.Port < 1024 || Server.AnnouncePorts.Contains(endpoint.Port))
                            throw new ArgumentOutOfRangeException("endpoint.Port",
                                string.Format("Port must be between 1024-{0} or {1}-65535.",
                                    Server.AnnouncePorts.First - 1, Server.AnnouncePorts.Last + 1));
                        if (endpoint.Address.Equals(IPAddress.Any) || endpoint.Address.Equals(IPAddress.Broadcast)
                            || endpoint.Address.Equals(IPAddress.None) || endpoint.Address.Equals(IPAddress.IPv6Any)
                            || endpoint.Address.Equals(IPAddress.IPv6None))
                            throw new ArgumentOutOfRangeException("endpoint.Address", "Address cannot be Any, None or Broadcast.");

                        //session connection
                        TcpClient tcpClient = null;
                        PacketStream packetStream = null;
                        Packet responsePacket = null;
                        ConnectionResponse response = null;
                        try
                        {
                            //establish tcp connection
                            tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
                            tcpClient.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                            tcpClient.Connect(endpoint);
                            packetStream = new PacketStream(tcpClient);

                            //get metadata
                            byte[] md = null;
                            lock (metadataLock)
                            {
                                md = metadata;
                                metadataChanged = false;
                            }

                            //send connection request packet
                            packetStream.Write(new ConnectionRequest(ID, md, password));

                            //get response
                            responsePacket = packetStream.Read();
                            if (responsePacket.PayloadType != ConnectionResponse.PayloadType)
                                throw new InvalidDataException("unexpected response packet type");
                            response = responsePacket.Payload.Deserialize<ConnectionResponse>();
                            if (response.Result != ConnectionResponse.ResponseCode.Approved)
                                throw new Exception(string.Format("connection rejected by server: {0}",response.Result));
                            if (response.ServerID.Equals(Guid.Empty))
                                throw new InvalidDataException("response.ServerID was Guid.Empty");
                        }
                        catch (Exception)
                        {
                            if (packetStream != null)
                                packetStream.Dispose();
                            if (tcpClient != null)
                                tcpClient.Close();
                            throw;
                        }

                        //set connected state
                        connected = true;
                        clientSideDisconnection = false;
                        serverPort = (ushort)endpoint.Port;
                        serverAddress = endpoint.Address;
                        serverFilePath = response.FilePath ?? "";
                        serverName = response.Name ?? "";
                        serverID = response.ServerID;
                        awaitingOperationList = false;

                        //fire events
                        OnConnected?.Invoke(this);
                        InvokeRemoteOperations(response.Operations);
                        if (response.Metadata != null && response.Metadata.Count > 0 && OnRemoteMetadata != null)
                        {
                            foreach (var md in response.Metadata)
                                OnRemoteMetadata?.Invoke(this, md.Key, md.Value);
                        }

                        //create management thread
                        thread = new Thread(ControlThread);
                        thread.Name = "OTEX Client ControlThread";
                        thread.IsBackground = false;
                        thread.Start(new object[]{ tcpClient, packetStream });

                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONTROL (LOOP) THREAD
        /////////////////////////////////////////////////////////////////////

        private void ControlThread(object o)
        {
            var objs = o as object[];
            var client = objs[0] as TcpClient;
            var stream = objs[1] as PacketStream;
            var lastOpsRequestTimer = new Marzersoft.Timer();

            while (!clientSideDisconnection && stream.Connected)
            {
                Thread.Sleep(1);
                
                //listen for packets first
                //(returns true if the server has asked us to disconnect)
                if (Listen(stream, lastOpsRequestTimer))
                {
                    //override clientSideDisconnection so we don't send
                    //unnecessarily send a disconnection to the server
                    clientSideDisconnection = false;
                    break;
                }

                //send periodic requests for new operations
                if (!awaitingOperationList && lastOpsRequestTimer.Seconds >= updateInterval)
                {
                    lock (operationsLock)
                    {
                        /*
                         * COMP7722: step 1 & 2 of HOB: all outgoing operations sent to the server,
                         * clearing the local outgoing operation buffer.
                         */

                        //perform SLOT(OB,CIB) (1)
                        if (outgoingOperations.Count > 0 && incomingOperations.Count > 0)
                            Operation.SymmetricLinearTransform(outgoingOperations, incomingOperations);

                        //send metadata update
                        Dictionary<Guid, byte[]> md = null;
                        lock (metadataLock)
                        {
                            if (metadataChanged)
                            {
                                metadataChanged = false;
                                md = new Dictionary<Guid, byte[]>();
                                md[ID] = metadata;
                            }
                        }

                        //send request (2)
                        if (CaptureException(() =>{ stream.Write(new ClientUpdate(outgoingOperations, md)); }))
                            break;

                        awaitingOperationList = true;

                        //clear outgoing packet list (1)
                        if (outgoingOperations.Count > 0)
                            outgoingOperations.Clear();

                        //apply any incoming operations (also clears list)
                        InvokeRemoteOperations(incomingOperations);
                    }
                }
            }

            //disconnect
            connected = false;
            if (clientSideDisconnection) //tell the server the user is disconnecting client-side
                CaptureException(() => { stream.Write(new DisconnectionRequest()); });
            stream.Dispose();
            client.Close();
            outgoingOperations.Clear();
            incomingOperations.Clear();

            //fire event
            OnDisconnected?.Invoke(this, !clientSideDisconnection);
            clientSideDisconnection = false;
        }

        /// <summary>
        /// Listen for packets coming in from the server and handle them accordingly.
        /// </summary>
        /// <param name="stream">PacketStream in use by the Control thread.</param>
        /// <param name="lastOpsRequestTimer">Timer for timing request send intervals.</param>
        /// <returns>True if the server has asked us to disconnect.</returns>
        private bool Listen(PacketStream stream, Marzersoft.Timer lastOpsRequestTimer)
        {
            while (stream.Connected && stream.DataAvailable)
            {
                //read incoming packet
                Packet packet = null;
                if (CaptureException(() => { packet = stream.Read(); }))
                    break;

                switch (packet.PayloadType)
                {
                    case DisconnectionRequest.PayloadType: //disconnection request from server
                        return true;

                    case ClientUpdate.PayloadType:  //operation list (4)
                        /*
                         * COMP7722: step 4 of HOB: incoming operations are appended to the local
                         * incoming operation buffer.
                         */
                        if (awaitingOperationList)
                        {
                            awaitingOperationList = false;
                            ClientUpdate update = null;
                            if (CaptureException(() => { update = packet.Payload.Deserialize<ClientUpdate>(); }))
                                break;

                            //get incoming operations
                            if (update.Operations != null && update.Operations.Count > 0)
                                incomingOperations.AddRange(update.Operations);

                            //get incoming metadata
                            if (update.Metadata != null && update.Metadata.Count > 0 && OnRemoteMetadata != null)
                            {
                                foreach (var md in update.Metadata)
                                    OnRemoteMetadata?.Invoke(this, md.Key, md.Value);
                            }

                            //reset request timer so the wait interval starts from now
                            lastOpsRequestTimer.Reset();
                        }
                        break;
                }
            }
            return false;
        }

        /////////////////////////////////////////////////////////////////////
        // INVOKE REMOTE OPERATIONS
        /////////////////////////////////////////////////////////////////////

        /*
        * COMP7722: This function notifies any callback subscribers
        * clearing the local outgoing operation buffer.
        */

        private void InvokeRemoteOperations(List<Operation> ops)
        {
            if (ops != null && ops.Count() > 0)
            {
                OnRemoteOperations.Invoke(this, ops);
                ops.Clear();
            }
        }

        /////////////////////////////////////////////////////////////////////
        // SEND LOCAL OPERATIONS
        /////////////////////////////////////////////////////////////////////

        /*
        * COMP7722: Because the OTEX client acts as an intemediary, and does not directly maintain
        * it's own internal document buffer, it must provide applications with a method of
        * notifying the OTEX server when a local operation has been generated. The functions below
        * provide this functionality.
        */

        /// <summary>
        /// Send a notification to the server that some text was inserted at the client end.
        /// </summary>
        /// <param name="offset">The index of the insertion.</param>
        /// <param name="text">The text that was inserted.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InvalidOperationException" />
        public void Insert(uint offset, string text)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");
            if (text == null)
                throw new ArgumentNullException("text");
            if (text.Length == 0)
                throw new ArgumentException("insert text cannot be blank", "text");
            if (!connected)
                throw new InvalidOperationException("cannot send an operation without being connected");

            lock (operationsLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                outgoingOperations.Add(new Operation(ID, (int)offset, text));
            }
        }

        /// <summary>
        /// Send a notification to the server that some text was deleted at the client end.
        /// </summary>
        /// <param name="offset">The index of the deletion.</param>
        /// <param name="length">The length of the deleted range.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InvalidOperationException" />
        public void Delete(uint offset, uint length)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");
            if (length == 0)
                throw new ArgumentOutOfRangeException("length", "deletion length cannot be zero");
            if (!connected)
                throw new InvalidOperationException("cannot send an operation without being connected");

            lock (operationsLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                outgoingOperations.Add(new Operation(ID, (int)offset, (int)length));
            }
        }


        /////////////////////////////////////////////////////////////////////
        // DISCONNECTING FROM THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disconnect from an OTEX server. Does nothing if not already connected.
        /// </summary>
        public void Disconnect()
        {
            if (connected && !clientSideDisconnection)
            {
                lock (connectedLock)
                {
                    if (connected && !clientSideDisconnection)
                    {
                        clientSideDisconnection = true;
                        if (thread != null)
                        {
                            thread.Join();
                            thread = null;
                        }
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes this client, disconnecting it (if it was connected) and releasing resources.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            Disconnect();
        }
    }
}
