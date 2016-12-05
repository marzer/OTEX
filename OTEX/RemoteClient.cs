using OTEX.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// Server-side representation of a client.
    /// Also serialized and sent to new clients as part of the initial ConnectionResponse packet.
    /// </summary>
    [Serializable]
    public sealed class RemoteClient
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////
                
        /// <summary>
        /// Client's ID.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// Staging list for storing outgoing operations.
        /// </summary>
        [NonSerialized]
        internal readonly Dictionary<Guid, List<Operation>> OutgoingOperations;

        /// <summary>
        /// Metadata attached to this client, if any.
        /// </summary>
        public byte[] Metadata
        {
            get { return metadata != null && metadata.Length > 0 ? (byte[])metadata.Clone() : null; }
            internal set { metadata = value != null && value.Length > 0 ? (byte[])value.Clone() : null; }
        }
        internal byte[] metadata = null;

        /// <summary>
        /// Staging list for storing outgoing metadata updates.
        /// </summary>
        [NonSerialized]
        internal readonly Dictionary<Guid, RemoteClient> OutgoingMetadata;

        /// <summary>
        /// Has this client been kicked from the server? This will be true if the server calls Ban()
        /// or Kick() with this client's ID while the server is running.
        /// </summary>
        [NonSerialized]
        internal volatile bool Kicked = false;

        /// <summary>
        /// The packet stream object for this client.
        /// </summary>
        [NonSerialized]
        internal readonly PacketStream Stream;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a RemoteClient server-side.
        /// </summary>
        /// <param name="requestPacket">The connection request packet from the new remote client.</param>
        /// <param name="stream">The packet stream object created from the corresponding TcpClient.</param>
        /// <exception cref="ArgumentNullException" />
        internal RemoteClient(ConnectionRequest requestPacket, PacketStream stream)
        {
            if (requestPacket == null)
                throw new ArgumentNullException("requestPacket");
            ID = requestPacket.ClientID;
            Stream = stream ?? throw new ArgumentNullException("stream");
            Metadata = requestPacket.Metadata;
            OutgoingOperations = new Dictionary<Guid, List<Operation>>();
            OutgoingMetadata = new Dictionary<Guid, RemoteClient>();
        }

        /// <summary>
        /// Creates a RemoteClient client-side.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" />
        internal RemoteClient(Guid id, byte[] metadata)
        {
            if (id == Guid.Empty)
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
            ID = id;
            this.metadata = metadata != null && metadata.Length > 0 ? metadata : null;
            Stream = null;
            OutgoingMetadata = null;
            OutgoingOperations = null;
        }
    }
}
