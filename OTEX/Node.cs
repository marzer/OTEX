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
        // CAPTURING THREAD EXCEPTIONS
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
