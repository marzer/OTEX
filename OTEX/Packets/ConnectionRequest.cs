using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marzersoft;

namespace OTEX
{
    /// <summary>
    /// Connection request packet sent from an OTEX client to server.
    /// </summary>
    [Serializable]
    public sealed class ConnectionRequest : IPacketPayload
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 2;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /// <summary>
        /// Password for server (if the server requires one).
        /// </summary>
        public Password Password
        {
            get { return password; }
        }
        private Password password;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct a connection request packet.
        /// </summary>
        /// <param name="password">Password for the server, if requred.</param>
        public ConnectionRequest(Password password = null)
        {
            this.password = password;
        }
    }
}
