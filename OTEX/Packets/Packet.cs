using System;

namespace OTEX
{
    /// <summary>
    /// Packets sent between OTEX nodes.
    /// </summary>
    [Serializable]
    internal sealed class Packet
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type.
        /// </summary>
        public uint PayloadType { get; private set; }

        /// <summary>
        /// This packet's serialized payload data.
        /// </summary>
        public byte[] Payload { get; private set; }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX packet.
        /// </summary>
        /// <param name="payloadType">Unique type ID for this packet's payload.</param>
        /// <param name="payload">Serialized payload object.</param>
        public Packet(uint payloadType, byte[] payload = null)
        {
            PayloadType = payloadType;
            Payload = payload != null && payload.Length > 0 ? payload : null;
        }
    }
}
