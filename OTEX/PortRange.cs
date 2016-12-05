using System;
using Marzersoft;

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
        /// Check if a port number is contained within a range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <param name="first">The first port in the range.</param>
        /// <param name="last">The last port in the range.</param>
        /// <returns>True if the port is contained by the range.</returns>
        public static bool Contains(ushort port, ushort first, ushort last)
        {
            if (last < first)
                throw new ArgumentOutOfRangeException("last", "last port must be equal to or greater than first port");
            return port >= first && port <= last;
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(ushort port)
        {
            return Contains(port, First, Last);
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(uint port)
        {
            return port <= ushort.MaxValue && Contains((ushort)port, First, Last);
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(int port)
        {
            return port.Between(0, ushort.MaxValue) && Contains((ushort)port, First, Last);
        }

        /// <summary>
        /// Check if a port number is contained within this range.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the given port is contained by this PortRange.</returns>
        public bool Contains(long port)
        {
            return port.Between(0, ushort.MaxValue) && Contains((ushort)port, First, Last);
        }

        /// <summary>
        /// Print this port range as a string.
        /// </summary>
        /// <returns>A string representation of this PortRange.</returns>
        public override string ToString()
        {
            return string.Format("{0}-{1}",First,Last);
        }

        /// <summary>
        /// Check if two port ranges intersect/exactly overlap.
        /// </summary>
        /// <param name="firstA">The first port in range A.</param>
        /// <param name="lastA">The last port in range A.</param>
        /// <param name="firstB">The first port in range B.</param>
        /// <param name="lastB">The last port in range B.</param>
        /// <returns>True if the two ranges intersect/exactly overlap.</returns>
        public static bool Intersects(ushort firstA, ushort lastA, ushort firstB, ushort lastB)
        {
            if (lastA < firstA)
                throw new ArgumentOutOfRangeException("lastA", "last port must be equal to or greater than first port");
            if (lastB < firstB)
                throw new ArgumentOutOfRangeException("lastB", "last port must be equal to or greater than first port");
            return Contains(firstA, firstB, lastB) || Contains(lastA, firstB, lastB)
                || Contains(firstB, firstA, lastA) || Contains(lastB, firstA, lastA);
        }

        /// <summary>
        /// Check if this port range intersects/exactly overlaps with another.
        /// </summary>
        /// <param name="first">The first port in the other range.</param>
        /// <param name="last">The last port in the other range.</param>
        /// <returns>True if the two ranges intersect/exactly overlap.</returns>
        public bool Intersects(ushort first, ushort last)
        {
            return Intersects(First, Last, first, last);
        }

        /// <summary>
        /// Check if this port range intersects/exactly overlaps with another.
        /// </summary>
        /// <param name="range">The other range.</param>
        /// <returns>True if the two ranges intersect/exactly overlap.</returns>
        public bool Intersects(PortRange range)
        {
            if (range == null)
                throw new ArgumentNullException("range");
            return Intersects(First, Last, range.First, range.Last);
        }
    }
}
