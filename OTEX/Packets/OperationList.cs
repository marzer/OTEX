using System;
using System.Collections.Generic;

namespace OTEX.Packets
{
    /*
     * COMP7722: OTEX uses the NICE approach, and keeps the request-response
     * flow intact. The class implemented below is used both as the client's request for new updates
     * (containing it's own outgoing local changes), and the server's response with changes from
     * other clients.
     */

    /// <summary>
    /// A list of operations.
    /// </summary>
    [Serializable]
    internal sealed class OperationList : IPacketPayload, IOperationList
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Payload type ID for this class.
        /// </summary>
        public const uint PayloadType = 1;

        /// <summary>
        /// Implements IPacketPayload.PacketPayloadType (returns PayloadType).
        /// </summary>
        public uint PacketPayloadType
        {
            get { return PayloadType; }
        }

        /// <summary>
        /// New operations from the sender client's local output buffer. Can be null/empty; this
        /// just means "I don't have any new operations, but still send me any new data"
        /// </summary>
        public List<Operation> Operations
        {
            get { return operations; }
        }
        private List<Operation> operations;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION/INITIALIZATION/DESTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a text insertion operation.
        /// </summary>
        public OperationList(List<Operation> ops)
        {
            operations = ops;
        }
    }
}
