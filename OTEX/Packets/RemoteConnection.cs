using System;

namespace OTEX.Packets
{
    /// <summary>
    /// Notification that a remote client has connected to the server.
    /// </summary>
    [Serializable]
    internal sealed class RemoteConnection : IPacketPayload
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 6;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /// <summary>
        /// RemoteClient data for the connecting client.
        /// </summary>
        public RemoteClient Client { get; private set; }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a remote client disconnection notification.
        /// </summary>
        public RemoteConnection(RemoteClient client)
        {
            Client = client ?? throw new ArgumentNullException("client");
        }
    }
}
