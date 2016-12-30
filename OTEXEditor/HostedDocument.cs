using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Marzersoft;
using Marzersoft.Themes;
using System.Collections;
using System.IO;
using System.Drawing;

namespace OTEX.Editor
{
    /// <summary>
    /// Wrapper for OTEX documents so they can be added to a themed list box without having to stick
    /// this sort of plumbing into the Document class itself
    /// </summary>
    internal sealed class HostedDocument : EditorComponent, IThemedListBoxItem, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Has this HostedDocument been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return disposed; }
        }
        private volatile bool disposed = false;

        /// <summary>
        /// The document object held by this list item. Changes after a successful call to 
        /// ConvertReplaceToEdit.
        /// </summary>
        public Document Document { get; private set; }

        private string key;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public HostedDocument(EditorForm form, string name) : base(form)
        {
            name = (name ?? "").Trim();
            key = name.ToLower();
            var docs = HostedDocuments.Items.Cast<HostedDocument>().ToList();
            foreach (var doc in docs)
                if (doc.Matches(true, name))
                    throw new ArgumentException(
                        string.Format("A temporary document with description \"{0}\" has already been added to the session.", name),
                            "name");
            Document = HostedSession.AddDocument(name);
            HostedDocuments.Items.Add(this);
        }

        public HostedDocument(EditorForm form, string path, Document.ConflictResolutionStrategy conflictStrategy) : base(form)
        {
            if ((path = (path ?? "").Trim()).Length == 0)
                throw new ArgumentOutOfRangeException("path", "path cannot be blank");
            key = (path = Path.GetFullPath(path)).ToLower();
            var docs = HostedDocuments.Items.Cast<HostedDocument>().ToList();
            foreach (var doc in docs)
                if (doc.Matches(false, path))
                    throw new ArgumentException(
                        string.Format("{0} has already been added to the session.", path),
                            "path");
            Document = HostedSession.AddDocument(path, conflictStrategy, 4);
            HostedDocuments.Items.Add(this);
        }

        /////////////////////////////////////////////////////////////////////
        // CONVERTING REPLACE TO EDIT
        /////////////////////////////////////////////////////////////////////

        public bool ConvertReplaceToEdit()
        {
            if (Document.Temporary
                || Document.ConflictResolution != Document.ConflictResolutionStrategy.Replace)
                return false;

            if (HostedSession.RemoveDocument(Document))
            {
                Document = HostedSession.AddDocument(Document.Path, Document.ConflictResolutionStrategy.Edit, 4);
                return true;
            }
            return false;
        }

        /////////////////////////////////////////////////////////////////////
        // MATCHING WITH OTHER DOCUMENTS
        /////////////////////////////////////////////////////////////////////

        public bool Matches(bool temporary, string pathDesc)
        {
            if (temporary != Document.Temporary)
                return false;

            pathDesc = (pathDesc ?? "").Trim();
            if (!temporary)
                pathDesc = Path.GetFullPath(pathDesc);
            pathDesc = pathDesc.ToLower();

            return pathDesc.Equals(key);
        }

        /////////////////////////////////////////////////////////////////////
        // IThemedListBoxItem implementation
        /////////////////////////////////////////////////////////////////////

        void IThemedListBoxItem.DrawListboxItem(DrawItemEventArgs e)
        {
            //draw icon
            var iconBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + 8, 16, 16);
            var icon = IconManager[Document.Path];
            if (icon != null)
                e.Graphics.DrawImage(icon, iconBounds);

            //draw warning text
            var textbounds = Rectangle.FromLTRB(iconBounds.Right + 4, e.Bounds.Top,
                e.Bounds.Right-4, e.Bounds.Bottom);
            if (Document.Temporary || Document.ConflictResolution == Document.ConflictResolutionStrategy.Replace)
            {
                var str = Document.Temporary ? "(Temp)" : "(New)";
                var sz = e.Graphics.MeasureString(str, e.Font);
                var warningBounds = Rectangle.FromLTRB(textbounds.Right - (int)sz.Width, e.Bounds.Top,
                    e.Bounds.Right, e.Bounds.Bottom);
                textbounds.Width -= (warningBounds.Width + 4);
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    sf.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox | StringFormatFlags.NoFontFallback;
                    sf.Trimming = StringTrimming.None;
                    e.Graphics.DrawString(str, e.Font, e.ForeColor, warningBounds, sf);
                }
            }

            //draw text
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Center;
                sf.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.FitBlackBox | StringFormatFlags.NoFontFallback;
                sf.Trimming = Document.Temporary ? StringTrimming.EllipsisCharacter : StringTrimming.EllipsisPath;
                e.Graphics.DrawString(Document.Temporary ? Document.Path : Path.GetFileName(Document.Path), e.Font,
                    e.ForeColor, textbounds, sf);
            }
        }

        int IThemedListBoxItem.MeasureItemHeight(ThemedListBox host, MeasureItemEventArgs e)
        {
            return 32;
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            //remove from list
            HostedDocuments.Items.Remove(this);

            //remove from session
            HostedSession.RemoveDocument(Document);
        }
    }
}
