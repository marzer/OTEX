using System;

namespace OTEX.Packets
{
    /// <summary>
    /// Disconnection request packet sent from an OTEX client to server (or vice versa).
    /// </summary>
    [Serializable]
    internal sealed class DisconnectionRequest : IPacketPayload
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 4;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a disconnection request packet.
        /// </summary>
        public DisconnectionRequest()
        {
            //
        }
    }
}
