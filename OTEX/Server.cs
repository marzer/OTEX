using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Marzersoft;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using OTEX.Packets;
using System.Text;

namespace OTEX
{
    /// <summary>
    /// Server class for the OTEX framework.
    /// </summary>
    public sealed class Server : Node, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when the server is successfully started.
        /// </summary>
        public event Action<Server> OnStarted;

        /// <summary>
        /// Triggered when a new client connects.
        /// </summary>
        public event Action<Server, RemoteClient> OnClientConnected;

        /// <summary>
        /// Triggered when a client disconnects.
        /// </summary>
        public event Action<Server, RemoteClient> OnClientDisconnected;

        /// <summary>
        /// Triggered when the server is stopped.
        /// </summary>
        public event Action<Server> OnStopped;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default port used by the OTEX system for connections between client and server.
        /// </summary>
        public const ushort DefaultPort = 55550;

        /// <summary>
        /// The port range used to annouce the presence of public servers.
        /// </summary>
        public static readonly PortRange AnnouncePorts = new PortRange(55561, 55564);

        /// <summary>
        /// has this server been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return isDisposed; }
        }
        private volatile bool isDisposed = false;

        /// <summary>
        /// The currently running session.
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
        /// Is the server currently running?
        /// </summary>
        public bool Running
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                return session != null;
            }
        }

        /// <summary>
        /// Lock object for documents, operations and clients.
        /// </summary>
        private readonly object stateLock = new object();

        /// <summary>
        /// Master thread for listening for new clients and controlling sub-threads.
        /// </summary>
        private Thread thread = null;
        
        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX server.
        /// </summary>
        /// <param name="key">AppKey for this server. Will only be compatible with other nodes sharing a matching AppKey.</param>
        /// <param name="guid">Session ID for this server. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public Server(AppKey key, Guid? guid = null) : base(key, guid)
        {
            //
        }

        /////////////////////////////////////////////////////////////////////
        // STARTING THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Starts the server. Does nothing if the server is already running.
        /// </summary>
        /// <param name="session">Configuration of this session.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="IOException" />
        public void Start(Session session)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Server");

            if (this.session == null)
            {
                if (session == null)
                    throw new ArgumentNullException("session");

                lock (sessionLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.Server");

                    if (this.session != null)
                        return;

                    //copy session (do not keep reference to input)
                    session = new Session(session);
                    session.ID = ID;

                    //attempt to initialize session (throws on failure)
                    session.Initialize();

                    //network initialization
                    TcpListener tcpListener = null;
                    UdpClient announcer = null;
                    try
                    {
                        //create tcp listener
                        tcpListener = new TcpListener(IPAddress.IPv6Any, session.Port);
                        tcpListener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                        tcpListener.AllowNatTraversal(true);
                        tcpListener.Start();

                        if (session.Public)
                        {
                            //create udp client
                            announcer = new UdpClient();
                            announcer.EnableBroadcast = true;
                            announcer.AllowNatTraversal(true);
                        }
                    }
                    catch (Exception)
                    {
                        if (tcpListener != null)
                        {
                            try { tcpListener.Stop(); } catch (Exception) { };
                        }
                        if (announcer != null)
                        {
                            try { announcer.Close(); } catch (Exception) { };
                        }
                        throw;
                    }

                    //session initialized OK, store it
                    this.session = session;
                    killSession = false;

                    //create thread
                    thread = new Thread(ControlThread);
                    thread.Name = "OTEX Server ControlThread";
                    thread.IsBackground = false;
                    thread.Start(new object[] { tcpListener, announcer });

                    //fire event
                    OnStarted?.Invoke(this);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONTROL (LOOP) THREAD
        /////////////////////////////////////////////////////////////////////

        private void ControlThread(object nobjs)
        {
            var networkObjects = nobjs as object[];
            var tcpListener = networkObjects[0] as TcpListener;
            var announcer = networkObjects[1] as UdpClient;
            var clientThreads = new List<Thread>();
            var flushTimer = new Marzersoft.Timer();
            var announceTimer = new Marzersoft.Timer();
            var announceEndpoints = new List<IPEndPoint>();

            while (session != null && !killSession)
            {
                Thread.Sleep(1);

                //accept the connection
                while (tcpListener.Pending())
                {
                    //get client
                    TcpClient tcpClient = null;
                    if (CaptureException(() => { tcpClient = tcpListener.AcceptTcpClient(); }))
                        continue;

                    //create client thread
                    clientThreads.Add(new Thread(ClientThread));
                    clientThreads.Last().Name = "OTEX Server ClientThread";
                    clientThreads.Last().IsBackground = false;
                    clientThreads.Last().Start(tcpClient);
                }

                //announce
                if (session.Public && announceTimer.Seconds >= 1.0)
                {
                    CaptureException(() =>
                    {
                        //create endpoints if they don't exist
                        if (announceEndpoints.Count == 0)
                            for (int i = AnnouncePorts.First; i <= AnnouncePorts.Last; ++i)
                                announceEndpoints.Add(new IPEndPoint(IPAddress.Broadcast, i));

                        //serialize a server description of the current state
                        var announceData = (new ServerDescription(this)).Serialize();

                        //broadcast to the port range
                        foreach (IPEndPoint ep in announceEndpoints)
                            announcer.Send(announceData, announceData.Length, ep);
                    });
                    announceTimer.Reset();
                }

                //flush file contents to disk periodically
                if (flushTimer.Seconds >= 15.0)
                {
                    lock (stateLock)
                        CaptureException(() => { session.FlushDocuments(); });
                    flushTimer.Reset();
                }
            }

            //stop listeners and announcer
            CaptureException(() => { tcpListener.Stop(); });
            if (announcer != null)
                CaptureException(() => { announcer.Close(); });

            //wait for client threads to close
            foreach (Thread thread in clientThreads)
                thread.Join();

            //final flush to disk
            CaptureException(() => { session.FlushDocuments(); });
        }

        /////////////////////////////////////////////////////////////////////
        // PER-CLIENT THREAD
        /////////////////////////////////////////////////////////////////////

        private void ClientThread(object tcpClientObject)
        {
            TcpClient tcpClient = tcpClientObject as TcpClient;

            //create packet stream
            PacketStream stream = null;
            if (CaptureException(() => { stream = new PacketStream(tcpClient); }))
                return;

            //read from stream
            bool clientSideDisconnect = false;
            RemoteClient client = null;
            while (session != null && !killSession && stream.Connected)
            {
                //check kicked flag
                if (client != null && client.Kicked)
                    break;
                
                //check if client has sent data
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(1);
                    continue;
                }

                //read incoming packet
                Packet packet = null;
                if (CaptureException(() => { packet = stream.Read(); }))
                    break;

                //check kicked flag again (in case it changed during the packet read)
                if (client != null && client.Kicked)
                    break;

                //is this the first packet from a new client?
                if (client == null)
                {
                    //check packet type
                    //if it's not a connection request, abort (the client is in a bad state)
                    if (packet.PayloadType != ConnectionRequest.PayloadType)
                    {
                        //let client know we're cutting them off
                        CaptureException(() =>
                        {
                            stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.InvalidState));
                        });
                        break;
                    }

                    //deserialize packet
                    ConnectionRequest request = null;
                    if (CaptureException(() => { request = packet.Payload.Deserialize<ConnectionRequest>(); }))
                        break;

                    //check app key
                    if (AppKey != request.AppKey)
                    {
                        CaptureException(() =>
                        {
                            stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.DifferentAppKey));
                        });
                        break;
                    }

                    //check password
                    if (session.Password != request.Password)
                    {
                        CaptureException(() =>
                        {
                            stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.IncorrectPassword));
                        });
                        break;
                    }

                    lock (stateLock)
                    {
                        //check ban list
                        if (session.BanList.Contains(request.ClientID))
                        {
                            CaptureException(() =>
                            {
                                stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.Banned));
                            });
                            break;
                        }

                        //duplicate id (already connected... shouldn't happen?)
                        if (session.Clients.TryGetValue(request.ClientID, out var cl))
                        {
                            CaptureException(() =>
                            {
                                stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.DuplicateGUID));
                            });
                            break;
                        }

                        //too many connections already
                        if (session.Clients.Count >= session.ClientLimit)
                        {
                            CaptureException(() =>
                            {
                                stream.Write(new ConnectionResponse(ConnectionResponse.ResponseCode.SessionFull));
                            });
                            break;
                        }

                        //send response with session state snapshot
                        if (!CaptureException(() => { stream.Write(new ConnectionResponse(session)); }))
                        {
                            //create RemoteClient data for this client
                            client = new RemoteClient(request, stream);
                            foreach (var kvp in session.documents)
                                client.OutgoingOperations[kvp.Key] = new List<Operation>();

                            //send connection notification to other clients
                            var outgoingPacket = PacketStream.Prepare(new RemoteConnection(client));
                            foreach (var kvp in session.Clients)
                                if (kvp.Value.Stream.Connected)
                                    CaptureException(() => { kvp.Value.Stream.Write(outgoingPacket); });

                            //add new client to session
                            session.Clients[client.ID] = client;
                        }
                        else
                            break; //sending ConnectionResponse failed (connection broken)
                    }

                    //notify
                    OnClientConnected?.Invoke(this, client);
                }
                else //initial handshake sync has been performed, handle normal requests
                {
                    switch (packet.PayloadType)
                    {
                        case DisconnectionRequest.PayloadType: //disconnection request from client
                            clientSideDisconnect = true;
                            break;

                        case ClientUpdate.PayloadType: //normal update request
                            {
                                //deserialize operation request
                                ClientUpdate incoming = null;
                                if (CaptureException(() => { incoming = packet.Payload.Deserialize<ClientUpdate>(); }))
                                    break;

                                //lock operation lists (3a)
                                lock (stateLock)
                                {
                                    //if this oplist is not an empty request
                                    if (incoming.Operations != null && incoming.Operations.Count > 0)
                                    {
                                        foreach (var kvp in incoming.Operations)
                                        {
                                            if (kvp.Value != null && kvp.Value.Count > 0
                                                && session.documents.TryGetValue(kvp.Key, out var doc))
                                            {
                                                //perform SLOT(OB,SIB) (3b)
                                                Operation.SymmetricLinearTransform(kvp.Value,
                                                        client.OutgoingOperations[doc.ID]);

                                                //append incoming ops to master and to all other outgoing (3c)
                                                doc.MasterOperations.AddRange(kvp.Value);
                                                foreach (var ckvp in session.Clients)
                                                    if (ckvp.Key != client.ID)
                                                        ckvp.Value.OutgoingOperations[doc.ID].AddRange(kvp.Value);
                                            }
                                        }
                                    }

                                    //handle incoming metadata
                                    if (incoming.Metadata != null && incoming.Metadata.Count > 0)
                                    {
                                        //get metadata array
                                        if (!incoming.Metadata.TryGetValue(client.ID, out var md))
                                            break; //shouldn't happen; clients only send their own

                                        //compare it to existing metadata
                                        if ((md.metadata == null && client.metadata != null)
                                            || (md.metadata != null && (client.metadata == null
                                                || !md.metadata.MemoryEquals(client.metadata))))
                                        {
                                            //update client metadata
                                            client.metadata = md.metadata;

                                            //add to staging lists for other clients
                                            foreach (var kvp in session.Clients)
                                                if (!kvp.Key.Equals(client.ID))
                                                    kvp.Value.OutgoingMetadata[client.ID] = md;
                                        }
                                    }

                                    //send response
                                    CaptureException(() => { stream.Write(
                                        new ClientUpdate(client.OutgoingOperations, client.OutgoingMetadata)); });

                                    //clear outgoing packet list (3d)
                                    foreach (var kvp in client.OutgoingOperations)
                                        kvp.Value.Clear();

                                    //clear outgoing metadata list
                                    client.OutgoingMetadata.Clear();
                                }
                            }
                            break;
                    }

                    if (clientSideDisconnect)
                        break;
                }
            }

            //remove the internal data for this client
            if (client != null)
            {
                bool disconnected = false;
                lock (stateLock)
                {
                    disconnected = session.Clients.Remove(client.ID);

                    //send disconnection notification to other clients
                    var packet = PacketStream.Prepare(new RemoteDisconnection(client.ID));
                    foreach (var kvp in session.Clients)
                    {
                        kvp.Value.OutgoingMetadata.Remove(client.ID);
                        if (kvp.Value.Stream.Connected)
                            CaptureException(() => { kvp.Value.Stream.Write(packet); });
                    }
                }
                if (disconnected)
                {
                    //if the client has not requested a disconnection themselves, send them one
                    if (!clientSideDisconnect)
                        CaptureException(() => { stream.Write(new DisconnectionRequest()); });
                    OnClientDisconnected?.Invoke(this, client);
                }
            }

            //close stream and tcp client
            stream.Dispose();
            tcpClient.Close();
        }

        /////////////////////////////////////////////////////////////////////
        // BANNING/KICKING CLIENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Kicks a client from the server, optionally banning them.
        /// </summary>
        /// <param name="id">The id of the client to kick</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ObjectDisposedException" />
        public void Kick(Guid id, bool ban = false)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.Server");
            if (id == Guid.Empty)
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
            if (session == null || killSession)
                throw new InvalidOperationException("Server is not running.");

            lock (stateLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.Server");
                if (session == null || killSession)
                    throw new InvalidOperationException("Server is not running.");

                if (session.Clients.TryGetValue(id, out RemoteClient client))
                    client.Kicked = true; //will cause it to be disconnected by the main thread

                if (ban)
                {
                    session.BanList.Add(id);
                    session.FlushBanList();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // STOPPING THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stops the server. 
        /// </summary>
        public void Stop()
        {
            if (session != null)
            {
                lock (sessionLock)
                {
                    if (session != null)
                    {
                        killSession = true;
                        if (thread != null)
                        {
                            thread.Join();
                            thread = null;
                        }
                        session = null;
                        OnStopped?.Invoke(this);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes this server, stopping it (if it was running) and releasing resources.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            Stop();
            ClearEventListeners();
        }

        /// <summary>
        /// Clears all subscriptions to event listeners
        /// </summary>
        protected override void ClearEventListeners()
        {
            base.ClearEventListeners();
            OnClientConnected = null;
            OnClientDisconnected = null;
            OnStarted = null;
            OnStopped = null;
        }
    }
}
