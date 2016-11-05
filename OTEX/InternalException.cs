using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// General-purpose exception class for capturing .NET exceptions.
    /// </summary>
    public class InternalException : Exception
    {
        public InternalException(Exception innerException)
            : base(string.Format("{0}: {1}", innerException.GetType().FullName, innerException.Message), innerException) { }

        /// <summary>
        /// Perform an action, capturing any exceptions and returning them captured inside an OTEX.InternalException.
        /// </summary>
        /// <param name="func">Function to execute.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static InternalException Capture(Action func)
        {
            if (func == null)
                throw new ArgumentNullException("func cannot be null");

            InternalException output = null;
            try
            {
                func();
            }
            catch (Exception exc)
            {
                output = new InternalException(exc);
            }
            return output;
        }

        /// <summary>
        /// Perform an action, re-throwing any exceptions as an OTEX.InternalException.
        /// </summary>
        /// <param name="func">Function to execute.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InternalException"></exception>
        public static void Rethrow(Action func)
        {
            if (func == null)
                throw new ArgumentNullException("func cannot be null");

            InternalException thrown = Capture(func);
            if (thrown != null)
                throw thrown;
        }
    }
}
