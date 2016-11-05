using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
        /// Bool parameter is true if the disconnection was forced (server-side or timeout).
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
        /// Session ID for this client.
        /// </summary>
        public readonly Guid GUID;

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

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX client.
        /// </summary>
        /// <param name="guid">Session id for this client. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public Client(Guid? guid = null)
        {
            if ((GUID = guid.HasValue ? guid.Value : Guid.NewGuid()).CompareTo(Guid.Empty) == 0)
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
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        public void Connect(IPAddress address, ushort port = 55555)
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
                        if (port < 1024)
                            throw new ArgumentOutOfRangeException("Port must be between 1024 and 65535");
                        serverPort = port;

                        if (address == null)
                            throw new ArgumentNullException("Address cannot be null");
                        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.Broadcast) || address.Equals(IPAddress.None))
                            throw new ArgumentOutOfRangeException("Address must be a valid non-range IP Address.");
                        serverAddress = address;


                        connected = true;

                        ///////


                        OnConnected?.Invoke(this);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // DISCONNECTING FROM THE SERVER
        /////////////////////////////////////////////////////////////////////

        private void ClearConnectedState()
        {

        }

        public void Disconnect()
        {
            if (connected)
            {
                lock (connectedLock)
                {
                    if (connected)
                    {
                        connected = false;
                        ClearConnectedState();
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
