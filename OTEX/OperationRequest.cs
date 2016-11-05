using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// A client's request for new operation data from the server.
    /// </summary>
    [Serializable]
    public class OperationRequest
    {
        /// <summary>
        /// Session ID of the client sending the request.
        /// </summary>
        public readonly Guid Sender;

        /// <summary>
        /// New operations from the sender client's local output buffer. Can be null/empty; this
        /// just means "I don't have any new operations, but still send me any new data"
        /// </summary>
        public readonly List<Operation> Operations;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTION/INITIALIZATION/DESTRUCTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a text insertion operation.
        /// </summary>
        public OperationRequest(Guid senderGuid, List<Operation> ops)
        {
            if (senderGuid == Guid.Empty)
                throw new ArgumentException("senderGuid cannot be Guid.Empty");
            Sender = senderGuid;
            Operations = ops;
        }
    }
}
