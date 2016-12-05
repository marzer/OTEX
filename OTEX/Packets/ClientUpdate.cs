using System;
using System.Collections.Generic;

namespace OTEX.Packets
{
    /// <summary>
    /// A list of operations and client metadata.
    /// </summary>
    [Serializable]
    internal sealed class ClientUpdate : IPacketPayload
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
        /// When sent from a client, this list is the set of new operations from the sender client's
        /// local output buffer. Can be null/empty; this means "I don't have any new operations,
        /// but still send me any new data"
        /// 
        /// When sent from the server, this is the list of new remote operations the client needs
        /// to synchronize with other clients. Can be null/empty; this means "There have been no new
        /// remote operations since your last request".
        /// 
        /// Guid key corresponds to target document.
        /// </summary>
        public Dictionary<Guid, List<Operation>> Operations { get; private set; }

        /// <summary>
        /// When sent from a client, this will contain at most 1 element, which will be the new value
        /// for the sender's metadata. Can be null/empty; this means "my metadata has not changed".
        /// 
        /// When sent from a server, this will contain the entire set of changed metadata, so the client
        /// may synchronize it's local replications of remote clients metadata with their new values.
        /// Can be null/empty; this means "there have been no changes to metadata since your last request".
        /// 
        /// Guid key corresponds to client.
        /// </summary>
        public Dictionary<Guid, RemoteClient> Metadata { get; private set; }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTORS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Construct an update packet.
        /// </summary>
        /// <param name="operations">New operations to send.</param>
        /// <param name="metadata">New metadata to send.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public ClientUpdate(Dictionary<Guid, List<Operation>> operations = null, Dictionary<Guid, RemoteClient> metadata = null)
        {
            Metadata = metadata != null && metadata.Count > 0 ? metadata : null;
            Operations = operations != null && operations.Count > 0 ? operations : null;
        }
    }
}
