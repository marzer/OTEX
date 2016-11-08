using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marzersoft;

namespace OTEX.Packets
{
    /// <summary>
    /// Reads and writes OTEX packets using a TCPClient.
    /// </summary>
    internal class PacketStream : IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        private byte[] buffer = new byte[4096];
        private TcpClient client = null;
        private NetworkStream stream = null;

        /// <summary>
        /// Has this PacketStream been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return isDisposed; }
        }
        private volatile bool isDisposed = false;

        /// <summary>
        /// Is the tcp client still connected to the remote endpoint?
        /// </summary>
        public bool Connected
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.PacketStream");
                return client.Connected;
            }
        }

        /// <summary>
        /// Is there incoming data waiting to be read?
        /// </summary>
        public bool DataAvailable
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.PacketStream");
                return stream.DataAvailable;
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a PacketStream for reading and writing OTEX packets using TcpClient.
        /// </summary>
        /// <param name="client">The TcpClient to use.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="IOException" />
        public PacketStream(TcpClient client)
        {
            //tcp client
            if (client == null)
                throw new ArgumentNullException("client cannot be null");
            if (!client.Connected)
                throw new IOException("client was not connected");
            this.client = client;

            //get network stream
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                if (!stream.CanRead)
                    throw new IOException("client stream does not support reading");
                
            }
            catch (Exception)
            {
                if (stream != null)
                    stream.Dispose();
                throw;
            }
            this.stream = stream;
        }

        /////////////////////////////////////////////////////////////////////
        // READING PACKETS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Read an OTEX packet object from the TCP stream.
        /// </summary>
        /// <returns>A packet.</returns>
        /// <exception cref="IOException" />
        /// <exception cref="InvalidDataException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        public Packet Read()
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.PacketStream");
            if (!client.Connected)
                throw new IOException("client has been disconnected");

            //read size
            int offset = 0;
            int remaining = 4;
            while (remaining > 0)
            {
                var readAmount = stream.Read(buffer, offset, remaining);
                remaining -= readAmount;
                offset += readAmount;
            }
            var size = BitConverter.ToInt32(buffer, 0);

            //expand buffer if necessary
            if (buffer.Length < size)
                buffer = new byte[size];

            //read packet
            offset = 0;
            remaining = size;
            while (remaining > 0)
            {
                var readAmount = stream.Read(buffer, offset, remaining);
                remaining -= readAmount;
                offset += readAmount;
            }

            //deserialize packet
            byte[] serializedPacket = new byte[size];
            Array.Copy(buffer, serializedPacket, size);
            var packet = serializedPacket.Deserialize<Packet>();

            //check guid
            if (packet.SenderID.Equals(Guid.Empty))
                throw new InvalidDataException("packet.SenderID was Guid.Empty");

            return packet;
        }

        /////////////////////////////////////////////////////////////////////
        // SENDING PACKETS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sends an OTEX packet object over the TCP stream.
        /// </summary>
        /// <typeparam name="T">Payload object type. Must have the [Serializable] attribute.</typeparam>
        /// <param name="senderID">ID of the node sending this packet.</param>
        /// <param name="data">Payload object.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        /// <exception cref="ObjectDisposedException" />
        public void Write<T>(Guid senderID, T data) where T : IPacketPayload
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.PacketStream");
            if (!client.Connected)
                throw new IOException("client has been disconnected");
            if (data == null)
                throw new ArgumentNullException("data cannot be null");
            if (!typeof(T).IsSerializable)
                throw new ArgumentException("data type must have the [Serializable] attribute");
            if (senderID.Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("senderID cannot be Guid.Empty");

            //packet
            Packet packet = new Packet(senderID, data.PacketPayloadType, data.Serialize());
            var serializedPacket = packet.Serialize();

            //send length
            var serializedLength = BitConverter.GetBytes(serializedPacket.Length);
            stream.Write(serializedLength, 0, serializedLength.Length);

            //send data
            stream.Write(serializedPacket, 0, serializedPacket.Length);
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Disposes this PacketStream, releasing the underlying stream.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            stream.Dispose();
            stream = null;
            client = null;
        }
    }
}
