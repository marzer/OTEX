using OTEX.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marzersoft;

namespace OTEX
{
    /// <summary>
    /// Class responsible for listening for the availability of public OTEX servers.
    /// </summary>
    public class ServerListener : ThreadController, IDisposable
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
        public bool AutoPing
        {
            get { return autoPing; }
        }
        private volatile bool autoPing = true;

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
                lock (activeServers)
                {
                    return activeServers.Values.ToList();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX server listener.
        /// Listens for OTEX server announce packets on ports 55555 - 55560.
        /// </summary>
        /// <exception cref="Exception" />
        public ServerListener()
        {
            //create udp client
            UdpClient udpClient = null;
            for (int i = 55555; i <= 55560 && udpClient == null; ++i)
            {
                try
                {
                    udpClient = new UdpClient(i);
                }
                catch (Exception) { };
            }

            if (udpClient == null)
                throw new Exception("Could not acquire a socket on any port in the range 55555-55560. (Are there many instances of ServerListener running?)");

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

                    //process
                    lock (activeServers)
                    {
                        ServerDescription knownServer = null;
                        if (!activeServers.TryGetValue(senderEndPoint, out knownServer))
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
                        }

                        //update pings
                        if (autoPing)
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
            if (activeServers.Count > 0)
                activeServers.Clear();
        }
    }
}
