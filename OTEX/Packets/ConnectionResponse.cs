using System;
using System.Collections.Generic;

namespace OTEX.Packets
{
    /// <summary>
    /// Connection response packet sent from an OTEX server to client.
    /// </summary>
    [Serializable]
    internal sealed class ConnectionResponse : IPacketPayload, IOperationList, IClientMetadata, IClientUpdate
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
        /// Session ID of the server sending the response.
        /// </summary>
        public Guid ServerID
        {
            get { return serverID; }
        }
        private Guid serverID;

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
        /// <param name="serverID">Session ID of the server.</param>
        /// <param name="filePath">Path (on the server) of the file being edited by the session.</param>
        /// <param name="name">The friendly name of the server.</param>
        /// <param name="operations">List of initial operations to send back to the client.</param>
        /// <param name="metadata">Set of metadata for other connected clients.</param>
        public ConnectionResponse(Guid serverID, string filePath, string name, List<Operation> operations = null, Dictionary<Guid, byte[]> metadata = null)
        {
            if ((this.serverID = serverID).Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("serverID", "serverID cannot be Guid.Empty");
            if (metadata != null && metadata.Count > 0)
            {
                foreach (var kvp in metadata)
                    if (kvp.Value != null && kvp.Value.LongLength >= Client.MaxMetadataSize)
                        throw new ArgumentOutOfRangeException("metadata",
                            string.Format("metadata byte arrays may not be longer than {0} bytes", Client.MaxMetadataSize));
            }
            this.filePath = (filePath ?? "").Trim();
            this.name = (name ?? "").Trim();
            this.metadata = metadata != null && metadata.Count > 0 ? metadata : null;
            this.operations = operations != null && operations.Count > 0 ? operations : null;

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
            serverID = Guid.Empty;
            result = failReason;
            filePath = null;
            name = null;
            metadata = null;
            operations = null;
        }
    }
}
