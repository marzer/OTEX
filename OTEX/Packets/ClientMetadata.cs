using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Packets
{
    /// <summary>
    /// A bundle of client-specific metadata. The server stores the most recent one sent by each client
    /// and forwards them on to each new client when they connect and when the client sends an updated
    /// version. Applications can use this for display name, text colour, etc.
    /// </summary>
    [Serializable]
    internal sealed class ClientMetadata : IPacketPayload, IClientMetadata
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maximum size of a metadata payload.
        /// </summary>
        public const uint MaxSize = 1024;

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 5;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /// <summary>
        /// Set of metadata for connected clients.
        /// </summary>
        public Dictionary<Guid, byte[]> Metadata
        {
            get { return metadata; }
        }
        private Dictionary<Guid, byte[]> metadata;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a client metadata packet (at the client end).
        /// </summary>
        /// <param name="clientID">Session ID of the client the metadata belongs to.</param>
        /// <param name="metadata">Metadata payload.</param>
        public ClientMetadata(Guid clientID, byte[] metadata = null)
        {
            if (metadata != null && metadata.Length > MaxSize)
                throw new ArgumentOutOfRangeException("metadata",
                    string.Format("metadata.Length may not be greater than {0}.", MaxSize));
            this.metadata = new Dictionary<Guid, byte[]>();
            this.metadata[clientID] = metadata;
        }

        /// <summary>
        /// Construct a client metadata packet (at the server end).
        /// </summary>
        /// <param name="metadata">Metadata payload.</param>
        public ClientMetadata(Dictionary<Guid, byte[]> metadata)
        {
            this.metadata = metadata;
        }
    }
}
