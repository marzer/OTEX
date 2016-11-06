using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// A list of operations.
    /// </summary>
    [Serializable]
    public sealed class OperationList : IPacketPayload, IOperationList
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
