using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marzersoft;

namespace OTEX
{
    /// <summary>
    /// Sequence of related packets sent between OTEX nodes (used for chunking large data).
    /// </summary>
    public sealed class PacketSequence
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Session ID of the node sending the packets.
        /// </summary>
        public readonly Guid Sender;

        /// <summary>
        /// Index of this sequence. Should be unique per-sender.
        /// </summary>
        public readonly ulong SequenceIndex;

        /// <summary>
        /// Payload type.
        /// </summary>
        public readonly uint PayloadType;

        /// <summary>
        /// Serialized payload data.
        /// </summary>
        public readonly byte[] Payload;

        /// <summary>
        /// Sequence index of last sent packet.
        /// </summary>
        private static long lastSequenceIndex = -1;

        /// <summary>
        /// Max size of a payload before it is divided into chunks (~4 KB, minus a little for NodePacket stuff)
        /// </summary>
        private static readonly long PayloadFragmentSize = 3900;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a packet sequence by reading from a network stream.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="IOException" />
        /// <exception cref="InvalidDataException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        public PacketSequence(NetworkStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream cannot be null");

            byte[] buffer = new byte[65535];
            List<Packet> packets = new List<Packet>();
            Packet first = null;
            uint count = 0;
            long size = 0;
            while (true)
            {
                //wait for data
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(1);
                    continue;
                }

                //read from stream
                byte[] data = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    while (true)
                    {
                        int read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                        {
                            data = ms.ToArray();
                            break;
                        }
                        ms.Write(buffer, 0, read);
                    }
                }

                //deserialize packet
                Packet packet = data.Deserialize<Packet>();

                //sanity-check packet (deserialization does not use constructors so can't trust data implicitly)
                if (packet.SequenceLength == 0)
                    throw new InvalidDataException("sequence length must be at least 1");
                if (packet.SequenceOffset >= packet.SequenceLength)
                    throw new InvalidDataException("sequence offset must be less than sequence length");

                //check packet against sequence
                if (first == null)
                {
                    //set values
                    Sender = packet.Sender;
                    SequenceIndex = packet.SequenceIndex;
                    PayloadType = packet.PayloadType;

                    //initialize array
                    for (uint i = 0; i < packet.SequenceLength; ++i)
                        packets.Add(null);
                    first = packet;
                }
                else
                {
                    //more sanity-checking
                    if (packet.SequenceIndex != SequenceIndex)
                        throw new InvalidDataException("sequence index did not match");
                    if (packet.PayloadType >= PayloadType)
                        throw new InvalidDataException("payload type did not match");
                    if (packet.SequenceOffset >= first.SequenceLength)
                        throw new InvalidDataException("sequence offset must be less than sequence length");
                    if (packets[(int)packet.SequenceOffset] != null)
                        throw new InvalidDataException("sequence offset must be unique");
                }

                //add new packet to array
                packets[(int)packet.SequenceOffset] = packet;
                size += (packet.Payload == null ? 0 : packet.Payload.LongLength);

                //check if we have the whole set
                if ((++count) == first.SequenceLength)
                    break;
            }

            //assemble payload
            if (first.Payload == null)
                Payload = null;
            else
            {
                if (first.SequenceLength == 1)
                    Payload = first.Payload;
                else
                {
                    Payload = new byte[size];
                    long s = 0;
                    for (int i = 0; i < packets.Count; ++i)
                    {
                        packets[i].Payload.CopyTo(Payload, s);
                        s += packets[i].Payload.LongLength;
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // SENDING A PACKET SEQUENCE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sends an object to an OTEX node as a packet sequence.
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
            if (!typeof(T).IsSerializable)
                throw new ArgumentException("data type must have the [Serializable] attribute");

            //payload
            var payloadType = data.PacketPayloadType;
            var payloadData = data.Serialize();
            List<byte[]> payload = new List<byte[]>();
            for (long i = 0; i < payloadData.LongLength; i += PayloadFragmentSize)
            {
                byte[] arr = new byte[Math.Min(PayloadFragmentSize, payloadData.LongLength - i)];
                Array.Copy(payloadData, i, arr, 0, arr.LongLength);
            }
            payloadData = null;

            //sequence index
            ulong idx = (ulong)Interlocked.Increment(ref lastSequenceIndex);

            //write packets
            for (int i = 0; i < payload.Count; ++i)
            {
                Packet packet = new Packet(sender, idx, (uint)payload.Count, (uint)i, payloadType, payload[i]);
                var packetBytes = packet.Serialize();
                stream.Write(packetBytes, 0, packetBytes.Length);
            }
        }
    }
}
