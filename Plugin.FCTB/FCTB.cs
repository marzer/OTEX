﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastColoredTextBoxNS;
using System.Windows.Forms;
using System.ComponentModel;
using Marzersoft;
using Marzersoft.Themes;
using System.Drawing;


namespace OTEX.Editor.Plugins
{
    /// <summary>
    /// OTEX Editor subclass of the FastColoredTextBox. Implements Marzersoft.IThemeable and handles
    /// refresh logic, text version caching, etc.
    /// </summary>
    public class FCTB : FastColoredTextBox, IThemeable, IEditorTextBox
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Fired when text is inserted into the document (if DiffEvents == true).
        /// parameters: sender, starting index, inserted text.
        /// </summary>
        public event Action<IEditorTextBox, uint, string> OnInsertion;

        /// <summary>
        /// Fired when text is deleted from the document (if DiffEvents == true).
        /// parameters: sender, starting index, length of deleted text.
        /// </summary>
        public event Action<IEditorTextBox, uint, uint> OnDeletion;

        /// <summary>
        /// Fired when the user's current selection changes.
        /// parameters: sender, starting index, length of selection.
        /// </summary>
        public event Action<IEditorTextBox, uint, uint> OnSelection;

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
                    value = Color.FromArgb(255, value);
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

        /// <summary>
        /// Ruler rendering.
        /// </summary>
        private bool ruler = true;
        private uint rulerOffset = 100;

        /// <summary>
        /// Highlight ranges object.
        /// </summary>
        private readonly HighlightRanges ranges = new HighlightRanges();

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public FCTB()
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
                        OnDeletion?.Invoke(this, (uint)position, (uint)diff.DeleteLength);

                    //process an insertion
                    if (position < (offset + diff.InsertStart + diff.InsertLength))
                        OnInsertion?.Invoke(this, (uint)position, newDataString.Substring(diff.InsertStart, diff.InsertLength));
                }
            };

            //selection
            SelectionChanged += (s, e) =>
            {
                var sel = Selection;
                OnSelection?.Invoke(this, (uint)PlaceToPosition(sel.Start), (uint)PlaceToPosition(sel.End));
            };

            //highlight ranges
            ranges.OnAdded += (hr, r) => { this.Execute(Refresh); };
            ranges.OnChanged += (hr, r) => { this.Execute(Refresh); };
            ranges.OnRemoved += (hr, r) => { this.Execute(Refresh); };
            ranges.OnCleared += (hr) => { this.Execute(Refresh); };

            //themes
            ApplyTheme(App.Theme);
            App.ThemeChanged += ApplyTheme;
        }


        /////////////////////////////////////////////////////////////////////
        // THEMES AND STYLES
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
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
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
        void IEditorTextBox.InsertText(uint offset, string text)
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            InsertTextAndRestoreSelection(
                new Range(this, PositionToPlace((int)offset), PositionToPlace((int)offset)),
                    text, null, false);
        }

        /// <summary>
        /// Delete text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Deletion start position</param>
        /// <param name="length">Deletion length</param>
        void IEditorTextBox.DeleteText(uint offset, uint length)
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            InsertTextAndRestoreSelection(
                new Range(this, PositionToPlace((int)offset), PositionToPlace((int)(offset + length))),
                    "", null, false);
        }

        /// <summary>
        /// Clear the contents of the text box.
        /// </summary>
        void IEditorTextBox.ClearText()
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            Text = "";
        }

        /////////////////////////////////////////////////////////////////////
        // CLEAR UNDO
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clear the undo history stack.
        /// </summary>
        void IEditorTextBox.ClearUndoHistory()
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            ClearUndo();
        }

        /////////////////////////////////////////////////////////////////////
        // RULER
        /////////////////////////////////////////////////////////////////////

        void IEditorTextBox.SetRuler(bool visible, uint offset)
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            if (ruler != visible || rulerOffset != offset)
            {
                ruler = visible;
                rulerOffset = offset;
                this.Execute(Refresh);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsDesignMode)
                return;

            if (ruler)
            {
                e.Graphics.AsQuality(GraphicsQuality.High, (g) =>
                {
                    var pt = PlaceToPoint(new Place((int)rulerOffset, 0));
                    using (Pen p = new Pen(Color.FromArgb(32, userColour)))
                    {
                        p.Width = 2;
                        p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        e.Graphics.DrawLine(p, new Point(pt.X, e.ClipRectangle.Top), new Point(pt.X, e.ClipRectangle.Bottom));
                    }
                });
            }
        }

        /////////////////////////////////////////////////////////////////////
        // HIGHLIGHT RANGES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set/update a custom highlight range for a specific id.
        /// </summary>
        /// <param name="id">Unique id of range.</param>
        /// <param name="start">Range start index</param>
        /// <param name="end">Range end index</param>
        /// <param name="colour">Colour to use for highlighting</param>
        void IEditorTextBox.SetHighlightRange(Guid id, uint start, uint end, Color colour)
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            ranges.Set(id, start, end, colour);
        }

        /// <summary>
        /// Clears all custom highlight ranges.
        /// </summary>
        void IEditorTextBox.ClearHighlightRanges()
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            ranges.Clear();
        }

        protected override void OnPaintLine(PaintLineEventArgs e)
        {
            base.OnPaintLine(e);
            if (IsDesignMode)
                return;

            var highlightRanges = ranges.Ranges;
            if (highlightRanges == null || highlightRanges.Length == 0)
                return;

            Range lineRange = new Range(this, e.LineIndex);
            var len = (uint)TextLength;
            foreach (var highlightRange in highlightRanges)
            {
                if (highlightRange == null)
                    continue;
                
                //check range
                var selStart = PositionToPlace((int)highlightRange.Start.Clamp(0, len));
                var selEnd = PositionToPlace((int)highlightRange.End.Clamp(0, len));
                var selRange = new Range(this, selStart, selEnd);
                var hlRange = lineRange.GetIntersectionWith(selRange);
                if (hlRange.Length == 0 && !lineRange.Contains(selStart))
                    continue;

                var ptStart = PlaceToPoint(hlRange.Start);
                var ptEnd = PlaceToPoint(hlRange.End);
                var caret = lineRange.Contains(selStart);

                //draw "current line" fill
                if (caret && selRange.Length == 0)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(12, highlightRange.Colour)))
                        e.Graphics.FillRectangle(b, e.LineRect);
                }
                //draw highlight
                if (hlRange.Length > 0)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(32, highlightRange.Colour)))
                        e.Graphics.FillRectangle(b, new Rectangle(ptStart.X, e.LineRect.Y,
                            ptEnd.X - ptStart.X, e.LineRect.Height));
                }
                //draw caret
                if (caret)
                {
                    ptStart = PlaceToPoint(selStart);
                    using (Pen p = new Pen(Color.FromArgb(190, highlightRange.Colour)))
                    {
                        p.Width = 2;
                        e.Graphics.DrawLine(p, ptEnd.X, e.LineRect.Top,
                            ptEnd.X, e.LineRect.Bottom);
                    }
                }
            }
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
                        OnSelection = null;
                        lock (currentLanguageLock)
                        {
                            currentLanguage = null;
                        }
                        previousLines = null;
                        ranges.Clear();
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
