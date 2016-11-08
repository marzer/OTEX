using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Packets
{
    /// <summary>
    /// Connection response packet sent from an OTEX server to client.
    /// </summary>
    [Serializable]
    public sealed class ConnectionResponse : IPacketPayload, IOperationList
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
            Approved,
            IncorrectPassword,
            ServerShuttingDown,
            SessionFull,
            DuplicateGUID,
            Other //unknown (future)
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
        /// List of initial operations.
        /// </summary>
        public List<Operation> Operations
        {
            get { return operations; }
        }
        private List<Operation> operations;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an "approved" connection request response.
        /// </summary>
        /// <param name="filePath">Path (on the server) of the file being edited by the session.</param>
        /// <param name="operations">List of initial operations to send back to the client.</param>
        /// <exception cref="ArgumentException" />
        public ConnectionResponse(string filePath, List<Operation> operations)
        {
            if ((filePath = (filePath ?? "").Trim()).Length == 0)
                throw new ArgumentException("filePath cannot be empty");
            this.filePath = filePath;
            this.operations = operations;
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
                throw new ArgumentOutOfRangeException("failReason must be one of the negative ResponseCode values.");
            result = failReason;
        }
    }
}
