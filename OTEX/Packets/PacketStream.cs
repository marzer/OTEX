using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marzersoft;
using Marzersoft.Extensions;

namespace OTEX.Packets
{
    /// <summary>
    /// Reads and writes OTEX packets using a TCPClient.
    /// </summary>
    internal sealed class PacketStream : IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        private byte[] buffer = new byte[4096];
        private TcpClient client = null;
        private NetworkStream stream = null;
        private volatile bool connected = false;
        private readonly Marzersoft.Timer lastConnectPollTimer = new Marzersoft.Timer();
        private readonly object connectionCheckLock = new object();

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
                if (connected && lastConnectPollTimer.Seconds >= 0.5)
                {
                    try
                    {
                        connected = client.IsConnected();
                    }
                    catch (SocketException)
                    {
                        connected = false;
                    }
                    lastConnectPollTimer.Reset();
                }
                return connected;
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
                throw new ArgumentNullException("client");
            if (!client.Connected)
                throw new IOException("client was not connected");
            this.client = client;
            connected = true;

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
        // DISCONNECTION DETECTION
        /////////////////////////////////////////////////////////////////////

        private bool DetectBrokenConnection(Action action)
        {
            try
            {
                action();
            }
            catch (IOException e)
            {
                if (e.InnerException == null)
                    throw;

                SocketException se = e.InnerException as SocketException;
                if (se == null)
                    throw;

                if (se.SocketErrorCode == SocketError.OperationAborted
                    || se.SocketErrorCode == SocketError.NetworkDown
                    || se.SocketErrorCode == SocketError.NetworkUnreachable
                    || se.SocketErrorCode == SocketError.NetworkReset
                    || se.SocketErrorCode == SocketError.ConnectionAborted
                    || se.SocketErrorCode == SocketError.ConnectionRefused
                    || se.SocketErrorCode == SocketError.ConnectionReset
                    || se.SocketErrorCode == SocketError.NotConnected
                    || se.SocketErrorCode == SocketError.Shutdown
                    || se.SocketErrorCode == SocketError.Disconnecting)
                    return true;

                throw;
            }
            return false;
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
            if (!Connected)
                throw new IOException("client was not connected");

            //read size
            int offset = 0;
            int remaining = 4;
            while (remaining > 0)
            {
                int readAmount = 0;
                if (DetectBrokenConnection(() => { readAmount = stream.Read(buffer, offset, remaining); }))
                {
                    connected = false;
                    throw new IOException("client has been disconnected");
                }
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
                int readAmount = 0;
                if (DetectBrokenConnection(() => { readAmount = stream.Read(buffer, offset, remaining); }))
                {
                    connected = false;
                    throw new IOException("client has been disconnected");
                }
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
            if (!Connected)
                throw new IOException("client was not connected");
            if (data == null)
                throw new ArgumentNullException("data");
            if (!typeof(T).IsSerializable)
                throw new ArgumentException("data type must have the [Serializable] attribute", "data");
            if (senderID.Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("senderID", "senderID cannot be Guid.Empty");

            //packet data
            Packet packet = new Packet(senderID, data.PacketPayloadType, data.Serialize());
            var serializedPacket = packet.Serialize();
            var serializedLength = BitConverter.GetBytes(serializedPacket.Length);

            //combine and send together (reduce hitting Nagle's)
            byte[] output = new byte[serializedLength.Length + serializedPacket.Length];
            serializedLength.CopyTo(output, 0);
            serializedPacket.CopyTo(output, serializedLength.Length);
            if (DetectBrokenConnection(() => { stream.Write(output, 0, output.Length); }))
            {
                connected = false;
                throw new IOException("client has been disconnected");
            }
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
            connected = false;
            if (stream != null)
            {
                stream.Flush();
                stream.Dispose();
                stream = null;
            }
            client = null;
        }
    }
}
