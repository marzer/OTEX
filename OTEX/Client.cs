﻿using System;
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
        /// Triggered when the client receives a new list of operations from the server.
        /// Do not call Connect or Disconnect from this callback or you will deadlock!
        /// </summary>
        public event Action<Client, IEnumerable<Operation>> OnIncomingOperations;

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
        /// Connect to an OTEX server. Does nothing if already connected.
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
        public void Connect(IPAddress address, ushort port = 55555, Password password = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Client");

            if (!connected)
            {
                lock (connectedLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Client");

                    if (!connected)
                    {
                        //session state
                        if (port < 1024)
                            throw new ArgumentOutOfRangeException("Port must be between 1024 and 65535");
                        if (address == null)
                            throw new ArgumentNullException("Address cannot be null");
                        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.Broadcast) || address.Equals(IPAddress.None)
                            || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None))
                            throw new ArgumentOutOfRangeException("Address must be a valid non-range IP Address.");

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
                            tcpClient.Connect(address, port);
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
                        serverPort = port;
                        serverAddress = address;
                        serverFilePath = response.FilePath;
                        serverID = responsePacket.SenderID;
                        awaitingOperationList = false;

                        //fire events
                        OnConnected?.Invoke(this);
                        if (response.Operations != null && response.Operations.Count > 0)
                            OnIncomingOperations?.Invoke(this, response.Operations);

                        //create management thread
                        thread = new Thread(ControlThread);
                        thread.IsBackground = false;
                        thread.Start(new object[]{ tcpClient, packetStream });

                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONTROL THREAD
        /////////////////////////////////////////////////////////////////////

        private void ControlThread(object o)
        {
            var objs = o as object[];
            var client = objs[0] as TcpClient;
            var stream = objs[1] as PacketStream;

            while (!clientSideDisconnection && client.Connected)
            {
                var lastOpsRequestTimer = new Marzersoft.Timer();
                
                //listen for packets first
                //(returns true if the server has asked us to disconnect)
                if (Listen(stream))
                {
                    //override clientSideDisconnection so we don't send
                    //unnecessarily send a disconnection to the server
                    clientSideDisconnection = false;
                    break;
                }

                //send period requests for new operations
                if (!awaitingOperationList && lastOpsRequestTimer.Seconds >= 1.0)
                {
                    //todo: send operations request


                    lastOpsRequestTimer.Reset();
                }
            }

            //disconnect
            connected = false;
            if (clientSideDisconnection) //tell the server the user is disconnecting client-side
                CaptureException(() => { stream.Write(ID, new DisconnectionRequest()); });
            stream.Dispose();
            client.Close();

            //fire event
            OnDisconnected?.Invoke(this, !clientSideDisconnection);
        }

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

                    case OperationList.PayloadType:  //operation list
                        if (awaitingOperationList)
                        {

                            //todo: process ops list client-side

                            awaitingOperationList = false;
                        }
                        break;
                }
            }
            return false;
        }


        /////////////////////////////////////////////////////////////////////
        // DISCONNECTING FROM THE SERVER
        /////////////////////////////////////////////////////////////////////

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
