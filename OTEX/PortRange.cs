using System;

namespace OTEX
{
    /// <summary>
    /// A simple class for representing a range of ports.
    /// </summary>
    [Serializable]
    public sealed class PortRange
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The first port in the port range.
        /// </summary>
        public ushort First
        {
            get { return first; }
        }
        private ushort first;

        /// <summary>
        /// The last port in the port range.
        /// </summary>
        public ushort Last
        {
            get { return last; }
        }
        private ushort last;

        /// <summary>
        /// The total number of ports in the port range.
        /// </summary>
        public ushort Count
        {
            get { return (ushort)(((int)last - first) + 1); }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a port range.
        /// </summary>
        /// <param name="first">The first port in the range.</param>
        /// <param name="last">The last port in the range.</param>
        /// <exception cref="ArgumentOutOfRangeException" />
        public PortRange(ushort first, ushort last)
        {
            if (last < first)
                throw new ArgumentOutOfRangeException("last", "last port must be equal to or greater than first port");
            this.first = first;
            this.last = last;
        }

        /////////////////////////////////////////////////////////////////////
        // HELPER METHODS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(ushort port)
        {
            return port >= first && port <= last;
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(uint port)
        {
            return port >= first && port <= last;
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(int port)
        {
            return port >= first && port <= last;
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(long port)
        {
            return port >= first && port <= last;
        }

        /// <summary>
        /// Print this port range as a string.
        /// </summary>
        /// <returns>A string representation of this PortRange.</returns>
        public override string ToString()
        {
            return string.Format("{0}-{1}",first,last);
        }
    }
}
