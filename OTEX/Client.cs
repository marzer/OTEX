using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Marzersoft;

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
        /// Triggered when the client is disconnected from an OTEX server.
        /// Bool parameter is true if the connection was forced server-side.
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
        /// Thread for managing client connection.
        /// </summary>
        private Thread thread = null;

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
            if (GUID.Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("guid cannot be Guid.Empty");
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
                        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.Broadcast) || address.Equals(IPAddress.None))
                            throw new ArgumentOutOfRangeException("Address must be a valid non-range IP Address.");


                        //session connection
                        TcpClient client = null;
                        NetworkStream stream = null;
                        try
                        {
                            //establish tcp connection
                            client = new TcpClient();
                            client.Connect(address, port);
                            stream = client.GetStream();

                            //send connection request packet
                            PacketSequence.Send(stream, GUID, new ConnectionRequest(password));

                            //get response
                            PacketSequence responseSequence = new PacketSequence(stream);
                            if (responseSequence.PayloadType != ConnectionResponse.PayloadType)
                                throw new InvalidDataException("unexpected response packet type");
                            ConnectionResponse response = responseSequence.Payload.Deserialize<ConnectionResponse>();
                            if (response.Result != ConnectionResponse.ResponseCode.Approved)
                                throw new Exception(string.Format("connection rejected by server: {0}",response.Result));

                            //set connected state
                            connected = true;
                            serverPort = port;
                            serverAddress = address;
                            serverFilePath = response.FilePath;
                        }
                        catch (Exception)
                        {
                            if (stream != null)
                                stream.Dispose();
                            if (client != null)
                                client.Close();
                            throw;
                        }

                        OnConnected?.Invoke(this);
                    }
                }
            }
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
                    if (connected)
                    {
                        connected = false;
                        OnDisconnected?.Invoke(this, false);
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
