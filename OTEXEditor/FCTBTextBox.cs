using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastColoredTextBoxNS;
using System.Windows.Forms;
using System.ComponentModel;
using Marzersoft;
using Marzersoft.Themes;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace OTEX
{
    /// <summary>
    /// OTEX Editor subclass of the FastColoredTextBox. Implements Marzersoft.IThemeable and handles
    /// refresh logic, text version caching, etc.
    /// </summary>
    public class FCTBTextBox : FastColoredTextBox, IThemeable, IEditorTextBox
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// More robust check for design mode.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsDesignMode
        {
            get { return DesignMode || this.IsDesignMode(); }
        }

        /// <summary>
        /// Fired when text is inserted into the document (if DiffEvents == true).
        /// parameters: sender, starting index, inserted text.
        /// </summary>
        public event Action<IEditorTextBox, int, string> OnInsertion;

        /// <summary>
        /// Fired when text is deleted from the document (if DiffEvents == true).
        /// parameters: sender, starting index, length of deleted text.
        /// </summary>
        public event Action<IEditorTextBox, int, int> OnDeletion;

        /// <summary>
        /// Are the OnInsertion/OnDeletion events fired when the text is changed?
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DiffEvents
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                return diffGeneration;
            }
            set
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                diffGeneration = value;
            }
        }
        private volatile bool diffGeneration = true;

        /// <summary>
        /// The user colour as applied to this editor text box.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color UserColour
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                return userColour;
            }
            set
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                if (value.A < 255)
                    value = Color.FromArgb(255,value);
                if (userColour.Equals(value))
                    return;
                SelectionColor
                    = CurrentLineColor
                    = LineNumberColor
                    = userColour = value;
                CaretColor = userColour.Brighten(0.3f);

            }
        }
        private Color userColour = Color.White;

        /// <summary>
        /// When an TextChanged event is triggered, this will be a cached version of the text before
        /// the changes were made.
        /// </summary>
        private List<string> previousLines = null;

        /// <summary>
        /// The current language definitions used for syntax highlighting.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        LanguageManager.Language IEditorTextBox.Language
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                return currentLanguage;
            }
            set
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                lock (currentLanguageLock)
                {
                    if (currentLanguage == value)
                        return;
                    currentLanguage = value;
                    if (currentLanguage == null)
                        Language = Language.Custom;
                    else
                    {
                        switch (currentLanguage.Name)
                        {
                            case "cs": Language = Language.CSharp; break;
                            case "vb": Language = Language.VB; break;
                            case "html": Language = Language.HTML; break;
                            case "xml": Language = Language.XML; break;
                            case "sql": Language = Language.SQL; break;
                            case "php": Language = Language.PHP; break;
                            case "javascript": Language = Language.JS; break;
                            case "javascript.js": Language = Language.JS; break;
                            case "json": Language = Language.JS; break;
                            case "lua": Language = Language.Lua; break;
                        }
                    }
                }
            }
        }
        private LanguageManager.Language currentLanguage = null;
        private readonly object currentLanguageLock = new object();

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public FCTBTextBox()
        {
            //setup
            ReservedCountOfLineNumberChars = 5;
            WordWrap = true;
            WordWrapAutoIndent = true;
            WordWrapMode = WordWrapMode.WordWrapControlWidth;
            TabLength = 4;
            LineInterval = 2;
            HotkeysMapping.Remove(Keys.Control | Keys.H); //remove default "replace" (CTRL + H, wtf?)
            HotkeysMapping[Keys.Control | Keys.R] = FCTBAction.ReplaceDialog; // CTRL + R for replace
            HotkeysMapping[Keys.Control | Keys.Y] = FCTBAction.Redo; // CTRL + Y for redo
            Language = Language.Custom;

            if (IsDesignMode)
                return;

            //cache previous text
            TextChanging += (s, e) =>
            {
                if (Disposing || IsDisposed || !diffGeneration
                    || e.Cancel || (OnInsertion == null && OnDeletion == null))
                    return;
                previousLines = Lines.ToList();
            };

            //diffs
            TextChanged += (s, e) =>
            {
                if (Disposing || IsDisposed || !diffGeneration
                    || (OnInsertion == null && OnDeletion == null))
                    return;

                //figure out what actually changed (don't do a diff on the whole content)
                Range range = e.ChangedRange.Clone();
                range.Normalize();
                char[] oldData = null, newData = null;
                int offset = PlaceToPosition(range.Start);
                var linesDelta = LinesCount - previousLines.Count;
                if (linesDelta == 0)
                {
                    oldData = previousLines[range.FromLine].ToCharArray();
                    newData = Lines[range.FromLine].ToCharArray();
                }
                else
                {
                    //build old string
                    StringBuilder sb = new StringBuilder();
                    var end = linesDelta < 0 ? range.Start.iLine + Math.Abs(linesDelta) : range.End.iLine - linesDelta;
                    for (int i = range.Start.iLine; i <= end; ++i)
                    {
                        if (i > range.Start.iLine)
                            sb.AppendLine();
                        sb.Append(previousLines[i]);
                    }
                    var oldValue = sb.ToString();

                    //build new string
                    sb.Clear();
                    for (int i = range.Start.iLine; i <= range.End.iLine; ++i)
                    {
                        if (i > range.Start.iLine)
                            sb.AppendLine();
                        sb.Append(Lines[i]);
                    }
                    var newValue = sb.ToString();

                    oldData = oldValue.ToCharArray();
                    newData = newValue.ToCharArray();
                }

                //build diff
                var diffs = Diff.Calculate(oldData, newData);

                //fire events
                string newDataString = new string(newData);
                foreach (var diff in diffs)
                {
                    int position = Math.Min(diff.InsertStart + offset, TextLength);

                    //process a deletion
                    if (diff.DeleteLength > 0)
                        OnDeletion?.Invoke(this, position, diff.DeleteLength);

                    //process an insertion
                    if (position < (offset + diff.InsertStart + diff.InsertLength))
                        OnInsertion?.Invoke(this, position, newDataString.Substring(diff.InsertStart, diff.InsertLength));
                }
            };

            //themes
            ApplyTheme(App.Theme);
            App.ThemeChanged += ApplyTheme;
        }

        /////////////////////////////////////////////////////////////////////
        // APPLY THEME
        /////////////////////////////////////////////////////////////////////

        public virtual void ApplyTheme(Theme t)
        {
            if (t == null)
                return;

            ForeColor = t.Foreground.Colour;
            BackBrush = t.Workspace.Brush;
            IndentBackColor = t.Workspace.Colour;
            ServiceLinesColor = t.Workspace.HighContrast.Colour;
            Font = t.Monospaced.Regular;
        }

        /////////////////////////////////////////////////////////////////////
        // TEXT INSERTION/DELETION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Insert text into replaceRange and restore previous selection
        /// (this is exactly the same code as FCTB's InsertTextAndRestoreSelection, but provides jumpToCaret)
        /// </summary>
        public virtual Range InsertTextAndRestoreSelection(Range replaceRange, string text, Style style, bool jumpToCaret)
        {
            if (text == null)
                return null;

            var oldStart = PlaceToPosition(Selection.Start);
            var oldEnd = PlaceToPosition(Selection.End);
            var count = replaceRange.Text.Length;
            var pos = PlaceToPosition(replaceRange.Start);
            //
            Selection.BeginUpdate();
            Selection = replaceRange;
            var range = InsertText(text, style, jumpToCaret);
            //
            count = range.Text.Length - count;
            Selection.Start = PositionToPlace(oldStart + (oldStart >= pos ? count : 0));
            Selection.End = PositionToPlace(oldEnd + (oldEnd >= pos ? count : 0));
            Selection.EndUpdate();
            return range;
        }

        /// <summary>
        /// Insert text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Insert postion</param>
        /// <param name="text">New text</param>
        void IEditorTextBox.InsertText(int offset, string text)
        {
            InsertTextAndRestoreSelection(
                new Range(this, PositionToPlace(offset), PositionToPlace(offset)),
                    text, null, false);
        }

        /// <summary>
        /// Delete text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Deletion start position</param>
        /// <param name="length">Deletion length</param>
        void IEditorTextBox.DeleteText(int offset, int length)
        {
            InsertTextAndRestoreSelection(
                new Range(this, PositionToPlace(offset), PositionToPlace(offset + length)),
                    "", null, false);
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!IsDisposed)
                {
                    if (disposing)
                    {
                        if (!IsDesignMode)
                            App.ThemeChanged -= ApplyTheme;
                        OnInsertion = null;
                        OnDeletion = null;
                        lock (currentLanguageLock)
                        {
                            currentLanguage = null;
                        }
                        previousLines = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
