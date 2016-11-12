using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// Base class of OTEX framework objects which own threads.
    /// </summary>
    public abstract class ThreadController
    {
        /////////////////////////////////////////////////////////////////////
        // THREAD EXCEPTION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// General-purpose exception class for capturing other exceptions thrown on a thread managed by
        /// a ThreadController subclass.
        /// </summary>
        public sealed class ThreadException : Exception
        {
            internal ThreadException(Exception innerException)
                : base(string.Format("{0}: {1}", innerException.GetType().FullName, innerException.Message), innerException) { }
        }

        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Triggered when an internal thread throws an exception.
        /// </summary>
        public event Action<ThreadController, ThreadException> OnThreadException;

        /////////////////////////////////////////////////////////////////////
        // THREAD EXCEPTIONS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Capture thrown exceptions and trigger the OnInternalException event.
        /// Returns true if an exception was caught.
        /// </summary>
        protected bool CaptureException(Action func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            try
            {
                func();
            }
            catch (Exception exc)
            {
                OnThreadException?.Invoke(this, new ThreadException(exc));
                return true;
            }
            return false;
        }
    }
}
