using System;
using System.Collections.Generic;

namespace OTEX.Packets
{
    /*
     * COMP7722: OTEX uses the NICE approach, and keeps the request-response
     * flow intact. The class implemented below is the server's initial response
     * to the client's request to connect; in the event the client is connecting to a 
     * session that is already in progress, this response contains the server's "master"
     * set of operations, so they may synchronize without any further handshaking.
     */

    /// <summary>
    /// Connection response packet sent from an OTEX server to client.
    /// </summary>
    [Serializable]
    internal sealed class ConnectionResponse : IPacketPayload, IOperationList, IClientMetadata
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 3;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /// <summary>
        /// Possible results of a connection request.
        /// </summary>
        public enum ResponseCode : uint
        {
            /// <summary>
            /// Connection request was approved and the client
            /// is now 'connected' according to the Server.
            /// </summary>
            Approved,

            /// <summary>
            /// The password provided by client was not correct
            /// (also fires when clients provide passwords when none is required)
            /// </summary>
            IncorrectPassword,

            /// <summary>
            /// Server has already reached the maximum number of connected clients
            /// </summary>
            SessionFull,

            /// <summary>
            /// A client with the same GUID is already connected to the server
            /// (shouldn't happen, really; fluke!)
            /// </summary>
            DuplicateGUID,

            /// <summary>
            /// Client fell into an invalid state(sent non-request packet as first packet)
            /// </summary>
            InvalidState,

            /// <summary>
            /// Other (not used by anything currently)
            /// </summary>
            Other
        }

        /// <summary>
        /// What was the result of the request?
        /// </summary>
        public ResponseCode Result
        {
            get { return result; }
        }
        private ResponseCode result;

        /// <summary>
        /// Path (on the server) of the file being edited by the session.
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
        }
        private string filePath;

        /// <summary>
        /// The friendly name of the server.
        /// </summary>
        public string Name
        {
            get { return name; }
        }
        private string name;

        /// <summary>
        /// List of initial operations.
        /// </summary>
        public List<Operation> Operations
        {
            get { return operations; }
        }
        private List<Operation> operations;

        /// <summary>
        /// Set of metadata for other connected clients.
        /// </summary>
        public Dictionary<Guid, byte[]> Metadata
        {
            get { return metadata; }
        }
        private Dictionary<Guid, byte[]> metadata;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an "approved" connection request response.
        /// </summary>
        /// <param name="filePath">Path (on the server) of the file being edited by the session.</param>
        /// <param name="name">The friendly name of the server.</param>
        /// <param name="operations">List of initial operations to send back to the client.</param>
        /// <param name="metadata">Set of metadata for other connected clients.</param>
        public ConnectionResponse(string filePath, string name, List<Operation> operations, Dictionary<Guid, byte[]> metadata)
        {
            this.filePath = (filePath ?? "").Trim();
            this.name = (name ?? "").Trim();
            this.operations = operations;
            this.metadata = metadata;
            result = ResponseCode.Approved;
        }

        /// <summary>
        /// Constructs a "rejected" connection request response.
        /// </summary>
        /// <param name="failReason">Reason the connection was rejected.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public ConnectionResponse(ResponseCode failReason = ResponseCode.Other)
        {
            if (failReason == ResponseCode.Approved || failReason > ResponseCode.Other)
                throw new ArgumentOutOfRangeException("failReason", "failReason must be one of the negative ResponseCode values.");
            result = failReason;
            filePath = null;
            name = null;
        }
    }
}
