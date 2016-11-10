using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Marzersoft;
using OTEX.Packets;

namespace OTEX
{
    /// <summary>
    /// Client class for the OTEX framework.
    /// </summary>
    public class Client : Node, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when the client successfully connects to an OTEX server.
        /// </summary>
        public event Action<Client> OnConnected;

        /// <summary>
        /// Triggered when the client receives remote operations from the server.
        /// Do not call any of this object's methods from this callback or you may deadlock!
        /// </summary>
        public event Action<Client, IEnumerable<Operation>> OnRemoteOperations;

        /// <summary>
        /// Triggered when the client is disconnected from an OTEX server.
        /// Boolean parameter is true if the disconnection was forced by the server.
        /// </summary>
        public event Action<Client, bool> OnDisconnected;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// has this client been disposed?
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
            get { return connected; }
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
            get { return serverAddress; }
        }
        private volatile IPAddress serverAddress;

        /// <summary>
        /// Server's listen port.
        /// </summary>
        public ushort ServerPort
        {
            get { return serverPort; }
        }
        private volatile ushort serverPort;

        /// <summary>
        /// Path of the file being edited on the server.
        /// </summary>
        public string ServerFilePath
        {
            get { return serverFilePath; }
        }
        private volatile string serverFilePath;

        /// <summary>
        /// ID of server.
        /// </summary>
        public Guid ServerID
        {
            get { return serverID; }
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
        /// Lock object for operations and clients state;
        /// </summary>
        private readonly object stateLock = new object();

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
        public void Connect(IPAddress address, ushort port = 55555, Password password = null)
        {
            Connect(new IPEndPoint(address, port), password);
        }

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
                        //session state
                        if (endpoint == null)
                            throw new ArgumentNullException("endpoint");
                        if (endpoint.Port < 1024)
                            throw new ArgumentOutOfRangeException("endpoint.Port", "Port must be between 1024 and 65535");

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

                            //send connection request packet
                            packetStream.Write(ID, new ConnectionRequest(password));

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
                        serverID = responsePacket.SenderID;
                        awaitingOperationList = false;

                        //fire events
                        OnConnected?.Invoke(this);
                        InvokeRemoteOperations(response.Operations);

                        //create management thread
                        thread = new Thread(ControlThread);
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

            while (!clientSideDisconnection && client.Connected)
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
                if (!awaitingOperationList && lastOpsRequestTimer.Seconds >= 0.5)
                {
                    lock (stateLock)
                    {
                        //perform SLOT(OB,CIB) (1)
                        if (outgoingOperations.Count > 0 && incomingOperations.Count > 0)
                            Operation.SymmetricLinearTransform(outgoingOperations, incomingOperations);

                        //send request
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
            }

            //disconnect
            connected = false;
            if (clientSideDisconnection) //tell the server the user is disconnecting client-side
                CaptureException(() => { stream.Write(ID, new DisconnectionRequest()); });
            stream.Dispose();
            client.Close();
            outgoingOperations.Clear();
            incomingOperations.Clear();

            //fire event
            OnDisconnected?.Invoke(this, !clientSideDisconnection);
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
                }
            }
            return false;
        }

        /////////////////////////////////////////////////////////////////////
        // INVOKE REMOTE OPERATIONS
        /////////////////////////////////////////////////////////////////////

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
            if (text == null)
                throw new ArgumentNullException("text");
            if (text.Length == 0)
                throw new ArgumentException("insert text cannot be blank", "text");
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");
            if (!connected)
                throw new InvalidOperationException("cannot send an operation without being connected");

            lock (stateLock)
            {
                outgoingOperations.Add(new Operation(ID, (int)offset, text));
            }
        }

        /// <summary>
        /// Send a notification to the server that some text was deleted at the client end.
        /// </summary>
        /// <param name="offset">The index of the deletion.</param>
        /// <param name="text">The length of the deleted range.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InvalidOperationException" />
        public void Delete(uint offset, uint length)
        {
            if (length == 0)
                throw new ArgumentOutOfRangeException("length", "deletion length cannot be zero");
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");
            if (!connected)
                throw new InvalidOperationException("cannot send an operation without being connected");

            lock (stateLock)
            {
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
            if (connected)
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
