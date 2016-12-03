using System;

namespace OTEX
{
    /// <summary>
    /// Base class of OTEX framework network nodes.
    /// </summary>
    public abstract class Node : ThreadController, INode
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// ID for this node.
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// AppKey for this node. Will only be compatible with other nodes sharing a matching AppKey.
        /// </summary>
        public AppKey AppKey { get; private set; }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX node.
        /// </summary>
        /// <param name="key">AppKey for this node. Will only be compatible with other nodes sharing a matching AppKey.</param>
        /// <param name="id">ID for this node. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="ArgumentOutOfRangeException" />
        public Node(AppKey key, Guid? id = null)
        {
            if ((AppKey = key) == null)
                throw new ArgumentNullException("key");
            if ((ID = id.HasValue ? id.Value : Guid.NewGuid()) == Guid.Empty)
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
        }
    }
}
