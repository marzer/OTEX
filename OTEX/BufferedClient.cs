using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace OTEX
{
    /// <summary>
    /// BufferedClient class for the OTEX framework. Unlike the regular client, which requires you
    /// to manually calculate operations, the BufferedClient keeps a local cache and generates
    /// operations based on a diff algorithm. Less flexible than Client, easier to get going "in a pinch".
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
        /// Triggered when a remote client updates it's metadata.
        /// Do not call any of this object's methods from this callback or you may deadlock!
        /// </summary>
        public event Action<IClient, Guid, byte[]> OnMetadataUpdated;

        /// <summary>
        /// Triggered when the client is disconnected from an OTEX server.
        /// Boolean parameter is true if the disconnection was forced by the server.
        /// </summary>
        public event Action<IClient, bool> OnDisconnected;

        /// <summary>
        /// Triggered when the buffered client's internal text has been changed.
        /// Bool parameter is true if the change was from a remote client.
        /// </summary>
        public event Action<BufferedClient, bool> OnDocumentChanged;

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
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.Connected;
            }
        }

        /// <summary>
        /// IP Address of server.
        /// </summary>
        public IPAddress ServerAddress
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.ServerAddress;
            }
        }

        /// <summary>
        /// Server's listen port.
        /// </summary>
        public ushort ServerPort
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.ServerPort;
            }
        }

        /// <summary>
        /// Path of the file being edited on the server.
        /// </summary>
        public string ServerFilePath
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.ServerFilePath;
            }
        }

        /// <summary>
        /// The friendly name of the server.
        /// </summary>
        public string ServerName
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.ServerName;
            }
        }

        /// <summary>
        /// ID of server.
        /// </summary>
        public Guid ServerID
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return client.ServerID;
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
        /// Internal client this BufferedClient is wrapping.
        /// </summary>
        private readonly Client client;

        /// <summary>
        /// Contents of the document. Setting this property will perform a diff comparision
        /// between the new value and the old value, and send off OT operations as necessary.
        /// </summary>
        public string Document
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                return document;
            }
            set
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.BufferedClient");
                if (!Connected)
                    throw new InvalidOperationException("cannot update document without being connected");
                var input = value ?? "";

                lock (documentLock)
                {
                    if (isDisposed)
                        throw new ObjectDisposedException("OTEX.BufferedClient");
                    if (!Connected)
                        throw new InvalidOperationException("cannot update document without being connected");

                    //check for equality
                    if (input.Equals(document))
                        return;

                    //do diff on two versions of text
                    var diffs = Diff.Calculate(document.ToCharArray(), input.ToCharArray());

                    //convert changes into operations
                    int position = 0;
                    foreach (var diff in diffs)
                    {
                        //skip unchanged characters
                        position = Math.Min(diff.InsertStart, document.Length);

                        //process a deletion
                        if (diff.DeleteLength > 0)
                            client.Delete((uint)position, (uint)diff.DeleteLength);

                        //process an insertion
                        if (position < (diff.InsertStart + diff.InsertLength))
                            client.Insert((uint)position, document.Substring(position, diff.InsertLength));
                    }

                    //update value fire event
                    document = input;
                    OnDocumentChanged?.Invoke(this, false);

                }
                
            }
        }
        private volatile string document = "";
        private readonly object documentLock = new object();

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX buffered client.
        /// </summary>
        /// <param name="guid">Session ID for this client. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public BufferedClient(Guid? guid = null)
        {
            client = new Client(guid);
            client.OnConnected += (c) => { OnConnected?.Invoke(this); };
            client.OnDisconnected += (c,forced) => { OnDisconnected?.Invoke(this, forced); };
            client.OnMetadataUpdated += (c,id,md) => { OnMetadataUpdated?.Invoke(this,id,md); };
            client.OnThreadException += (c, ex) => { NotifyException(ex); };
            client.OnRemoteOperations += (c, ops) =>
            {               
                lock (documentLock)
                {
                    //execute operations
                    string newDocument = document;
                    foreach (var op in ops)
                        if (!op.IsNoop)
                            newDocument = op.Execute(newDocument);

                    //check equality (might have been all no-ops)
                    if (document.Equals(newDocument))
                        return;

                    //update value fire event
                    document = newDocument;
                    OnDocumentChanged?.Invoke(this, true);
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
        /// <param name="metadata">Client-specific application data to send to the server.</param>
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
        public void Connect(IPAddress address, ushort port = Server.DefaultPort, Password password = null, byte[] metadata = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.BufferedClient");
            client.Connect(address, port, password, metadata);
        }

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="serverDescription">ServerDescription for an OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        /// <param name="metadata">Client-specific application data to send to the server.</param>
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
        public void Connect(ServerDescription serverDescription, Password password = null, byte[] metadata = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.BufferedClient");
            client.Connect(serverDescription.EndPoint, password, metadata);
        }

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="endpoint">IP Endpoint (address and port) of the OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        /// <param name="metadata">Client-specific application data to send to the server.</param>
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
        public void Connect(IPEndPoint endpoint, Password password = null, byte[] metadata = null)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.BufferedClient");
            client.Connect(endpoint, password, metadata);
        }

        /////////////////////////////////////////////////////////////////////
        // SEND CLIENT-SPECIFIC APPLICATION DATA (METADATA)
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Updates this client's application-specific metadata, sending it to the server
        /// which then ensures the new version is receieved by all other clients.
        /// </summary>
        /// <param name="metadata">This client's metadata. Can be null ("I don't have any metadata").</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="InvalidOperationException" />
        public void Metadata(byte[] metadata)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.BufferedClient");
            client.Metadata(metadata);
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
        }

    }
}
