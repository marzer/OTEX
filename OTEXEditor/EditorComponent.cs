using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OTEX.Editor
{
    /// <summary>
    /// Base class for objects needing easy access to editor application state.
    /// I realize this is _bad design_, but it's entirely internal, so meh.
    /// </summary>
    internal abstract class EditorComponent
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The language manager.
        /// </summary>
        public readonly LanguageManager LanguageManager;

        /// <summary>
        /// The icon manager.
        /// </summary>
        public readonly IconManager IconManager;

        /// <summary>
        /// The OTEX client.
        /// </summary>
        public readonly Client Client;

        /// <summary>
        /// The OTEX server.
        /// </summary>
        public readonly Server Server;

        /// <summary>
        /// The editor form.
        /// </summary>
        public readonly EditorForm Form;

        /// <summary>
        /// The local user of the editor.
        /// </summary>
        public readonly User User;

        /// <summary>
        /// The application settings.
        /// </summary>
        public readonly Settings Settings;

        /// <summary>
        /// The host-mode session.
        /// </summary>
        public readonly Session HostedSession;

        /// <summary>
        /// The host-mode list box containing hosted documents.
        /// </summary>
        public readonly ListBox HostedDocuments;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public EditorComponent(EditorForm form)
        {
            //assign
            Form = form ?? throw new ArgumentNullException("form");
            LanguageManager = form.languageManager ?? throw new ArgumentNullException("form.languageManager");
            IconManager = form.iconManager ?? throw new ArgumentNullException("form.iconManager");
            Client = form.otexClient ?? throw new ArgumentNullException("form.otexClient");
            Server = form.otexServer ?? throw new ArgumentNullException("form.otexServer");
            User = form.localUser ?? throw new ArgumentNullException("form.localUser");
            Settings = form.settings ?? throw new ArgumentNullException("form.settings");
            HostedSession = form.hostSession ?? throw new ArgumentNullException("form.hostSession");
            HostedDocuments = form.lbDocuments ?? throw new ArgumentNullException("form.lbDocuments");
        }
    }
}
