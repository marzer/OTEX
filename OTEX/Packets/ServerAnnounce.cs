using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Packets
{
    /// <summary>
    /// Packet of information describing a public OTEX server.
    /// Unlike other packets, this is not handled using the PacketStream,
    /// so does not implement IPacketPayload (these are sent over UDP instead).
    /// </summary>
    [Serializable]
    internal sealed class ServerAnnounce
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Session ID of the server.
        /// </summary>
        public Guid ID
        {
            get { return id; }
        }
        private Guid id;

        /// <summary>
        /// Server name.
        /// </summary>
        public string Name
        {
            get { return name; }
        }
        private string name;

        /// <summary>
        /// Server port.
        /// </summary>
        public ushort Port
        {
            get { return port; }
        }
        private ushort port;

        /// <summary>
        /// Does this server require a password?
        /// </summary>
        public bool RequiresPassword
        {
            get { return requiresPassword; }
        }
        private bool requiresPassword;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a server's announce packet.
        /// </summary>
        /// <param name="id">Server's session id.</param>
        /// <param name="name">Server's friendly name.</param>
        /// <param name="port">Server's listen port.</param>
        /// <param name="requiresPassword">Does this server require a password?</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public ServerAnnounce(Guid id, string name, ushort port, bool requiresPassword)
        {
            if ((this.id = id).Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
            if ((this.port = port) < 1024)
                throw new ArgumentOutOfRangeException("Port", "Port must be between 1024 and 65535");
            this.name = (name ?? "").Trim();
            this.requiresPassword = requiresPassword;
        }
    }
}
