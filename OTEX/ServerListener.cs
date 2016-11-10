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
        /// Master thread for listening for new servers.
        /// </summary>
        private Thread thread = null;

        /// <summary>
        /// List of all known active servers.
        /// </summary>
        private readonly Dictionary<IPEndPoint, ServerDescription> activeServers
            = new Dictionary<IPEndPoint, ServerDescription>();

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX server listener.
        /// Listens for OTEX server announce packets on port 55555.
        /// </summary>
        /// <exception cref="SocketException" />
        public ServerListener()
        {
            //create udp client
            UdpClient udpClient = udpClient = new UdpClient(55555);

            /*
            try
            {
                udpClient = new UdpClient(55555);
                //udpClient.EnableBroadcast = false;
                //udpClient.AllowNatTraversal(true);
            }
            catch (Exception)
            {
                if (udpClient != null)
                {
                    try { udpClient.Close(); } catch (Exception) { };
                }
                throw;
            }
            */

            //create thread
            thread = new Thread(ControlThread);
            thread.IsBackground = false;
            thread.Start(udpClient);
        }

        /////////////////////////////////////////////////////////////////////
        // CONTROL (LOOP) THREAD
        /////////////////////////////////////////////////////////////////////

        private void ControlThread(object uc)
        {
            var udpClient = uc as UdpClient;
            var activeCheckTimer = new Marzersoft.Timer();
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

                //check timeouts of servers
                if (activeCheckTimer.Seconds > 3.0f)
                {
                    lock (activeServers)
                    {
                        var inactiveServers = activeServers
                            .Where((kvp) => { return kvp.Value.LastUpdated >= 10.0; })
                            .ToList();
                        foreach (var inactive in inactiveServers)
                        {
                            activeServers.Remove(inactive.Key);
                            inactive.Value.Active = false; //invokes event
                        }
                    }
                    activeCheckTimer.Reset();
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
