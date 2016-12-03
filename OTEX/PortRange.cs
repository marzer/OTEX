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
        public ushort First { get; private set; }

        /// <summary>
        /// The last port in the port range.
        /// </summary>
        public ushort Last { get; private set; }

        /// <summary>
        /// The total number of ports in the port range.
        /// </summary>
        public ushort Count
        {
            get { return (ushort)(((int)Last - First) + 1); }
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
            First = first;
            Last = last;
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
            return port >= First && port <= Last;
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(uint port)
        {
            return port >= First && port <= Last;
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(int port)
        {
            return port >= First && port <= Last;
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(long port)
        {
            return port >= First && port <= Last;
        }

        /// <summary>
        /// Print this port range as a string.
        /// </summary>
        /// <returns>A string representation of this PortRange.</returns>
        public override string ToString()
        {
            return string.Format("{0}-{1}",First,Last);
        }
    }
}
