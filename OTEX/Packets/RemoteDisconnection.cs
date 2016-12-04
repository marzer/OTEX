using System;

namespace OTEX.Packets
{
    /// <summary>
    /// Notification that a remote client has disconnected from the server.
    /// </summary>
    [Serializable]
    internal sealed class RemoteDisconnection : IPacketPayload
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

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
        /// Session ID of the disconnecting remote client.
        /// </summary>
        public Guid ClientID { get; private set; }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a remote client disconnection notification.
        /// </summary>
        public RemoteDisconnection(Guid clientID)
        {
            if ((ClientID = clientID) == Guid.Empty)
                throw new ArgumentOutOfRangeException("clientID", "clientID cannot be Guid.Empty");
        }
    }
}
