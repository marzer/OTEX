using System;
#if DEBUG
using System.Runtime.CompilerServices;
using Marzersoft;
#endif

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
        /// </summary>
        /// <param name="func">The action to execute. Exceptions raised within will be caught and redirected to the OnThreadException event.</param>
        /// <returns>True if an exception was caught.</returns>
        protected bool CaptureException(Action func
#if DEBUG
            , [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
#endif
            )
        {
            if (func == null)
                throw new ArgumentNullException("func");

            try
            {
                func();
            }
            catch (Exception exc)
            {
#if DEBUG
                Logger.E("[{0}:{1}, {2}] {3}: {4}",
                    System.IO.Path.GetFileName(sourceFilePath), sourceLineNumber, memberName,
                    exc.GetType().Name, exc.Message);
#endif
                NotifyException(new ThreadException(exc));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Invokes the OnThreadException event for the given exception.
        /// </summary>
        /// <param name="ex">The exception to pass to the OnThreadException event handler.</param>
        protected void NotifyException(ThreadException ex)
        {
            OnThreadException?.Invoke(this, ex);
        }
    }
}
