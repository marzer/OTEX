using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// Base class of OTEX framework network nodes.
    /// </summary>
    public abstract class Node
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when an internal thread throws an exception.
        /// </summary>
        public event Action<Node, InternalException> OnInternalException;

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
                throw new ArgumentOutOfRangeException("id cannot be Guid.Empty");
        }

        /////////////////////////////////////////////////////////////////////
        // THREAD EXCEPTIONS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Capture thrown exceptions and trigger the OnInternalException event.
        /// Returns true if an exception was caught.
        /// </summary>
        protected bool CaptureException(Action func)
        {
            InternalException exception = InternalException.Capture(func);
            if (exception != null && OnInternalException != null)
            {
                OnInternalException(this, exception);
                return true;
            }
            return false;
        }
    }
}
