using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marzersoft;

namespace OTEX.Packets
{
    /// <summary>
    /// Connection request packet sent from an OTEX client to server.
    /// </summary>
    [Serializable]
    internal sealed class ConnectionRequest : IPacketPayload
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 2;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /// <summary>
        /// Password for server (if the server requires one).
        /// </summary>
        public Password Password
        {
            get { return password; }
        }
        private Password password;

        /// <summary>
        /// Initial metadata for the client.
        /// </summary>
        public byte[] Metadata
        {
            get { return metadata; }
        }
        private byte[] metadata;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a connection request packet.
        /// </summary>
        /// <param name="password">Password for the server, if requred.</param>
        /// <param name="metadata">Initial metadata for the client.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public ConnectionRequest(Password password = null, byte[] metadata = null)
        {
            if (metadata != null && metadata.Length > ClientMetadata.MaxSize)
                throw new ArgumentOutOfRangeException("metadata",
                    string.Format("metadata.Length may not be greater than {0}.", ClientMetadata.MaxSize));
            this.password = password;
            this.metadata = metadata;
        }
    }
}
