using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Marzersoft;
using Marzersoft.Themes;

namespace OTEX.Editor
{
    /// <summary>
    /// An editor user. Handles the storing/loading of settings for local users.
    /// Some parts of this class (those not marked as [NonSerialized]) are shared
    /// with other OTEX clients via "metadata".
    /// </summary>
    [Serializable]
    public sealed class User : IEquatable<User>, IComparable<User>, IComparable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Event triggered when this User's Colour changes.
        /// </summary>
        [field: NonSerialized]
        public event Action<User> OnColourChanged;

        /// <summary>
        /// Event triggered when this User's selection range changes.
        /// </summary>
        [field: NonSerialized]
        public event Action<User> OnSelectionChanged;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This user's ID (from OTEXClient.ID).
        /// </summary>
        public Guid ID
        {
            get { return id; }
        }
        [NonSerialized]
        private Guid id = Guid.Empty;

        /// <summary>
        /// The start index of this user's selection.
        /// </summary>
        public uint SelectionStart
        {
            get { return selectionStart; }
            set { SetSelection(value, selectionEnd); }
        }
        private uint selectionStart = 0;

        /// <summary>
        /// The end index of this user's selection.
        /// </summary>
        public uint SelectionEnd
        {
            get { return selectionEnd; }
            set { SetSelection(selectionStart, value); }
        }
        private uint selectionEnd = 0;

        /// <summary>
        /// Internal setter for colour integer (to prevent manual setting on remote clients).
        /// </summary>
        private int ColourIntegerInternal
        {
            set
            {
                //ensure alpha is 255 (client colours are 100% opaque)
                var col = value | 0xFF000000;
                if (col == colour)
                    return;
                colour = (int)col;
                if (IsLocal)
                    App.Config.User.Set("user.colour", colour.ToColour());
                OnColourChanged?.Invoke(this);
            }
        }
        private int colour = 0;

        /// <summary>
        /// This user's colour, expressed as an integer.
        /// </summary>
        public int ColourInteger
        {
            get { return colour; }
            set
            {
                if (!IsLocal)
                    throw new InvalidOperationException("You cannot manually set values for remote users (use Update() instead).");
                ColourIntegerInternal = value;
            }
        }        

        /// <summary>
        /// This user's colour.
        /// </summary>
        public Color Colour
        {
            get { return colour.ToColour(); }
            set { ColourInteger = value.ToArgb(); }
        }

        /// <summary>
        /// Is this user a local user?
        /// </summary>
        [NonSerialized]
        public readonly bool IsLocal;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create the local User.
        /// </summary>
        /// <param name="id">ID of the local user (from OTEXClient.ID)</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public User(Guid id)
        {
            if ((this.id = id) == Guid.Empty)
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
            IsLocal = true;
            colour = App.Config.User.Get("user.colour", Color.Transparent).ToArgb();
        }

        /// <summary>
        /// Create a User given an ID and the serialized form of a User from metadata.
        /// This constructor is for remote users.
        /// </summary>
        /// <param name="id">ID of the remote user (from OTEXClient.OnRemoteMetadata)</param>
        /// <param name="metadata">Metadata to deserialize.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="System.IO.IOException" />
        public User(Guid id, byte[] metadata)
        {
            if ((this.id = id) == Guid.Empty)
                throw new ArgumentOutOfRangeException("id", "id cannot be Guid.Empty");
            IsLocal = false;
            Update(metadata);
        }

        /////////////////////////////////////////////////////////////////////
        // UPDATING A REMOTE EDITOR CLIENT
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Updates a remote user using data from a metadata event.
        /// </summary>
        /// <param name="metadata">Metadata to deserialize.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException" />
        /// <exception cref="System.Runtime.Serialization.SerializationException" />
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="System.IO.IOException" />
        /// <exception cref="InvalidOperationException" />
        public void Update(byte[] metadata)
        {
            //sanity
            if (IsLocal)
                throw new InvalidOperationException("You cannot call Update() on the local user.");
            if (metadata == null || metadata.Length == 0)
                throw new ArgumentNullException("metadata");

            //deserialize
            var remote = metadata.Deserialize<User>();
            if (remote.id != Guid.Empty)
                throw new ArgumentException("metadata.ID must be Guid.Empty (cannot copy a non-deserialized User)", "metadata");

            //update
            ColourIntegerInternal = remote.colour;
            SetSelectionInternal(remote.selectionStart, remote.selectionEnd);
        }

        /////////////////////////////////////////////////////////////////////
        // SETTING SELECTION RANGE
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the selection range of this user. Use this instead of the individual setters if updating
        /// start and end at the same time as this will only fire the OnSelectionChanged event once.
        /// </summary>
        /// <param name="start">The start index of this user's selection.</param>
        /// <param name="end">The end index of this user's selection.</param>
        public void SetSelection(uint start, uint end)
        {
            if (!IsLocal)
                throw new InvalidOperationException("You cannot manually set values for remote users (use Update() instead).");
            SetSelectionInternal(start, end);
        }

        /// <summary>
        /// Internal setter for selection range (to prevent manual setting on remote clients).
        /// </summary>
        private void SetSelectionInternal(uint start, uint end)
        {
            if (start == selectionStart && end == selectionEnd)
                return;

            selectionStart = start;
            selectionEnd = end;
            OnSelectionChanged?.Invoke(this);
        }

        /////////////////////////////////////////////////////////////////////
        // EQUALITY, COMPARISONS
        /////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override bool Equals(object other)
        {
            return Equals(other as User);
        }

        public bool Equals(User other)
        {
            if (other == null)
                return false;
            return this == other;
        }

        public static bool operator ==(User a, User b)
        {
            //same instance or both null
            if (ReferenceEquals(a, b))
                return true;

            //one is null, one is not
            if (((object)a == null) || ((object)b == null))
                return false;

            return a.ID == b.ID;
        }

        public static bool operator !=(User a, User b)
        {
            return !(a == b);
        }

        public int CompareTo(object other)
        {
            return CompareTo(other as User);
        }

        public int CompareTo(User other)
        {
            if (other == null)
                return 1;
            if (ReferenceEquals(this, other))
                return 0;
            return ID.CompareTo(other.ID);
        }
    }
}
