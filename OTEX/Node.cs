using System;

namespace OTEX
{
    /// <summary>
    /// Base class of OTEX framework network nodes.
    /// </summary>
    public abstract class Node : ThreadController
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// ID for this node.
        /// </summary>
        public readonly Guid ID;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates an OTEX node.
        /// </summary>
        /// <param name="id">ID for this node. Leaving it null will auto-generate one.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public Node(Guid? id = null)
        {
            if ((ID = id.HasValue ? id.Value : Guid.NewGuid()).Equals(Guid.Empty))
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
        }
    }
}
