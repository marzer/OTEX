using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Guid Sender
        {
            get { return sender; }
        }
        private Guid sender;

        /// <summary>
        /// Index of this packet's sequence. Should be unique per-sender.
        /// </summary>
        public ulong SequenceIndex
        {
            get { return sequenceIndex; }
        }
        private ulong sequenceIndex;

        /// <summary>
        /// Number of packets in the current packet sequence.
        /// </summary>
        public uint SequenceLength
        {
            get { return sequenceLength; }
        }
        private uint sequenceLength;

        /// <summary>
        /// Index of this packet in the current packet sequence.
        /// </summary>
        public uint SequenceOffset
        {
            get { return sequenceOffset; }
        }
        private uint sequenceOffset;

        /// <summary>
        /// Payload type.
        /// All payloads in a sequence should be of the same type otherwise undefined behaviour may result.
        /// </summary>
        public uint PayloadType
        {
            get { return payloadType; }
        }
        private uint payloadType;

        /// <summary>
        /// This packet's fragment of the sequence's serialized payload data.
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
        /// <param name="sequenceIndex">Index of this packet's sequence. Should be unique per-sender.</param>
        /// <param name="sequenceLength">Number of packets in the current packet sequence.</param>
        /// <param name="sequenceOffset">Index of this packet in the current packet sequence.</param>
        /// <param name="payloadType">Unique type ID for this packet's payload.</param>
        /// <param name="payload">Payload. Must be null or [a fragment of] the serialized payload data.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="ArgumentException" />
        public Packet(Guid sender, ulong sequenceIndex, uint sequenceLength, uint sequenceOffset, uint payloadType, byte[] payload = null)
        {
            if (sequenceLength == 0)
                throw new ArgumentOutOfRangeException("sequence length must be at least 1");
            if (sequenceOffset >= sequenceLength)
                throw new ArgumentOutOfRangeException("sequence offset must be less than sequence length");
            this.sender = sender;
            this.sequenceIndex = sequenceIndex;
            this.sequenceLength = sequenceLength;
            this.sequenceOffset = sequenceOffset;
            this.payloadType = payloadType;
            this.payload = payload;
        }
    }
}
