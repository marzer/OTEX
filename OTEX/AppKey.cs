using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX
{
    /// <summary>
    /// The core OTEX framework is implementation-agnostic; it can be 'plugged-in' to any application
    /// using the same Client/Server/ServerListener etc. The AppKey class is a means of uniquely identifying an
    /// implementation so servers and clients from different implementations do not connect to each other.
    /// </summary>
    [Serializable]
    public sealed class AppKey : IEquatable<AppKey>
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maximum size of an appkey's metadata.
        /// </summary>
        private const uint MaxMetadataSize = 512;

        /// <summary>
        /// ID of the app. Must match at both ends.
        /// </summary>
        private Guid ID = Guid.Empty;

        /// <summary>
        /// App version. Must match at both ends.
        /// Not to be confused with <see cref="System.Version"/>, this is an arbitary value used to
        /// represent compatibility at both ends. If changes are made such that previous builds will no longer
        /// interop correctly, increase this value.
        /// </summary>
        private ulong Version = 0;

        /// <summary>
        /// Metadata. Does not (necessarily) need to match at both ends; use this as an additional check
        /// if your needs are more complicated. Limited to 512 bytes. Can be null.
        /// </summary>
        private byte[] Metadata = null;

        /// <summary>
        /// Function used to compare the metadata of two AppKeys for equality.
        /// </summary>
        [NonSerialized] 
        private readonly Func<byte[], byte[], bool> MetadataComparator;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new appkey.
        /// </summary>
        /// <param name="id">Unique identifier of the application.</param>
        /// <param name="version">Version number of the application.</param>
        /// <param name="metadata">Metadata for the application. If this is not null, you must also provide
        /// a value for metadataComparator.</param>
        /// <param name="metadataComparator">Comparator used for comparing two blocks of metadata.</param>
        public AppKey(Guid id, ulong version = 0, byte[] metadata = null, Func<byte[], byte[], bool> metadataComparator = null)
        {
            if ((ID = id) == Guid.Empty)
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
            if (metadata != null && metadata.Length > 0)
            {
                if (metadata.LongLength >= MaxMetadataSize)
                    throw new ArgumentOutOfRangeException("metadata",
                        string.Format("metadata byte arrays may not be longer than {0} bytes", MaxMetadataSize));
                if ((MetadataComparator = metadataComparator) == null)
                    throw new ArgumentNullException("metadataComparator", "AppKeys with metadata must also have a comparator");

                Metadata = (byte[])metadata.Clone();
            }
            Version = version;
        }

        /////////////////////////////////////////////////////////////////////
        // EQUALITY
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check if this AppKey matches another object.
        /// </summary>
        /// <param name="other">The other object to match against.</param>
        public override bool Equals(object other)
        {
            return Equals(other as AppKey);
        }

        /// <summary>
        /// Check if this AppKey matches another AppKey.
        /// </summary>
        /// <param name="other">The other AppKey object to match against.</param>
        public bool Equals(AppKey other)
        {
            if (other == null)
                return false;
            return this == other;
        }

        /// <summary>
        /// Check if two AppKeys match.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>True if they are both the same instance, both null, or both represent the same internal data.</returns>
        public static bool operator ==(AppKey a, AppKey b)
        {
            //same instance or both null
            if (ReferenceEquals(a, b))
                return true;

            //one is null, one is not
            if (((object)a == null) || ((object)b == null))
                return false;

            //id or version is different
            if (a.ID != b.ID || a.Version != b.Version)
                return false;
            
            //one has metadata, the other does not
            if (!ReferenceEquals(a.Metadata, b.Metadata) && (a.Metadata == null || b.Metadata == null))
                return false;

            //have metadata
            if (a.Metadata != null)
            {               
                //must be only one comparator (in the event one is deserialized), or two identical
                if (a.MetadataComparator != null && b.MetadataComparator != null
                    && a.MetadataComparator != b.MetadataComparator)
                    return false;

                //use whichever has a comparator to perform the comparison
                if (!(a.MetadataComparator ?? b.MetadataComparator)(a.Metadata, b.Metadata))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Check if two AppKeys do not match.
        /// </summary>
        /// <param name="a">The left operand</param>
        /// <param name="b">The right operand</param>
        /// <returns>False if they are both the same instance, both null, or both represent the same internal data.</returns>
        public static bool operator !=(AppKey a, AppKey b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Gets a hash code for this AppKey.
        /// </summary>
        /// <returns>A hash code for this AppKey.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)2166136261;
                hash = (hash * 16777619) ^ ID.GetHashCode();
                hash = (hash * 16777619) ^ Version.GetHashCode();
                return hash;
            }
        }
    }
}
