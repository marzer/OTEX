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
        /// AppKey for the client's application. Will only be compatible with servers sharing a matching AppKey.
        /// </summary>
        public AppKey AppKey { get; private set; }

        /// <summary>
        /// Session ID of the client sending the connection request.
        /// </summary>
        public Guid ClientID { get; private set; }

        /// <summary>
        /// Password for server (if the server requires one).
        /// </summary>
        public Password Password { get; private set; }

        /// <summary>
        /// Initial metadata for the client.
        /// </summary>
        public byte[] Metadata { get; private set; }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a connection request packet.
        /// </summary>
        /// <param name="key">AppKey for this node. Will only be compatible with other nodes sharing a matching AppKey.</param>
        /// <param name="clientID">Client's session ID.</param>
        /// <param name="metadata">Initial metadata for the client, if any.</param>
        /// <param name="password">Password for the server, if requred.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public ConnectionRequest(AppKey key, Guid clientID, byte[] metadata = null, Password password = null)
        {
            if ((AppKey = key) == null)
                throw new ArgumentNullException("key");
            if ((ClientID = clientID) == Guid.Empty)
                throw new ArgumentOutOfRangeException("clientID", "clientID cannot be Guid.Empty");
            if (metadata != null && metadata.Length > 0)
            {
                if (metadata.LongLength >= Client.MaxMetadataSize)
                    throw new ArgumentOutOfRangeException("metadata",
                        string.Format("metadata byte arrays may not be longer than {0} bytes", Client.MaxMetadataSize));
                Metadata = metadata;
            }
            Password = password;
        }
    }
}
