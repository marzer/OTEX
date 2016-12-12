using Marzersoft;
using Marzersoft.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OTEX.Editor
{
    public sealed class Editor : IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Has this Editor been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return disposed; }
        }
        private volatile bool disposed = false;

        /// <summary>
        /// The document's ID.
        /// </summary>
        public readonly Guid ID;

        /// <summary>
        /// The OTEX document.
        /// </summary>
        public readonly Document Document;

        /// <summary>
        /// The text editor control for this document.
        /// </summary>
        public readonly IEditorTextBox TextBox;

        /// <summary>
        /// The repaint marshal for the editor, if available.
        /// </summary>
        public readonly IRepaintMarshal RepaintMarshal;

        /// <summary>
        /// The title bar tab for this document.
        /// </summary>
        public readonly TitleBarTab Tab;

        /// <summary>
        /// The paginator hosting the editor control.
        /// </summary>
        public readonly Paginator Paginator;

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
        /// The form
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

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public Editor(EditorForm form, Document document)
        {
            //assign
            Form = form ?? throw new ArgumentNullException("form");
            Document = document ?? throw new ArgumentNullException("document");
            LanguageManager = form.languageManager ?? throw new ArgumentNullException("form.languageManager");
            IconManager = form.iconManager ?? throw new ArgumentNullException("form.iconManager");
            Client = form.otexClient ?? throw new ArgumentNullException("form.otexClient");
            User = form.localUser ?? throw new ArgumentNullException("form.localUser");
            Paginator = form.editorPaginator ?? throw new ArgumentNullException("form.editorPaginator");
            Settings = form.settings ?? throw new ArgumentNullException("form.settings");
            ID = Document.ID;

            //create tab
            Tab = new TitleBarTab(form, document.Temporary ? document.Path : Path.GetFileName(document.Path));
            Tab.Tag = this;
            var icon = IconManager[document.Path];
            if (icon != null)
                Tab.Image.Add(icon);

            //create editor
            var textBox = form.plugins.CreateByConfig<IEditorTextBox>("editor", "plugins.editor", true, false, true, "", false);
            if (textBox == null)
                textBox = new ScintillaTextBox();
            TextBox = textBox;
            RepaintMarshal = TextBox as IRepaintMarshal;
            TextBox.Language = LanguageManager[document.Path];
            TextBox.UserColour = User.Colour;
            TextBox.SetRuler(Settings.RulerVisible, Settings.RulerOffset);
            TextBox.LineEndingsVisible = Settings.LineEndings;
            Paginator.Add(ID.ToString(), textBox as Control, Tab.Active);           

            //subscribe to events
            TextBox.OnInsertion += TextBox_OnInsertion;
            TextBox.OnDeletion += TextBox_OnDeletion;
            TextBox.OnSelection += TextBox_OnSelection;
            LanguageManager.OnLoaded += LanguageManager_OnLoaded;
            IconManager.OnLoaded += IconManager_OnLoaded;
            Client.OnRemoteDisconnection += Client_OnRemoteDisconnection;
            Tab.Activated += Tab_Activated;
            User.OnColourChanged += User_OnColourChanged;
            Settings.OnRulerChanged += Settings_OnRulerChanged;
            Settings.OnLineEndingsChanged += Settings_OnLineEndingsChanged;
        }

        /////////////////////////////////////////////////////////////////////
        // SETTINGS EVENTS
        /////////////////////////////////////////////////////////////////////

        private void Settings_OnRulerChanged(Settings obj)
        {
            TextBox.SetRuler(Settings.RulerVisible, Settings.RulerOffset);
        }

        private void Settings_OnLineEndingsChanged(Settings obj)
        {
            TextBox.LineEndingsVisible = Settings.LineEndings;
        }

        /////////////////////////////////////////////////////////////////////
        // USER EVENTS
        /////////////////////////////////////////////////////////////////////

        private void User_OnColourChanged(User user)
        {
            TextBox.UserColour = User.Colour;
        }

        /////////////////////////////////////////////////////////////////////
        // TAB EVENTS
        /////////////////////////////////////////////////////////////////////

        private void Tab_Activated(TitleBarTab tab)
        {
            Paginator.ActivePage = TextBox as Control;
            Form.activeEditor = this;
            User.SetSelection(ID, TextBox.SelectionStart, TextBox.SelectionEnd);
        }

        /////////////////////////////////////////////////////////////////////
        // CLIENT EVENTS
        /////////////////////////////////////////////////////////////////////

        public void UpdateRemoteUser(User user)
        {
            if (user.SelectionDocument == ID)
                TextBox.SetHighlightRange(user.ID, user.SelectionStart, user.SelectionEnd, user.Colour);
            else
                TextBox.SetHighlightRange(user.ID, 0, 0, Color.Black);
        }

        public void RemoteOperations(IEnumerable<Operation> ops)
        {
            TextBox.DiffEvents = false;
            foreach (var operation in ops)
            {
                if (operation.IsInsertion)
                    TextBox.InsertText((uint)operation.Offset, operation.Text);
                else if (operation.IsDeletion)
                    TextBox.DeleteText((uint)operation.Offset, (uint)operation.Length);
            }
            TextBox.DiffEvents = true;
        }

        private void Client_OnRemoteDisconnection(IClient sender, RemoteClient remoteClient)
        {
            Form.Execute(() => {
                TextBox.SetHighlightRange(remoteClient.ID, 0, 0, Color.Black);
            });
        }

        /////////////////////////////////////////////////////////////////////
        // TEXT BOX EVENTS
        /////////////////////////////////////////////////////////////////////

        private void TextBox_OnInsertion(IEditorTextBox sender, uint offset, string text)
        {
            if (Client.Connected)
                Client.Insert(ID, offset, text);
        }

        private void TextBox_OnDeletion(IEditorTextBox sender, uint offset, uint length)
        {
            if (Client.Connected)
                Client.Delete(ID, offset, length);
        }

        private void TextBox_OnSelection(IEditorTextBox sender, uint start, uint end)
        {
            if (Form.activeEditor == this)
                User.SetSelection(ID, start, end);
        }

        /////////////////////////////////////////////////////////////////////
        // LANGUAGES
        /////////////////////////////////////////////////////////////////////

        private void LanguageManager_OnLoaded(LanguageManager sender, int languageCount)
        {
            Form.Execute(() =>
            {
                if (languageCount > 0)
                    TextBox.Language = sender[Document.Path];
            });
        }

        /////////////////////////////////////////////////////////////////////
        // ICONS
        /////////////////////////////////////////////////////////////////////

        private void IconManager_OnLoaded(IconManager sender, int iconCount, int extensionCount)
        {
            Form.Execute(() =>
            {
                if (iconCount > 0 && extensionCount > 0)
                {
                    var icon = IconManager[Document.Path];
                    if (icon != null)
                        Tab.Image.Add(icon);
                }
            });
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            //unsubscribe from events
            Settings.OnLineEndingsChanged -= Settings_OnLineEndingsChanged;
            Settings.OnRulerChanged -= Settings_OnRulerChanged;
            User.OnColourChanged -= User_OnColourChanged;
            LanguageManager.OnLoaded -= LanguageManager_OnLoaded;
            IconManager.OnLoaded -= IconManager_OnLoaded;
            TextBox.OnInsertion -= TextBox_OnInsertion;
            TextBox.OnDeletion -= TextBox_OnDeletion;
            TextBox.OnSelection -= TextBox_OnSelection;
            Tab.Activated -= Tab_Activated;
            Client.OnRemoteDisconnection -= Client_OnRemoteDisconnection;

            //destroy editor
            Paginator.Remove(TextBox as Control);
            var disposable = (TextBox as IDisposable);
            if (disposable != null)
                disposable.Dispose();

            //destroy tab
            Tab.Dispose();
        }

    }
}
