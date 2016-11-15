using System;

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
        /// Session ID of the client sending the connection request.
        /// </summary>
        public Guid ClientID
        {
            get { return clientID; }
        }
        private Guid clientID;

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
        /// <param name="clientID">Client's session ID.</param>
        /// <param name="metadata">Initial metadata for the client, if any.</param>
        /// <param name="password">Password for the server, if requred.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public ConnectionRequest(Guid clientID, byte[] metadata = null, Password password = null)
        {
            if ((this.clientID = clientID).Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("clientID", "clientID cannot be Guid.Empty");
            if (metadata != null && metadata.LongLength >= Client.MaxMetadataSize)
                throw new ArgumentOutOfRangeException("metadata",
                    string.Format("metadata byte arrays may not be longer than {0} bytes", Client.MaxMetadataSize));
            this.password = password;
            this.metadata = metadata != null && metadata.Length > 0 ? metadata : null;
        }
    }
}
