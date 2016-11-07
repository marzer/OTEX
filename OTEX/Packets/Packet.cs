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
        public Guid Sender
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
        private Packet(Guid sender, uint payloadType, byte[] payload = null)
        {
            this.sender = sender;
            this.payloadType = payloadType;
            this.payload = payload;
        }

        /////////////////////////////////////////////////////////////////////
        // SENDING A PACKET
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sends an object to an OTEX node as an OTEX packet.
        /// </summary>
        /// <typeparam name="T">Payload object type. Must have the [Serializable] attribute.</typeparam>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="sender">ID of the node sending this packet.</param>
        /// <param name="data">Payload object.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        public static void Send<T>(NetworkStream stream, Guid sender, T data) where T : IPacketPayload
        {
            if (stream == null)
                throw new ArgumentNullException("stream cannot be null");
            if (data == null)
                throw new ArgumentNullException("data cannot be null");
            if (!typeof(T).IsSerializable)
                throw new ArgumentException("data type must have the [Serializable] attribute");

            //packet
            Packet packet = new Packet(sender, data.PacketPayloadType, data.Serialize());
            var serializedPacket = packet.Serialize();

            //send length
            var serializedLength = BitConverter.GetBytes(serializedPacket.Length);
            stream.Write(serializedLength, 0, serializedLength.Length);

            //send data
            stream.Write(serializedPacket, 0, serializedPacket.Length);
        }
    }
}
