using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace OTEX
{
    /// <summary>
    /// Packet of information describing a public OTEX server.
    /// Unlike other packets, this is not handled using the PacketStream,
    /// so does not implement IPacketPayload (these are sent over UDP instead).
    /// </summary>
    [Serializable]
    public sealed class ServerDescription
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when some value of this server description is updated internally
        /// by a server listener (e.g. ping, name, requires passsword, etc.).
        /// </summary>
        [field: NonSerialized]
        public event Action<ServerDescription> OnUpdated;

        /// <summary>
        /// Triggered when the owning server listener has not heard from the server in
        /// long enough, and has deemed it to be offline. Servers with Active==false
        /// are removed from their server listener's Servers collection.
        /// </summary>
        [field: NonSerialized]
        public event Action<ServerDescription> OnInactive;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// AppKey of the server.
        /// </summary>
        public AppKey AppKey { get; private set; }

        /// <summary>
        /// Session ID of the server.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Server name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Server port.
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// Does this server require a password?
        /// </summary>
        public bool RequiresPassword { get; private set; }

        /// <summary>
        /// Is the document a temporary one?
        /// (i.e. not backed by a file, will be lost when the server is shutdown)
        /// </summary>
        public bool TemporaryDocument { get; private set; }

        /// <summary>
        /// How many clients are currently connected?
        /// </summary>
        public uint ClientCount { get; private set; }

        /// <summary>
        /// How many clients are allowed to be connected at once?
        /// </summary>
        public uint MaxClients { get; private set; }

        /// <summary>
        /// Where did the packet come from?
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return endPoint; }
        }
        [NonSerialized]
        private IPEndPoint endPoint;

        /// <summary>
        /// What is the average RTT to the server, in milliseconds?
        /// </summary>
        public uint Ping
        {
            get { return ping; }
            private set
            {
                if (value != ping)
                {
                    ping = value;
                    OnUpdated?.Invoke(this);
                }
            }
        }
        [NonSerialized]
        private volatile uint ping = 0;

        /// <summary>
        /// Is this server active?
        /// </summary>
        public bool Active
        {
            get { return active; }
            internal set
            {
                if (value != active)
                {
                    active = value;
                    if (!active)
                        OnInactive?.Invoke(this);
                }
            }
        }
        [NonSerialized]
        private volatile bool active = true;

        /// <summary>
        /// Time in seconds since a server listener last recieved an update packet from this server.
        /// </summary>
        public double LastUpdated
        {
            get { return lastUpdateTimer == null ? 0.0 : lastUpdateTimer.Seconds; }
        }
        [NonSerialized]
        private readonly Marzersoft.Timer lastUpdateTimer;

        /// <summary>
        /// Tag property for attaching additional data to this server description.
        /// </summary>
        [NonSerialized]
        public object Tag = null;

        /// <summary>
        /// Thread responsible for pinging the server.
        /// </summary>
        [NonSerialized]
        private volatile Thread pingThread = null;

        /// <summary>
        /// Time in seconds since this server was last updated by a server listener.
        /// </summary>
        internal double LastPinged
        {
            get { return lastPingTimer == null ? 0.0 : lastPingTimer.Seconds; }
        }
        [NonSerialized]
        private readonly Marzersoft.Timer lastPingTimer;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a server description for use as an announce packet.
        /// </summary>
        /// <param name="server">Server being announced</param>
        /// <exception cref="ArgumentNullException" />
        internal ServerDescription(Server server)
        {
            if (server == null)
                throw new ArgumentNullException("server", "server cannot be null");
            AppKey = server.AppKey;
            ID = server.ID;
            ClientCount = server.ClientCount;
            MaxClients = server.MaxClients;
            RequiresPassword = server.RequiresPassword;
            Port = server.Port;
            Name = server.Name ?? "";
            TemporaryDocument = (server.FilePath ?? "").Length == 0;
            endPoint = null;
            lastUpdateTimer = null;
            lastPingTimer = null;
        }

        /// <summary>
        /// Reconstructs server description from an announce packet.
        /// </summary>
        /// <param name="listenEndpoint">Endpoint of the sender (listen server, NOT the announce source)</param>
        /// <param name="packet">Packet that was sent</param>
        /// <exception cref="ArgumentNullException" />
        internal ServerDescription(IPEndPoint listenEndpoint, ServerDescription packet)
        {
            if (listenEndpoint == null)
                throw new ArgumentNullException("listenEndpoint", "listenEndpoint cannot be null");
            if (packet == null)
                throw new ArgumentNullException("packet", "packet cannot be null");
            ID = packet.ID;
            ClientCount = packet.ClientCount;
            MaxClients = packet.MaxClients;
            RequiresPassword = packet.RequiresPassword;
            Port = packet.Port;
            Name = packet.Name ?? "";
            TemporaryDocument = packet.TemporaryDocument;
            endPoint = listenEndpoint;
            lastUpdateTimer = new Marzersoft.Timer();
            lastPingTimer = new Marzersoft.Timer();
            UpdatePing();
        }

        /////////////////////////////////////////////////////////////////////
        // UPDATING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Updates a server description from an announce packet.
        /// Must be from the same sender (AppKey, ID, address and listen port must match).
        /// Not all aspects of the server can be changed; some properties are fixed for the session
        /// (e.g. file path, temporary). If differences are detected in these properies, an exception will be thrown.
        /// Does not update Ping, that is updated separately.
        /// </summary>
        /// <param name="listenEndpoint">Endpoint of the sender (listen server, NOT the announce source)</param>
        /// <param name="packet">Packet that was sent</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ArgumentNullException" />
        internal void Update(IPEndPoint listenEndpoint, ServerDescription packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet", "packet cannot be null");
            if (listenEndpoint == null)
                throw new ArgumentNullException("listenEndpoint", "sender cannot be null");
            if (packet.AppKey != AppKey)
                throw new ArgumentOutOfRangeException("packet", "AppKeys did not match");
            if (packet.ID != ID)
                throw new ArgumentOutOfRangeException("packet", "ID did not match");
            if (packet.Port != Port)
                throw new ArgumentOutOfRangeException("packet", "port did not match");
            if (!listenEndpoint.Equals(endPoint))
                throw new ArgumentOutOfRangeException("listenEndpoint", "listenEndpoint did not match");
            if (TemporaryDocument != packet.TemporaryDocument)
                throw new ArgumentOutOfRangeException("packet", "temporaryDocument did not match");

            bool changed = false;
            if (changed = (packet.ClientCount != ClientCount))
                ClientCount = packet.ClientCount;
            if (changed = (packet.MaxClients != MaxClients))
                MaxClients = packet.MaxClients;
            if (changed = (packet.RequiresPassword != RequiresPassword))
                RequiresPassword = packet.RequiresPassword;
            if (changed = (!Name.Equals(packet.Name)))
                Name = packet.Name ?? "";
            lastUpdateTimer.Reset();
            if (changed)
                OnUpdated?.Invoke(this);
        }

        /////////////////////////////////////////////////////////////////////
        // PING
        /////////////////////////////////////////////////////////////////////

        internal void UpdatePing()
        {
            if (!active || endPoint == null || pingThread != null)
                return;
            pingThread = new Thread(() =>
            {
                Ping pinger = new Ping();
                try
                {
                    PingReply reply = pinger.Send(endPoint.Address);
                    if (reply.Status == IPStatus.Success && active)
                        Ping = (uint)reply.RoundtripTime;
                }
                catch (Exception) { }
                lastPingTimer.Reset();
                pingThread = null;
            });
            pingThread.Name = "OTEX ServerDescription Ping";
            pingThread.Start();
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears all subscriptions to event listeners.
        /// </summary>
        internal void ClearEventListeners()
        {
            OnInactive = null;
            OnUpdated = null;
        }
    }
}
