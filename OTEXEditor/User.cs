using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marzersoft;

namespace OTEX
{
    /// <summary>
    /// An editor user. Handles the storing/loading of settings for local users.
    /// Some parts of this class (those not marked as [NonSerialized]) are shared
    /// with other OTEX clients via "metadata".
    /// </summary>
    [Serializable]
    public class User
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

        /// <summary>
        /// Event triggered when this User's line-length ruler settings change (local user only).
        /// </summary>
        [field: NonSerialized]
        public event Action<User> OnRulerChanged;

        /// <summary>
        /// Event triggered when this User's otex client update interval setting changes (local user only).
        /// </summary>
        [field: NonSerialized]
        public event Action<User> OnUpdateIntervalChanged;

        /// <summary>
        /// Event triggered when this User's colour theme setting changes (local user only).
        /// </summary>
        [field: NonSerialized]
        public event Action<User> OnThemeChanged;

        /// <summary>
        /// Event triggered when this User's last direct connection setting changes (local user only).
        /// </summary>
        [field: NonSerialized]
        public event Action<User> OnLastDirectConnectionChanged;

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
        public int SelectionStart
        {
            get { return selectionStart; }
            set { SetSelection(value, selectionEnd); }
        }
        private int selectionStart = 0;

        /// <summary>
        /// The end index of this user's selection.
        /// </summary>
        public int SelectionEnd
        {
            get { return selectionEnd; }
            set { SetSelection(selectionStart, value); }
        }
        private int selectionEnd = 0;

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

        /// <summary>
        /// Does the user have the line length ruler visible? (local user only)
        /// </summary>
        public bool Ruler
        {
            get { return IsLocal ? App.Config.User.Get("user.ruler", true) : false; }
            set
            {
                if (IsLocal && value != Ruler)
                {
                    App.Config.User.Set("user.ruler", value);
                    OnRulerChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// What is the line-length setting of the ruler? (local user only)
        /// </summary>
        public uint RulerOffset
        {
            get { return IsLocal ? App.Config.User.Get("user.ruler_offset", 100u).Clamp(60u,200u) : 0; }
            set
            {
                if (IsLocal && (value = value.Clamp(60u, 200u)) != RulerOffset)
                {
                    App.Config.User.Set("user.ruler_offset", value);
                    OnRulerChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Time, in seconds, between each request for updates sent to the server (local user only)
        /// </summary>
        public float UpdateInterval
        {
            get { return IsLocal ? App.Config.User.Get("user.update_interval", 1.0f).Clamp(0.5f,5.0f) : 0.0f; }
            set
            {
                if (IsLocal && !(value = value.Clamp(0.5f, 5.0f)).Equal(UpdateInterval))
                {
                    App.Config.User.Set("user.update_interval", value);
                    OnUpdateIntervalChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// User's chosen colour theme (local user only).
        /// </summary>
        public string Theme
        {
            get { return IsLocal ? App.Config.User.Get("user.theme", "dark").Trim().ToLower() : ""; }
            set
            {
                if (IsLocal && !(value = ((value ?? "").Trim().ToLower())).Equals(Theme))
                {
                    App.Config.User.Set("user.theme", value);
                    OnThemeChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// User's last connection from the "enter server details manually" control (local user only)
        /// </summary>
        public string LastDirectConnection
        {
            get { return IsLocal ? App.Config.User.Get("user.last_direct_connection", "127.0.0.1").Trim() : ""; }
            set
            {
                if (IsLocal && !(value = ((value ?? "").Trim())).Equals(LastDirectConnection))
                {
                    App.Config.User.Set("user.last_direct_connection", value);
                    OnLastDirectConnectionChanged?.Invoke(this);
                }
            }
        }

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
            if ((this.id = id).Equals(Guid.Empty))
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
            if ((this.id = id).Equals(Guid.Empty))
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
            if (!remote.id.Equals(Guid.Empty))
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
        public void SetSelection(int start, int end)
        {
            if (!IsLocal)
                throw new InvalidOperationException("You cannot manually set values for remote users (use Update() instead).");
            SetSelectionInternal(start, end);
        }

        /// <summary>
        /// Internal setter for selection range (to prevent manual setting on remote clients).
        /// </summary>
        private void SetSelectionInternal(int start, int end)
        {
            if (start > end)
                start = end;
            if (start == selectionStart && end == selectionEnd)
                return;

            selectionStart = start;
            selectionEnd = end;
            OnSelectionChanged?.Invoke(this);
        }
    }
}
