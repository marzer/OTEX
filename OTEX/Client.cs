﻿using System;
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
    /// Client class for the OTEX framework.
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
        public event Action<IClient, Guid, byte[]> OnMetadataUpdated;

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
        /// Pending metadata.
        /// </summary>
        private volatile byte[] pendingMetadata = null;

        /// <summary>
        /// Lock object for pending metadata.
        /// </summary>
        private readonly object pendingMetadataLock = new object();

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
        public void Connect(IPAddress address, ushort port = Server.DefaultPort, Password password = null, byte[] metadata = null)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            Connect(new IPEndPoint(address, port), password, metadata);
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
        public void Connect(ServerDescription serverDescription, Password password = null, byte[] metadata = null)
        {
            if (serverDescription == null)
                throw new ArgumentNullException("serverDescription");
            Connect(serverDescription.EndPoint, password, metadata);
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
        public void Connect(IPEndPoint endpoint, Password password = null, byte[] metadata = null)
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

                        //check metadata
                        if (metadata != null && metadata.Length > ClientMetadata.MaxSize)
                            throw new ArgumentOutOfRangeException("metadata",
                                string.Format("metadata.Length may not be greater than {0}.", ClientMetadata.MaxSize));

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

                            //send connection request packet
                            packetStream.Write(ID, new ConnectionRequest(password, metadata));

                            //get response
                            responsePacket = packetStream.Read();
                            if (responsePacket.SenderID.Equals(Guid.Empty))
                                throw new InvalidDataException("responsePacket.SenderID was Guid.Empty");
                            if (responsePacket.PayloadType != ConnectionResponse.PayloadType)
                                throw new InvalidDataException("unexpected response packet type");
                            response = responsePacket.Payload.Deserialize<ConnectionResponse>();
                            if (response.Result != ConnectionResponse.ResponseCode.Approved)
                                throw new Exception(string.Format("connection rejected by server: {0}",response.Result));
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
                        serverID = responsePacket.SenderID;
                        awaitingOperationList = false;

                        //fire events
                        OnConnected?.Invoke(this);
                        InvokeRemoteOperations(response.Operations);
                        if (response.Metadata != null && OnMetadataUpdated != null)
                        {
                            foreach (var md in response.Metadata)
                                OnMetadataUpdated?.Invoke(this, md.Key, md.Value);
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
                if (Listen(stream))
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

                        //send request (2)
                        if (CaptureException(() => { stream.Write(ID,
                            new OperationList(outgoingOperations.Count > 0 ? outgoingOperations : null)); }))
                            break;
                        awaitingOperationList = true;

                        //clear outgoing packet list (1)
                        if (outgoingOperations.Count > 0)
                            outgoingOperations.Clear();

                        //apply any incoming operations (also clears list)
                        InvokeRemoteOperations(incomingOperations);
                    }
                    lastOpsRequestTimer.Reset();
                }

                //send off any pending metadata updates
                if (pendingMetadata != null)
                {
                    lock (pendingMetadataLock)
                    {
                        if (pendingMetadata != null)
                        {
                            if (CaptureException(() => { stream.Write(ID, new ClientMetadata(ID, pendingMetadata));  }))
                                break;
                            pendingMetadata = null;
                        }
                    }
                }
            }

            //disconnect
            connected = false;
            if (clientSideDisconnection) //tell the server the user is disconnecting client-side
                CaptureException(() => { stream.Write(ID, new DisconnectionRequest()); });
            stream.Dispose();
            client.Close();
            outgoingOperations.Clear();
            incomingOperations.Clear();
            pendingMetadata = null;

            //fire event
            OnDisconnected?.Invoke(this, !clientSideDisconnection);
            clientSideDisconnection = false;
        }

        /// <summary>
        /// Listen for packets coming in from the server and handle them accordingly.
        /// </summary>
        /// <param name="stream">PacketStream in use by the Control thread.</param>
        /// <returns>True if the server has asked us to disconnect.</returns>
        private bool Listen(PacketStream stream)
        {
            while (stream.Connected && stream.DataAvailable)
            {
                //read incoming packet
                Packet packet = null;
                if (CaptureException(() => { packet = stream.Read(); }))
                    break;

                //check if guid of sender matches server
                if (!packet.SenderID.Equals(serverID))
                    continue; //ignore 

                switch (packet.PayloadType)
                {
                    case DisconnectionRequest.PayloadType: //disconnection request from server
                        return true;

                    case OperationList.PayloadType:  //operation list (4)
                        /*
                         * COMP7722: step 4 of HOB: incoming operations are appended to the local
                         * incoming operation buffer.
                         */
                        if (awaitingOperationList)
                        {
                            OperationList operationList = null;
                            if (CaptureException(() => { operationList = packet.Payload.Deserialize<OperationList>(); }))
                                break;
                            if (operationList.Operations != null && operationList.Operations.Count > 0)
                                incomingOperations.AddRange(operationList.Operations);
                            awaitingOperationList = false;
                        }
                        break;

                    case ClientMetadata.PayloadType:
                        {
                            //deserialize metadata packet
                            ClientMetadata metadataPacket = null;
                            if (CaptureException(() => { metadataPacket = packet.Payload.Deserialize<ClientMetadata>(); })
                                || metadataPacket.Metadata == null || metadataPacket.Metadata.Count == 0)
                                break;

                            if (metadataPacket.Metadata != null && OnMetadataUpdated != null)
                            {
                                foreach (var md in metadataPacket.Metadata)
                                    OnMetadataUpdated?.Invoke(this, md.Key, md.Value);
                            }
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
        // SEND CLIENT-SPECIFIC APPLICATION DATA (METADATA)
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Updates this client's application-specific metadata, sending it to the server
        /// which then ensures the new version is received by all other clients.
        /// </summary>
        /// <param name="metadata">This client's metadata. Can be null ("I don't have any metadata").</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InvalidOperationException" />
        public void Metadata(byte[] metadata)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");
            if (!connected)
                throw new InvalidOperationException("cannot send a metadata update without being connected");
            if (metadata != null && metadata.Length > ClientMetadata.MaxSize)
                throw new ArgumentOutOfRangeException("metadata",
                    string.Format("metadata.Length may not be greater than {0}.", ClientMetadata.MaxSize));

            if (metadata != pendingMetadata)
            {
                lock (pendingMetadataLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Client");
                    if (metadata != pendingMetadata)
                        pendingMetadata = metadata;
                }
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
