using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace OTEX
{
    /// <summary>
    /// BufferedClient class for the OTEX framework. Unlike the regular client, which requires you
    /// to manually calculate operations, the BufferedClient keeps a local cache and generates
    /// operations based on a diff algorithm. Less flexible than Client, easier to get going "in a pinch".
    /// Better suited to applications which need to send operations upstream rarely/never (e.g. a read-only client).
    /// </summary>
    public sealed class BufferedClient : ThreadController, IClient, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when the client successfully connects to an OTEX server.
        /// </summary>
        public event Action<IClient> OnConnected;

        /// <summary>
        /// Triggered when a remote client connects to the server.
        /// </summary>
        public event Action<IClient, RemoteClient> OnRemoteConnection;

        /// <summary>
        /// Triggered when a remote client updates it's metadata.
        /// </summary>
        public event Action<IClient, RemoteClient> OnRemoteMetadata;

        /// <summary>
        /// Triggered when a remote client in the same session disconnects from the server.
        /// </summary>
        public event Action<IClient, RemoteClient> OnRemoteDisconnection;

        /// <summary>
        /// Triggered when the client is disconnected from an OTEX server.
        /// Boolean parameter is true if the disconnection was forced by the server.
        /// </summary>
        public event Action<IClient, bool> OnDisconnected;

        /// <summary>
        /// Triggered when the buffered client's internal text has been changed.
        /// Guid is the document ID.
        /// Bool parameter is true if the change was from a remote client.
        /// </summary>
        public event Action<BufferedClient, Guid, bool> OnDocumentChanged;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// ID for this node.
        /// </summary>
        public Guid ID
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.ID;
            }
        }

        /// <summary>
        /// AppKey for this node. Will only be compatible with other nodes sharing a matching AppKey.
        /// </summary>
        public AppKey AppKey
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.AppKey;
            }
        }

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
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.Connected;
            }
        }

        /// <summary>
        /// Session information for the current connection.
        /// </summary>
        public ISession Session
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.Session;
            }
        }

        /// <summary>
        /// Time, in seconds, between each request for updates sent to the server
        /// (clamped between 0.5 and 5.0).
        /// </summary>
        public float UpdateInterval
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.UpdateInterval;
            }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                client.UpdateInterval = value;
            }
        }

        /// <summary>
        /// This client's metadata. Setting this value causes it to be sent to the server with the next update.
        /// The server then ensures the new version is received by all other clients during their next updates.
        /// May be set when the client is not connected; this means "send this when I next connect to a server".
        /// 
        /// The getter returns a copy of the internal metadata buffer (if not null); it does not keep a reference
        /// to the original set value.
        /// </summary>
        public byte[] Metadata
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.Metadata;
            }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                client.Metadata = value;
            }
        }

        /// <summary>
        /// Internal client this BufferedClient is wrapping.
        /// </summary>
        private readonly Client client;

        /// <summary>
        /// Contents of the document(s). Setting this property will perform a diff comparision
        /// between the new value and the old value, and send off OT operations as necessary.
        /// </summary>
        public string this[Guid id]
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                if (id == Guid.Empty)
                    throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
                if (documents.Count > 0)
                {
                    lock (documents)
                    {
                        if (documents.Count > 0 && documents.TryGetValue(id, out var document))
                            return document;
                    }
                }
                return null;
            }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                if (!Connected)
                    throw new InvalidOperationException("cannot update document without being connected");
                if (id == Guid.Empty)
                    throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
                var input = value ?? "";

                if (documents.Count > 0)
                {
                    lock (documents)
                    {
                        if (isDisposed)
                            throw new ObjectDisposedException("OTEX.BufferedClient");
                        if (!Connected)
                            throw new InvalidOperationException("cannot update document without being connected");

                        if (documents.Count > 0 && documents.TryGetValue(id, out var document))
                        {
                            //check for equality
                            if (input.Equals(document))
                                return;

                            //do diff on two versions of text
                            var diffs = Diff.Calculate(document.ToCharArray(), input.ToCharArray());

                            //convert changes into operations
                            foreach (var diff in diffs)
                            {
                                //skip unchanged characters
                                uint position = (uint)Math.Min(diff.InsertStart, document.Length);

                                //process a deletion
                                if (diff.DeleteLength > 0)
                                    client.Delete(id, position, diff.DeleteLength);

                                //process an insertion
                                if (position < (diff.InsertStart + diff.InsertLength))
                                    client.Insert(id, position, document.Substring((int)position, (int)diff.InsertLength));
                            }

                            //update value fire event
                            documents[id] = input;
                            OnDocumentChanged?.Invoke(this, id, false);
                            return;
                        }
                    }
                }
                throw new ArgumentOutOfRangeException("id","document ID did not match any document in the session");
            }
        }
        private readonly Dictionary<Guid, string> documents
            = new Dictionary<Guid, string>();

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX buffered client.
        /// </summary>
        /// <param name="key">AppKey for this client. Will only be compatible with other nodes sharing a matching AppKey.</param>
        /// <param name="guid">Session ID for this client. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public BufferedClient(AppKey key, Guid? guid = null)
        {
            client = new Client(key, guid);
            client.OnThreadException += (c, ex) => { NotifyException(ex); };
            client.OnConnected += (c) =>
            {
                lock (documents)
                {
                    documents.Clear();
                    foreach (var kvp in client.Session.Documents)
                        documents[kvp.Key] = "";
                }
                OnConnected?.Invoke(this);
            };
            client.OnRemoteConnection += (c, rc) => { OnRemoteConnection?.Invoke(this, rc); };
            client.OnRemoteMetadata += (c, rc) => { OnRemoteMetadata?.Invoke(this, rc); };
            client.OnRemoteDisconnection += (c, rc) => { OnRemoteDisconnection?.Invoke(this, rc); };
            client.OnDisconnected += (c,forced) => { OnDisconnected?.Invoke(this, forced); };
            client.OnRemoteOperations += (c, doc, ops) =>
            {               
                lock (documents)
                {
                    //execute operations
                    string document = documents[doc];
                    foreach (var op in ops)
                        if (!op.IsNoop)
                            document = op.Execute(document);

                    //check equality (might have been all no-ops)
                    if (documents[doc].Equals(document))
                        return;

                    //update value fire event
                    documents[doc] = document;
                    OnDocumentChanged?.Invoke(this, doc, true);
                }
            };
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
        public void Connect(IPAddress address, ushort port = Server.DefaultPort, Password password = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.BufferedClient");
            client.Connect(address, port, password);
        }

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="serverDescription">ServerDescription for an OTEX server.</param>
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
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.BufferedClient");
            client.Connect(serverDescription.EndPoint, password);
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
                throw new ObjectDisposedException("OTEX.BufferedClient");
            client.Connect(endpoint, password);
        }

        /////////////////////////////////////////////////////////////////////
        // DISCONNECTING FROM THE SERVER
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disconnect from an OTEX server. Does nothing if not already connected.
        /// </summary>
        public void Disconnect()
        {
            client.Disconnect();
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
            client.Dispose();
            ClearEventListeners();
        }

        /// <summary>
        /// Clears all subscriptions to event listeners
        /// </summary>
        protected override void ClearEventListeners()
        {
            base.ClearEventListeners();
            OnConnected = null;
            OnDisconnected = null;
            OnDocumentChanged = null;
            OnRemoteMetadata = null;
            OnRemoteDisconnection = null;
        }
    }
}
