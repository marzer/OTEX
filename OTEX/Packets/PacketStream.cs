using System;
using System.IO;
using System.Net.Sockets;
using Marzersoft;

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

        private TcpClient client = null;
        private NetworkStream stream = null;
        private volatile bool connected = false;
        private readonly Timer lastConnectPollTimer = new Timer();
        private readonly object readLock = new object(), writeLock = new object();

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

        /// <summary>
        /// Executes an action on the stream, catching any SocketExceptions thrown
        /// to determine if the connection was broken.
        /// </summary>
        /// <param name="action">An action to perform.</param>
        /// <returns>True if the connection was broken.</returns>
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

            byte[] buffer = null;
            lock (readLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.PacketStream");
                if (!Connected)
                    throw new IOException("client was not connected");

                //read size
                buffer = new byte[4];
                int offset = 0;
                int remaining = buffer.Length;
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

                //read packet
                buffer = new byte[BitConverter.ToInt32(buffer, 0)];
                offset = 0;
                remaining = buffer.Length;
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
            }

            //deserialize packet
            return buffer.Deserialize<Packet>();
        }

        /////////////////////////////////////////////////////////////////////
        // SENDING PACKETS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Prepares an OTEX packet object for sending over the TCP stream.
        /// </summary>
        /// <typeparam name="T">Payload object type. Must have the [Serializable] attribute.</typeparam>
        /// <param name="data">Payload object.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        /// <exception cref="ObjectDisposedException" />
        public static Packet Prepare<T>(T data) where T : class, IPacketPayload
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (!typeof(T).IsSerializable)
                throw new ArgumentException("data type must have the [Serializable] attribute", "data");

            return new Packet(data.PacketPayloadType, data.Serialize());
        }

        /// <summary>
        /// Sends an OTEX packet object over the TCP stream.
        /// </summary>
        /// <param name="data">OTEX packet.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        /// <exception cref="ObjectDisposedException" />
        public void Write(Packet packet)
        {
            if (isDisposed)
                throw new ObjectDisposedException("OTEX.PacketStream");
            if (!Connected)
                throw new IOException("client was not connected");
            if (packet == null)
                throw new ArgumentNullException("packet");

            //packet data
            var serializedPacket = packet.Serialize();
            var serializedLength = BitConverter.GetBytes(serializedPacket.Length);

            //combine and send together
            byte[] output = new byte[serializedLength.Length + serializedPacket.Length];
            serializedLength.CopyTo(output, 0);
            serializedPacket.CopyTo(output, serializedLength.Length);
            lock (writeLock)
            {
                if (isDisposed)
                    throw new ObjectDisposedException("OTEX.PacketStream");
                if (!Connected)
                    throw new IOException("client was not connected");

                if (DetectBrokenConnection(() => { stream.Write(output, 0, output.Length); }))
                {
                    connected = false;
                    throw new IOException("client has been disconnected");
                }
            }
        }

        /// <summary>
        /// Sends an OTEX packet object over the TCP stream.
        /// </summary>
        /// <typeparam name="T">Payload object type. Must have the [Serializable] attribute.</typeparam>
        /// <param name="data">Payload object.</param>
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="IOException" />
        /// <exception cref="ObjectDisposedException" />
        public void Write<T>(T data) where T : class, IPacketPayload
        {
            Write(Prepare(data));
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
