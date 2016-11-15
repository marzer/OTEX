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
        public uint PayloadType
        {
            get { return payloadType; }
        }
        private uint payloadType;

        /// <summary>
        /// This packet's serialized payload data.
        /// </summary>
        public byte[] Payload
        {
            get { return payload; }
        }
        private byte[] payload;

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
            this.payloadType = payloadType;
            this.payload = payload != null && payload.Length > 0 ? payload : null;
        }
    }
}
