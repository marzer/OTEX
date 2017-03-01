using Marzersoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Editor
{
    /// <summary>
    /// Editor settings class. Handles config files and publishes change notifications.
    /// </summary>
    public sealed class Settings
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Event triggered when the line-length ruler settings change.
        /// </summary>
        public event Action<Settings> OnRulerChanged;

        /// <summary>
        /// Event triggered when the otex client update interval setting changes.
        /// </summary>
        public event Action<Settings> OnUpdateIntervalChanged;

        /// <summary>
        /// Event triggered when the selected colour theme changes.
        /// </summary>
        public event Action<Settings> OnThemeChanged;

        /// <summary>
        /// Event triggered when the show line endings setting changes.
        /// </summary>
        public event Action<Settings> OnLineEndingsChanged;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Does the user have the line ending markers visible?
        /// </summary>
        public bool LineEndings
        {
            get { return App.Config.User.Get("editor.line_endings", false); }
            set
            {
                if (value != LineEndings)
                {
                    App.Config.User.Set("editor.line_endings", value);
                    OnLineEndingsChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Does the user have the line length ruler visible?
        /// </summary>
        public bool RulerVisible
        {
            get { return App.Config.User.Get("editor.ruler", true); }
            set
            {
                if (value != RulerVisible)
                {
                    App.Config.User.Set("editor.ruler", value);
                    OnRulerChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// What is the line-length setting of the ruler?
        /// </summary>
        public uint RulerOffset
        {
            get { return App.Config.User.Get("editor.ruler_offset", 100u).Clamp(60u, 200u); }
            set
            {
                if ((value = value.Clamp(60u, 200u)) != RulerOffset)
                {
                    App.Config.User.Set("editor.ruler_offset", value);
                    OnRulerChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Time, in seconds, between each request for updates sent to the server
        /// </summary>
        public float UpdateInterval
        {
            get { return App.Config.User.Get("client.update_interval", 1.0f).Clamp(0.5f, 5.0f); }
            set
            {
                if (!(value = value.Clamp(0.5f, 5.0f)).IsSimilar(UpdateInterval))
                {
                    App.Config.User.Set("client.update_interval", value);
                    OnUpdateIntervalChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// User's chosen colour theme
        /// </summary>
        public string Theme
        {
            get { return App.Config.User.Get("editor.theme", "dark").Trim().ToLower(); }
            set
            {
                if (!(value = ((value ?? "").Trim().ToLower())).Equals(Theme))
                {
                    App.Config.User.Set("editor.theme", value);
                    OnThemeChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// User's last connection from the "enter server details manually" control
        /// </summary>
        public string LastDirectConnection
        {
            get { return App.Config.User.Get("editor.last_direct_connection", "127.0.0.1").Trim(); }
            set
            {
                if (!(value = ((value ?? "").Trim())).Equals(LastDirectConnection))
                    App.Config.User.Set("editor.last_direct_connection", value);
            }
        }
    }
}
