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
    public sealed class InternalException : Exception
    {
        internal InternalException(Exception innerException)
            : base(string.Format("{0}: {1}", innerException.GetType().FullName, innerException.Message), innerException) { }

        /// <summary>
        /// Perform an action, capturing any exceptions and returning them captured inside an OTEX.InternalException.
        /// </summary>
        /// <param name="func">Function to execute.</param>
        /// <exception cref="ArgumentNullException"></exception>
        internal static InternalException Capture(Action func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

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
    }
}
