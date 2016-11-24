using Marzersoft.Themes;
using Marzersoft;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace OTEX.Editor
{
    public class Scintilla : ScintillaNET.Scintilla, IThemeable, IEditorTextBox
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

                //apply user colour
                //(alpha values match those of FCTB)
                userColour = value;
                SetSelectionBackColor(true, Styles[Style.Default].BackColor.Blend(userColour, 60));
                CaretLineBackColor = userColour;
                CaretLineBackColorAlpha = 50;
                EdgeColor = Styles[Style.Default].BackColor.Blend(userColour, 32);
                CaretForeColor = userColour.Brighten(0.3f);
                Styles[Style.LineNumber].ForeColor = userColour;
                for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++)
                    Markers[i].SetBackColor(userColour);
            }
        }
        private Color userColour = Color.White;

        /// <summary>
        /// The current language definitions used for syntax highlighting.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LanguageManager.Language Language
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                lock (currentLanguageLock)
                {
                    return currentLanguage;
                }
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
                    UpdateStyles(false);
                }
            }
        }
        private LanguageManager.Language currentLanguage = null;
        private readonly object currentLanguageLock = new object();

        /// <summary>
        /// Highlight ranges object.
        /// </summary>
        private readonly HighlightRanges ranges = new HighlightRanges();

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public Scintilla()
        {
            //setup
            WrapMode = WrapMode.Word;
            WrapStartIndent = 4;
            WrapIndentMode = WrapIndentMode.Indent;
            TabWidth = 4;
            BorderStyle = System.Windows.Forms.BorderStyle.None;
            ExtraAscent = 1;
            ExtraDescent = 1;
            MultipleSelection = false;
            MouseSelectionRectangularSwitch = false;
            AdditionalSelectionTyping = false;
            VirtualSpaceOptions = VirtualSpace.None;
            Margins[2].Type = MarginType.Symbol;
            Margins[2].Mask = Marker.MaskFolders;
            Margins[2].Sensitive = true;
            Margins[2].Width = 0;
            CaretLineVisible = true;
            AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);
            Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;
            FontQuality = FontQuality.LcdOptimized;
            Technology = Technology.DirectWrite;
            BufferedDraw = false; //don't need it with directwrite
            EdgeMode = EdgeMode.Line;
            EdgeColumn = 100;
            for (int i = 8; i <= 31; ++i)
            {
                Indicators[i].Style = IndicatorStyle.Hidden;
                Indicators[i].Under = true;
                Indicators[i].OutlineAlpha = 64;
                Indicators[i].Alpha = 32;
            }

            /*
            HotkeysMapping.Remove(Keys.Control | Keys.H); //remove default "replace" (CTRL + H, wtf?)
            HotkeysMapping[Keys.Control | Keys.R] = FCTBAction.ReplaceDialog; // CTRL + R for replace
            */

            if (IsDesignMode)
                return;

            //insert diff
            Insert += (s, e) =>
            {
                if (diffGeneration)
                    OnInsertion?.Invoke(this, (uint)e.Position, e.Text);
            };

            //delete diff
            Delete += (s, e) =>
            {
                if (diffGeneration)
                    OnDeletion?.Invoke(this, (uint)e.Position, (uint)e.Text.Length);
            };

            //selection changes
            UpdateUI += (s, e) =>
            {
                if ((e.Change & UpdateChange.Selection) != 0)
                {
                    CaretLineVisible = (AnchorPosition == CurrentPosition);
                    OnSelection?.Invoke(this, (uint)CurrentPosition, (uint)AnchorPosition);
                }
            };

            //zoom changes
            ZoomChanged += (s, e) =>
            {
                Margins[0].Width = TextWidth(Style.LineNumber, "00000") + 2;
            };

            //highlight ranges
            ranges.OnAdded += (hr, r) =>
            {
                Show(r);
            };
            ranges.OnColourChanged += (hr, r) =>
            {
                Update(r, true, false);
            };
            ranges.OnSelectionChanged += (hr, r) =>
            {
                Update(r, false, true);
            };
            ranges.OnRemoved += (hr, r) =>
            {
                Hide(r);
            };
            ranges.OnCleared += (hr) =>
            {
                for (int i = 8; i <= 31; ++i)
                    Indicators[i].Style = IndicatorStyle.Hidden;
            };

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

            lock (currentLanguageLock)
            {
                UpdateStyles(true);
            }

            for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++)
                Markers[i].SetForeColor(t.Workspace.Colour);
            SetFoldMarginHighlightColor(true, t.Workspace.Colour);
            SetFoldMarginColor(true, t.Workspace.Colour);
        }

        private void UpdateStyles(bool updateDefaults)
        {
            var oldLexer = Lexer;
            var newLexer = Lexer;

            if (updateDefaults)
            {                
                //set defaults
                StyleResetDefault();
                Styles[Style.Default].Font = App.Theme.Monospaced.Regular.FontFamily.Name;
                Styles[Style.Default].SizeF = App.Theme.Monospaced.Regular.SizeInPoints;
                Styles[Style.Default].BackColor = App.Theme.Workspace.Colour;
                Styles[Style.Default].ForeColor = App.Theme.Foreground.Colour;
                StyleClearAll();

                //margins
                Styles[Style.LineNumber].BackColor = App.Theme.Workspace.Colour;
                Styles[Style.LineNumber].ForeColor = userColour;
                Margins[0].Width = TextWidth(Style.LineNumber, "00000") + 2;

                //indentation guides
                Styles[Style.IndentGuide].ForeColor = App.Theme.Workspace.HighContrast.Colour;

                //selection colour
                SetSelectionBackColor(true, Styles[Style.Default].BackColor.Blend(userColour, 60));

                //line length indicator
                EdgeColor = Styles[Style.Default].BackColor.Blend(userColour, 32);
            }

            if (currentLanguage != null)
            {
                //C++ lexer
                if (currentLanguage.Name == "c"
                    || currentLanguage.Name == "cpp"
                    || currentLanguage.Name == "cs"
                    || currentLanguage.Name == "actionscript"
                    || currentLanguage.Name == "d"
                    || currentLanguage.Name == "java"
                    || currentLanguage.Name == "javascript"
                    || currentLanguage.Name == "javascript.js"
                    || currentLanguage.Name == "jsp"
                    || currentLanguage.Name == "objc"
                    || currentLanguage.Name == "rc"
                    || currentLanguage.Name == "verilog")
                {
                    //comments
                    Styles[Style.Cpp.Comment].ForeColor
                        = Styles[Style.Cpp.CommentLine].ForeColor
                        = SyntaxColours.Comments;

                    //documentation comments
                    Styles[Style.Cpp.CommentDoc].ForeColor
                        = Styles[Style.Cpp.CommentDocKeyword].ForeColor
                        = Styles[Style.Cpp.CommentDocKeywordError].ForeColor
                        = Styles[Style.Cpp.CommentLineDoc].ForeColor
                        = SyntaxColours.Documentation;

                    //preprocessor
                    Styles[Style.Cpp.Preprocessor].ForeColor
                        = Styles[Style.Cpp.PreprocessorComment].ForeColor
                        = Styles[Style.Cpp.PreprocessorCommentDoc].ForeColor
                        = SyntaxColours.PreprocessorDirectives;

                    //strings
                    Styles[Style.Cpp.String].ForeColor
                        = Styles[Style.Cpp.StringEol].ForeColor
                        = Styles[Style.Cpp.StringRaw].ForeColor
                        = Styles[Style.Cpp.Character].ForeColor
                        = Styles[Style.Cpp.HashQuotedString].ForeColor
                        = Styles[Style.Cpp.TripleVerbatim].ForeColor
                        = Styles[Style.Cpp.Verbatim].ForeColor
                        = SyntaxColours.Strings;

                    //numbers, user-defined literals
                    Styles[Style.Cpp.Number].ForeColor
                        = Styles[Style.Cpp.UserLiteral].ForeColor
                        = SyntaxColours.Numbers;

                    //keywords/types/type modifiers
                    Styles[Style.Cpp.Word].ForeColor
                        = Styles[Style.Cpp.Word2].ForeColor
                        = SyntaxColours.Keywords;

                    IndentationGuides = IndentView.LookBoth;                   
                    newLexer = Lexer.Cpp;
                }

                //python lexer
                else if (currentLanguage.Name == "python")
                {
                    //comments
                    Styles[Style.Python.CommentBlock].ForeColor
                        = Styles[Style.Python.CommentLine].ForeColor
                        = SyntaxColours.Comments;

                    //numbers
                    Styles[Style.Python.Number].ForeColor
                        = SyntaxColours.Numbers;

                    //strings
                    Styles[Style.Python.String].ForeColor
                        = Styles[Style.Python.StringEol].ForeColor
                        = Styles[Style.Python.Character].ForeColor
                        = Styles[Style.Python.Triple].ForeColor
                        = Styles[Style.Python.TripleDouble].ForeColor
                        = SyntaxColours.Strings;

                    //keywords
                    Styles[Style.Python.Word].ForeColor
                        = Styles[Style.Python.Word2].ForeColor
                        = SyntaxColours.Keywords;

                    //decorators
                    Styles[Style.Python.Decorator].ForeColor
                        = SyntaxColours.PreprocessorDirectives;

                    IndentationGuides = IndentView.LookForward;
                    newLexer = Lexer.Python;
                }
            }
            else
                newLexer = Lexer.Null;


            if (oldLexer != newLexer)
            {
                Lexer = newLexer;
                if (newLexer == Lexer.Null)
                {
                    Margins[1].Width = 4;
                    Margins[2].Width = 0;
                    IndentationGuides = IndentView.None;
                }
                else
                {
                    Margins[1].Width = 16;
                    Margins[2].Width = 20;
                    SetProperty("fold", "1");
                    SetProperty("fold.compact", "1");

                    var kwg = currentLanguage.KeywordGroups;
                    for (int i = 0; i < kwg.Count; ++i)
                        SetKeywords(i, string.Join(" ", currentLanguage[kwg[i]]));
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        // TEXT INSERTION/DELETION
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Insert text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Insert postion</param>
        /// <param name="text">New text</param>
        void IEditorTextBox.InsertText(uint offset, string text)
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            InsertText((int)offset, text);
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
            DeleteRange((int)offset, (int)length);
        }

        /// <summary>
        /// Clear the contents of the text box.
        /// </summary>
        void IEditorTextBox.ClearText()
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            ClearAll();
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
            EmptyUndoBuffer();
        }

        /////////////////////////////////////////////////////////////////////
        // RULER
        /////////////////////////////////////////////////////////////////////

        void IEditorTextBox.SetRuler(bool visible, uint offset)
        {
            if (IsDisposed || Disposing)
                throw new ObjectDisposedException(GetType().Name);
            EdgeMode = visible ? EdgeMode.Line : EdgeMode.None;
            EdgeColumn = (int)offset;
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

        private void Show(HighlightRanges.Range range)
        {
            int ind = range.Index + 8;
            if (ind > 31)
                return;
            Indicators[ind].Style = IndicatorStyle.StraightBox;
            Update(range,true,true);
        }

        private void Update(HighlightRanges.Range range, bool colour, bool selection)
        {
            int ind = range.Index + 8;
            if (ind > 31)
                return;
            if (colour)
                Indicators[ind].ForeColor = range.Colour;
            if (selection)
            {
                IndicatorCurrent = ind;
                IndicatorClearRange(0, TextLength);
                IndicatorFillRange((int)range.Low, (int)range.Length);
            }
        }

        private void Hide(HighlightRanges.Range range)
        {
            int ind = range.Index + 8;
            if (ind > 31)
                return;
            Indicators[ind].Style = IndicatorStyle.Hidden;
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