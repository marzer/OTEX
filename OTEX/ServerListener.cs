using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Marzersoft;
using System.IO;

namespace OTEX
{
    /// <summary>
    /// Class responsible for listening for the availability of public OTEX servers.
    /// </summary>
    public sealed class ServerListener : Node, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when a new server is added to the Servers collection.
        /// Subscribe to the events of the ServerDescription object itself to get updates about it.
        /// </summary>
        public event Action<ServerListener, ServerDescription> OnServerAdded;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// has this server listener been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return isDisposed; }
        }
        private volatile bool isDisposed = false;

        /// <summary>
        /// Does this server listener routinely update pings at regular intervals?
        /// If false, it only attempts to determine ping to a server the first time it learns of it.
        /// </summary>
        public bool AutoPing { get; private set; }

        /// <summary>
        /// Master thread for listening for new servers.
        /// </summary>
        private Thread thread = null;

        /// <summary>
        /// List of all known active servers.
        /// </summary>
        private readonly Dictionary<IPEndPoint, ServerDescription> activeServers
            = new Dictionary<IPEndPoint, ServerDescription>();

        /// <summary>
        /// Get a list of all active servers. Do not keep this list; it is a snapshot of the internal
        /// collection at the moment of the call. If you want regular updates, subscribe to OnServerAdded.
        /// </summary>
        public List<ServerDescription> Servers
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.ServerListener");
                lock (activeServers)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.ServerListener");
                    return activeServers.Values.ToList();
                }
            }
        }

        /// <summary>
        /// List of IDs this server listener is ignoring.
        /// </summary>
        private readonly Guid[] excludeServers;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX server listener.
        /// </summary>
        /// <param name="key">AppKey for this listener. Will only handle packets from servers with a matching AppKey.</param>
        /// <param name="excludes">A collection of server id's to ignore.</param>
        /// <exception cref="IOException" />
        /// <exception cref="ArgumentNullException" />
        public ServerListener(AppKey key, params Guid[] excludes) : base(key)
        {
            AutoPing = true;
            
            //set ignore list
            excludeServers = excludes != null && excludes.Length > 0 ? (Guid[])excludes.Clone() : null;

            //create udp client
            UdpClient udpClient = null;
            for (int i = Server.AnnouncePorts.First; i <= Server.AnnouncePorts.Last && udpClient == null; ++i)
            {
                try
                {
                    udpClient = new UdpClient(i);
                }
                catch (Exception) { };
            }

            if (udpClient == null)
                throw new IOException(string.Format("Could not acquire a socket on any port in the range {0}."
                    + " (Are there many instances of ServerListener running?)", Server.AnnouncePorts));

            //configure client
            udpClient.EnableBroadcast = true;
            udpClient.AllowNatTraversal(true);

            //create thread
            thread = new Thread(ControlThread);
            thread.Name = "OTEX ServerListener ControlThread";
            thread.IsBackground = false;
            thread.Start(udpClient);
        }

        /////////////////////////////////////////////////////////////////////
        // CONTROL (LOOP) THREAD
        /////////////////////////////////////////////////////////////////////

        private void ControlThread(object uc)
        {
            var udpClient = uc as UdpClient;
            var checkTimer = new Marzersoft.Timer();

            while (!isDisposed)
            {
                Thread.Sleep(250);

                //read all waiting packets
                while (udpClient.Available > 0)
                {
                    //read packet
                    IPEndPoint senderAnnounceEndpoint = null;
                    byte[] packetData = null;
                    if (CaptureException(() => { packetData = udpClient.Receive(ref senderAnnounceEndpoint); }))
                        continue;

                    //deserialize packet
                    ServerDescription packet = null;
                    IPEndPoint senderEndPoint = null;
                    if (CaptureException(() =>
                    {
                        packet = packetData.Deserialize<ServerDescription>();
                        senderEndPoint = new IPEndPoint(senderAnnounceEndpoint.Address, packet.Port);
                    }))
                        continue;

                    //check appkey
                    if (packet.AppKey != AppKey)
                        continue;

                    //check against blacklist
                    if (excludeServers != null && excludeServers.Contains(packet.ID))
                        continue;

                    //process
                    lock (activeServers)
                    {
                        if (!activeServers.TryGetValue(senderEndPoint, out var knownServer))
                        {
                            activeServers[senderEndPoint] = knownServer = new ServerDescription(senderEndPoint, packet);
                            OnServerAdded?.Invoke(this, knownServer);
                        }
                        else
                            CaptureException(() => { knownServer.Update(senderEndPoint, packet); });
                    }
                }

                //periodically check server inactive state and update pings
                if (checkTimer.Seconds >= 1.0)
                {
                    lock (activeServers)
                    {
                        //cull inactive servers
                        var inactiveServers = activeServers
                            .Where((kvp) => { return kvp.Value.LastUpdated >= 10.0; })
                            .ToList();
                        foreach (var inactive in inactiveServers)
                        {
                            activeServers.Remove(inactive.Key);
                            inactive.Value.Active = false; //invokes event
                            inactive.Value.ClearEventListeners();
                        }

                        //update pings
                        if (AutoPing)
                        {
                            foreach (var active in activeServers)
                            {
                                if (active.Value.LastPinged >= 10.0)
                                    active.Value.UpdatePing();
                            }
                        }
                    }
                    checkTimer.Reset();
                }
            }

            //close listener
            CaptureException(() => { udpClient.Close(); });
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes the ServerListener, terminating internal threads and releasing resources.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            if (thread != null)
            {
                thread.Join();
                thread = null;
            }
            ClearEventListeners();
            activeServers.Clear();
        }

        /// <summary>
        /// Clears all subscriptions to event listeners
        /// </summary>
        protected override void ClearEventListeners()
        {
            base.ClearEventListeners();
            foreach (var kvp in activeServers)
                kvp.Value.ClearEventListeners();
            OnServerAdded = null;
        }
    }
}
