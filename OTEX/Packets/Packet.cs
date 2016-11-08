using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Marzersoft;

namespace OTEX
{
    /// <summary>
    /// Packets sent between OTEX nodes.
    /// </summary>
    [Serializable]
    public sealed class Packet
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Session ID of the node sending the packet.
        /// </summary>
        public Guid SenderID
        {
            get { return sender; }
        }
        private Guid sender;

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
        /// <param name="sender">ID of the node sending this packet.</param>
        /// <param name="payloadType">Unique type ID for this packet's payload.</param>
        /// <param name="payload">Serialized payload object.</param>
        internal Packet(Guid sender, uint payloadType, byte[] payload = null)
        {
            this.sender = sender;
            this.payloadType = payloadType;
            this.payload = payload;
        }
    }
}
