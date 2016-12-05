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
        /// Triggered when a remote client connects to the server.
        /// </summary>
        public event Action<IClient, RemoteClient> OnRemoteConnection;

        /// <summary>
        /// Triggered when a remote client updates it's metadata.
        /// </summary>
        public event Action<IClient, RemoteClient> OnRemoteMetadata;

        /// <summary>
        /// Triggered when the client receives a bundle of remote operations from the server.
        /// </summary>
        public event Action<Client, Guid, IEnumerable<Operation>> OnRemoteOperations;

        /// <summary>
        /// Triggered when a remote client in the same session disconnects from the server.
        /// </summary>
        public event Action<IClient, RemoteClient> OnRemoteDisconnection;

        /// <summary>
        /// Triggered when the client is disconnected from an OTEX server.
        /// Boolean parameter is true if the disconnection was forced by the server.
        /// </summary>
        public event Action<IClient, bool> OnDisconnected;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maximum size of a client's metadata.
        /// </summary>
        internal const uint MaxMetadataSize = 2048;

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
                return session != null;
            }
        }   

        /// <summary>
        /// Session information for the current connection.
        /// </summary>
        public ISession Session
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                return session;
            }
        }
        private Session session = null;
        private volatile bool killSession = false;
        private readonly object sessionLock = new object();

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
        private readonly Dictionary<Guid, List<Operation>> outgoingOperations
            = new Dictionary<Guid, List<Operation>>();

        /// <summary>
        /// Staging list for storing incoming operations.
        /// </summary>
        private readonly Dictionary<Guid, List<Operation>> incomingOperations
            = new Dictionary<Guid, List<Operation>>();

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
        /// Getter and setter both work with shallow copies of the data; no reference is kept to the original input
        /// array, nor is a direct reference to the internal buffer returned.
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
                    return (byte[])metadata.Clone();
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
                            metadata = (byte[])md.Clone();
                        metadataChanged = true;
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
        /// <param name="key">AppKey for this client. Will only be compatible with other nodes sharing a matching AppKey.</param>
        /// <param name="guid">Session ID for this client. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public Client(AppKey key, Guid? guid = null) : base(key, guid)
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
            if (serverDescription.AppKey != AppKey)
                throw new ArgumentOutOfRangeException("serverDescription", "AppKeys do not match");
            Connect(serverDescription.EndPoint, password);
        }

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="endpoint">IP Endpoint (address and port) of the OTEX server.</param>
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
        public void Connect(IPEndPoint endpoint, Password password = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");

            if (session == null)
            {
                lock (sessionLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Client");
                    if (session != null)
                        throw new InvalidOperationException("Client is already connected to a server. Call Disconnect() first.");
 
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
                        packetStream.Write(new ConnectionRequest(AppKey, ID, md, password));

                        //get response
                        responsePacket = packetStream.Read();
                        if (responsePacket.PayloadType != ConnectionResponse.PayloadType)
                            throw new InvalidDataException("unexpected response packet type");
                        response = responsePacket.Payload.Deserialize<ConnectionResponse>();
                        if (response.Result != ConnectionResponse.ResponseCode.Approved)
                            throw new IOException(string.Format("Rejected: {0}", ConnectionResponse.Describe(response.Result)));                     
                    }
                    catch (Exception)
                    {
                        if (packetStream != null)
                            packetStream.Dispose();
                        if (tcpClient != null)
                            tcpClient.Close();
                        throw;
                    }

                    //connected ok
                    killSession = false;
                    awaitingOperationList = false;
                    foreach (var kvp in response.Session.documents)
                    {
                        incomingOperations[kvp.Key] = new List<Operation>();
                        outgoingOperations[kvp.Key] = new List<Operation>();
                    }
                    response.Session.address = endpoint.Address;
                    ++response.Session.RemoteClientCount; //self
                    response.Session.ReadOnly = true;
                    var clients = response.Session.Clients;
                    response.Session.Clients = null;
                    session = response.Session;
                    
                    //fire events
                    OnConnected?.Invoke(this);
                    foreach (var kvp in session.documents)
                    {
                        if (kvp.Value.MasterOperations != null && kvp.Value.MasterOperations.Count > 0)
                        {
                            OnRemoteOperations?.Invoke(this, kvp.Key, kvp.Value.MasterOperations);
                            kvp.Value.MasterOperations.Clear();
                        }
                        kvp.Value.MasterOperations = null;
                    }
                    foreach (var kvp in clients)
                    {
                        ++session.RemoteClientCount;
                        OnRemoteConnection?.Invoke(this, kvp.Value);
                    }

                    //create management thread
                    thread = new Thread(ControlThread);
                    thread.Name = "OTEX Client ControlThread";
                    thread.IsBackground = false;
                    thread.Start(new object[]{ tcpClient, packetStream });
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONTROL (LOOP) THREAD
        /////////////////////////////////////////////////////////////////////

        private void ControlThread(object o)
        {
            var objs = o as object[];
            var tcpClient = objs[0] as TcpClient;
            var stream = objs[1] as PacketStream;
            var lastOpsRequestTimer = new Marzersoft.Timer();

            while (!killSession && stream.Connected)
            {
                Thread.Sleep(1);
                
                //listen for packets first
                //(returns true if the server has asked us to disconnect)
                if (Listen(stream, lastOpsRequestTimer))
                {
                    //override killSession so we don't send
                    //unnecessarily send a disconnection to the server
                    killSession = false;
                    break;
                }

                //send periodic requests for new operations
                if (!awaitingOperationList && lastOpsRequestTimer.Seconds >= updateInterval)
                {
                    lock (operationsLock)
                    {
                        //merge adjacent operations
                        Operation.Merge(outgoingOperations);

                        //perform SLOT(OB,CIB) (1)
                        Operation.SymmetricLinearTransform(outgoingOperations, incomingOperations);
                        if (Operation.Merge(outgoingOperations))
                            Operation.Trim(outgoingOperations);

                        //send metadata update
                        Dictionary<Guid, RemoteClient> md = null;
                        lock (metadataLock)
                        {
                            if (metadataChanged)
                            {
                                metadataChanged = false;
                                md = new Dictionary<Guid, RemoteClient>();
                                md[ID] = new RemoteClient(ID, metadata);
                            }
                        }

                        //send request (2)
                        if (CaptureException(() =>{ stream.Write(new ClientUpdate(outgoingOperations, md)); }))
                            break;
                        awaitingOperationList = true;

                        //clear outgoing packet list (1)
                        foreach (var kvp in outgoingOperations)
                            kvp.Value.Clear();

                        //apply any incoming operations and clear list
                        foreach (var kvp in incomingOperations)
                        {
                            if (kvp.Value.Count > 0)
                            {
                                OnRemoteOperations?.Invoke(this, kvp.Key, kvp.Value);
                                kvp.Value.Clear();
                            }
                        }
                    }
                }
            }

            //disconnect
            session = null;
            if (killSession) //tell the server the user is disconnecting client-side
                CaptureException(() => { stream.Write(new DisconnectionRequest()); });
            stream.Dispose();
            tcpClient.Close();
            outgoingOperations.Clear();
            incomingOperations.Clear();

            //fire event
            OnDisconnected?.Invoke(this, !killSession);
            killSession = false;
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

                    case RemoteConnection.PayloadType: //another client has connected
                        {
                            RemoteConnection rcon = null;
                            if (CaptureException(() => { rcon = packet.Payload.Deserialize<RemoteConnection>(); }))
                                break;
                            ++session.RemoteClientCount;
                            OnRemoteConnection?.Invoke(this, rcon.Client);
                        }
                        break;

                    case RemoteDisconnection.PayloadType: //another client has disconnected
                        {
                            RemoteDisconnection remoteDisconnection = null;
                            if (CaptureException(() => { remoteDisconnection = packet.Payload.Deserialize<RemoteDisconnection>(); }))
                                break;
                            --session.RemoteClientCount;
                            OnRemoteDisconnection?.Invoke(this, new RemoteClient(remoteDisconnection.ClientID, null));
                        }
                        break;

                    case ClientUpdate.PayloadType:  //operation list (4)
                        if (awaitingOperationList)
                        {
                            awaitingOperationList = false;
                            ClientUpdate update = null;
                            if (CaptureException(() => { update = packet.Payload.Deserialize<ClientUpdate>(); }))
                                break;

                            //get incoming operations
                            if (update.Operations != null && update.Operations.Count > 0)
                            {
                                lock (operationsLock)
                                {
                                    foreach (var kvp in update.Operations)
                                    {
                                        if (kvp.Value != null && kvp.Value.Count > 0
                                            && incomingOperations.TryGetValue(kvp.Key, out var doc))
                                            doc.AddRange(kvp.Value);
                                    }
                                }
                            }

                            //get incoming metadata
                            if (update.Metadata != null && update.Metadata.Count > 0 && OnRemoteMetadata != null)
                            {
                                foreach (var md in update.Metadata)
                                    OnRemoteMetadata?.Invoke(this, md.Value);
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
        // SEND LOCAL OPERATIONS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Send a notification to the server that some text was inserted at the client end.
        /// </summary>
        /// <param name="documentID">ID of the document the insertion applies to.</param>
        /// <param name="offset">The index of the insertion.</param>
        /// <param name="text">The text that was inserted.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InvalidOperationException" />
        public void Insert(Guid documentID, uint offset, string text)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");
            if (text == null)
                throw new ArgumentNullException("text");
            if (text.Length == 0)
                throw new ArgumentException("insert text cannot be blank", "text");
            if (session == null)
                throw new InvalidOperationException("cannot send an operation without being connected");

            lock (operationsLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                if (session == null)
                    throw new InvalidOperationException("cannot send an operation without being connected");
                outgoingOperations[documentID].Add(new Operation(ID, (int)offset, text));
            }
        }

        /// <summary>
        /// Send a notification to the server that some text was deleted at the client end.
        /// </summary>
        /// <param name="documentID">ID of the document the deletion applies to.</param>
        /// <param name="offset">The index of the deletion.</param>
        /// <param name="length">The length of the deleted range.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InvalidOperationException" />
        public void Delete(Guid documentID, uint offset, uint length)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");
            if (length == 0)
                throw new ArgumentOutOfRangeException("length", "deletion length cannot be zero");
            if (session == null)
                throw new InvalidOperationException("cannot send an operation without being connected");

            lock (operationsLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Client");
                if (session == null)
                    throw new InvalidOperationException("cannot send an operation without being connected");
                outgoingOperations[documentID].Add(new Operation(ID, (int)offset, (int)length));
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
            if (session != null && !killSession)
            {
                lock (sessionLock)
                {
                    if (session != null && !killSession)
                    {
                        killSession = true;
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
            ClearEventListeners();
        }

        /// <summary>
        /// Clears all subscriptions to event listeners
        /// </summary>
        protected override void ClearEventListeners()
        {
            base.ClearEventListeners();
            OnConnected = null;
            OnDisconnected = null;
            OnRemoteConnection = null;
            OnRemoteOperations = null;
            OnRemoteMetadata = null;
            OnRemoteDisconnection = null;
        }
    }
}
