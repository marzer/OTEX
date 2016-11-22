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

namespace OTEX
{
    /// <summary>
    /// OTEX Editor subclass of the FastColoredTextBox. Implements Marzersoft.IThemeable and handles
    /// refresh logic, text version caching, etc.
    /// </summary>
    public class EditorTextBox : FastColoredTextBox, IThemeable
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
        /// Fired when the text box contents has changed (if DiffGeneration == true).
        /// parameters: sender, previous text, current text, diffs.
        /// </summary>
        public event Action<EditorTextBox, string, string, Diff.Item[]> DiffsGenerated;

        /// <summary>
        /// Is the DiffsGenerated event fired when the text is changed?
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DiffGeneration
        {
            get { return diffGeneration; }
            set { diffGeneration = value; }
        }
        private volatile bool diffGeneration = true;

        /// <summary>
        /// The user colour as applied to this editor text box.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color UserColour
        {
            get { return userColour; }
            set
            {
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
        private string previousText = "";

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public EditorTextBox()
        {
            ReservedCountOfLineNumberChars = 4;
            WordWrap = true;
            WordWrapAutoIndent = true;
            WordWrapMode = WordWrapMode.WordWrapControlWidth;
            TabLength = 4;
            LineInterval = 2;
            HotkeysMapping.Remove(Keys.Control | Keys.H); //remove default "replace" (CTRL + H, wtf?)
            HotkeysMapping[Keys.Control | Keys.R] = FCTBAction.ReplaceDialog; // CTRL + R for replace
            HotkeysMapping[Keys.Control | Keys.Y] = FCTBAction.Undo; // CTRL + Y for undo
            HighlightingRangeType = HighlightingRangeType.AllTextRange;

            if (IsDesignMode)
                return;

            //cache previous text
            TextChanging += (s, e) =>
            {
                if (!e.Cancel)
                    previousText = Text;
            };

            //diff
            TextChanged += (s, e) =>
            {
                if (Disposing || IsDisposed || !diffGeneration || DiffsGenerated == null)
                    return;

                var currentText = Text;
                var diffs = Diff.Calculate(previousText.ToCharArray(), currentText.ToCharArray());
                DiffsGenerated?.Invoke(this, previousText, currentText, diffs);
            };

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
            if (Language != Language.Custom)
            {
                ClearStyle(StyleIndex.All);
                SyntaxHighlighter.HighlightSyntax(Language, Range);
            }
        }

        /////////////////////////////////////////////////////////////////////
        // TEXT INSERTION
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
                        DiffsGenerated = null;
                        previousText = null;
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
