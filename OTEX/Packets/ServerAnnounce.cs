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
        /// Path (on the server) of the file being edited by the session.
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
        }
        private string filePath;

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

        /// <summary>
        /// How many clients are currently connected?
        /// </summary>
        public byte ClientCount
        {
            get { return clientCount; }
        }
        private byte clientCount;

        /// <summary>
        /// How many clients are allowed to be connected at once?
        /// </summary>
        public byte MaxClients
        {
            get { return maxClients; }
        }
        private byte maxClients;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a server's announce packet.
        /// </summary>
        /// <param name="server">Server being announced</param>
        /// <exception cref="ArgumentNullException" />
        public ServerAnnounce(Server server)
        {
            if (server == null)
                throw new ArgumentNullException("server", "server cannot be null");
            clientCount = server.ClientCount;
            maxClients = server.MaxClients;
            requiresPassword = server.RequiresPassword;
            port = server.Port;
            name = server.Name ?? "";
            filePath = server.FilePath ?? "";
        }
    }
}
