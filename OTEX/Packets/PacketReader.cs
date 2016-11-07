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
    /// Reads OTEX packets from a TCP stream.
    /// </summary>
    public class PacketReader
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        private byte[] buffer = new byte[65535];
        private readonly NetworkStream stream = null;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a PacketReader for parsing OTEX packets from a network stream.
        /// </summary>
        /// <param name="stream">The NetworkStream to read.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="IOException" />
        public PacketReader(NetworkStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream cannnot be null");
            if (!stream.CanRead)
                throw new IOException("stream does not support reading");
            this.stream = stream;
        }

        /////////////////////////////////////////////////////////////////////
        // READING PACKETS
        /////////////////////////////////////////////////////////////////////

        public Packet Read()
        {
            //read size
            if (stream.Read(buffer, 0, 4) == 0)
                return null;
            var size = BitConverter.ToInt32(buffer, 0);

            //expand buffer if necessary
            if (buffer.Length < size)
                buffer = new byte[size];

            //read packet
            int offset = 0;
            var remaining = size;
            while (remaining > 0)
            {
                var readAmount = stream.Read(buffer, offset, remaining);
                remaining -= readAmount;
                offset += readAmount;
            }

            //deserialize packet
            byte[] serializedPacket = new byte[size];
            Array.Copy(buffer, serializedPacket, size);
            return serializedPacket.Deserialize<Packet>();
        }
    }
}
