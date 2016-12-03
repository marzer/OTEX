using System;
using System.Net;

namespace OTEX
{
    /// <summary>
    /// Interface representing client-side objects in an OTEX connection.
    /// </summary>
    public interface IClient : INode
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when the client successfully connects to an OTEX server.
        /// </summary>
        event Action<IClient> OnConnected;

        /// <summary>
        /// Triggered when a remote client updates it's metadata.
        /// Do not call any of this object's methods from this callback or you may deadlock!
        /// </summary>
        event Action<IClient, Guid, byte[]> OnRemoteMetadata;

        /// <summary>
        /// Triggered when the client receives remote operations from the server.
        /// Do not call any of this object's methods from this callback or you may deadlock!
        /// </summary>
        event Action<IClient, bool> OnDisconnected;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Is the client currently connected to a server?
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// IP Address of server.
        /// </summary>
        IPAddress ServerAddress { get; }

        /// <summary>
        /// Path of the file being edited on the server.
        /// </summary>
        string ServerFilePath { get; }

        /// <summary>
        /// ID of server.
        /// </summary>
        Guid ServerID { get; }

        /// <summary>
        /// The friendly name of the server.
        /// </summary>
        string ServerName { get; }

        /// <summary>
        /// Server's listen port.
        /// </summary>
        ushort ServerPort { get; }

        /// <summary>
        /// Time, in seconds, between each request for updates sent to the server
        /// (clamped between 0.5 and 5.0).
        /// </summary>
        float UpdateInterval { get; set; }

        /// <summary>
        /// This client's metadata.
        /// </summary>
        byte[] Metadata { get; set; }

        /////////////////////////////////////////////////////////////////////
        // METHODS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="address">IP Address of the OTEX server.</param>
        /// <param name="port">Listen port of the OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        void Connect(IPAddress address, ushort port = 55550, Password password = null);

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="serverDescription">ServerDescription for an OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        void Connect(ServerDescription serverDescription, Password password = null);

        /// <summary>
        /// Connect to an OTEX server.
        /// </summary>
        /// <param name="endpoint">IP Endpoint (address and port) of the OTEX server.</param>
        /// <param name="password">Password required to connect to the server, if any. Leave as null for none.</param>
        void Connect(IPEndPoint endpoint, Password password = null);

        /// <summary>
        /// Disconnect from an OTEX server. Does nothing if not already connected.
        /// </summary>
        void Disconnect();
    }
}