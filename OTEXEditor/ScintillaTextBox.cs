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
    public class ScintillaTextBox : Scintilla, IThemeable, IEditorTextBox
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
                userColour = value;
                SetSelectionBackColor(true, App.Theme == null || App.Theme.IsDark
                    ? Styles[Style.Default].BackColor.Blend(userColour, 60)
                    : userColour.Blend(Styles[Style.Default].BackColor, 60));
                CaretLineBackColor = userColour;
                EdgeColor = App.Theme == null || App.Theme.IsDark
                    ? Styles[Style.Default].BackColor.Blend(userColour, 32)
                    : userColour.Blend(Styles[Style.Default].BackColor, 32);
                Styles[Style.LineNumber].ForeColor = userColour;
                Markers[BookmarkMarker].SetBackColor(userColour);
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
        private readonly HighlightRanges ranges = new HighlightRanges(24);

        /// <summary>
        /// Bookmark marker mask.
        /// </summary>
        private const int BookmarkMarker = 1;
        private const uint BookmarkMask = (1 << BookmarkMarker);

        /// <summary>
        /// Are line ending characters visible?
        /// </summary>
        public bool LineEndingsVisible
        {
            get { return ViewEol; }
            set { ViewEol = value; }
        }

        public bool VerticalScrollbarVisible
        {
            get { return verticalScrollbarVisible; }
            private set
            {
                if (value == verticalScrollbarVisible)
                    return;
                verticalScrollbarVisible = value;
            }
        }
        private volatile bool verticalScrollbarVisible = false;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public ScintillaTextBox()
        {
            //setup
            WrapMode = WrapMode.Word;
            WrapStartIndent = 4;
            WrapIndentMode = WrapIndentMode.Indent;
            TabWidth = 4;
            BorderStyle = BorderStyle.None;
            ExtraAscent = 1;
            ExtraDescent = 1;
            MultipleSelection = false;
            MouseSelectionRectangularSwitch = false;
            AdditionalSelectionTyping = false;
            VirtualSpaceOptions = VirtualSpace.None;
            Margins[1].Width = 16;
            Margins[1].Type = MarginType.Symbol;
            Margins[1].Mask = BookmarkMask;
            Margins[1].Sensitive = true;
            Margins[1].Cursor = MarginCursor.Arrow;
            Margins[2].Width = 0;
            Margins[2].Type = MarginType.Symbol;
            Margins[2].Mask = Marker.MaskFolders;
            Margins[2].Sensitive = true;
            CaretLineVisible = true;
            AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);
            Markers[BookmarkMarker].Symbol = MarkerSymbol.Bookmark;
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
            CaretLineBackColorAlpha = 50;
            for (int i = 8; i <= 31; ++i)
            {
                Indicators[i].Style = IndicatorStyle.Hidden;
                Indicators[i].Under = true;
                Indicators[i].OutlineAlpha = 64;
                Indicators[i].Alpha = 32;
            }
            switch (Environment.NewLine)
            {
                case "\r": EolMode = Eol.Cr; break;
                case "\n": EolMode = Eol.Lf; break;
                default: EolMode = Eol.CrLf; break;
            }
            PasteConvertEndings = true;

            if (IsDesignMode)
                return;

            //hotkeys
            ClearAllCmdKeys();
            //explicitly disable certain key combinations first
            //(to prevent weird characters or strange behaviour)
            for (Keys key = Keys.A; key <= Keys.Z; ++key)
                NullCmdKey(key);
            for (Keys key = Keys.D0; key <= Keys.D9; ++key)
                NullCmdKey(key);
            NullCmdKey(Keys.Enter);
            //assign regular scintilla hotkeys
            AssignCmdKey(Keys.Left, Command.CharLeft);
            AssignCmdKey(Keys.Right, Command.CharRight);
            AssignCmdKey(Keys.Up, Command.LineUp);
            AssignCmdKey(Keys.Down, Command.LineDown);
            AssignCmdKey(Keys.Home, Command.Home);
            AssignCmdKey(Keys.End, Command.LineEnd);
            AssignCmdKey(Keys.PageDown, Command.PageDown);
            AssignCmdKey(Keys.PageUp, Command.PageUp);
            AssignCmdKey(Keys.Shift | Keys.Left, Command.CharLeftExtend);
            AssignCmdKey(Keys.Shift | Keys.Right, Command.CharRightExtend);
            AssignCmdKey(Keys.Shift | Keys.Up, Command.LineUpExtend);
            AssignCmdKey(Keys.Shift | Keys.Down, Command.LineDownExtend);
            AssignCmdKey(Keys.Shift | Keys.Home, Command.HomeExtend);
            AssignCmdKey(Keys.Shift | Keys.End, Command.LineEndExtend);
            AssignCmdKey(Keys.Shift | Keys.PageDown, Command.PageDownExtend);
            AssignCmdKey(Keys.Shift | Keys.PageUp, Command.PageUpExtend);
            AssignCmdKey(Keys.Control | Keys.X, MissingCommands.Cut);
            AssignCmdKey(Keys.Control | Keys.C, MissingCommands.Copy);
            AssignCmdKey(Keys.Control | Keys.V, MissingCommands.Paste);
            AssignCmdKey(Keys.Control | Keys.A, Command.SelectAll);
            AssignCmdKey(Keys.Control | Keys.Z, Command.Undo);
            AssignCmdKey(Keys.Control | Keys.Y, Command.Redo);
            AssignCmdKey(Keys.Tab, Command.Tab);
            AssignCmdKey(Keys.Shift | Keys.Tab, Command.BackTab);
            AssignCmdKey(Keys.Control | Keys.Home, Command.DocumentStart);
            AssignCmdKey(Keys.Control | Keys.End, Command.DocumentEnd);
            AssignCmdKey(Keys.Control | Keys.Shift | Keys.Home, Command.DocumentStartExtend);
            AssignCmdKey(Keys.Control | Keys.Shift | Keys.End, Command.DocumentEndExtend);
            AssignCmdKey(Keys.Control | Keys.Left, Command.WordLeft);
            AssignCmdKey(Keys.Control | Keys.Right, Command.WordRight);
            AssignCmdKey(Keys.Control | Keys.Shift | Keys.Left, Command.WordLeftExtend);
            AssignCmdKey(Keys.Control | Keys.Shift | Keys.Right, Command.WordRightExtend);
            AssignCmdKey(Keys.Insert, Command.EditToggleOvertype);
            AssignCmdKey(Keys.Control | Keys.Back, Command.DelWordLeft);
            AssignCmdKey(Keys.Control | Keys.Delete, Command.DelWordRight);
            AssignCmdKey(Keys.Control | Keys.Up, Command.LineScrollUp);
            AssignCmdKey(Keys.Control | Keys.Down, Command.LineScrollDown);
            AssignCmdKey(Keys.Back, Command.DeleteBack);
            AssignCmdKey(Keys.Delete, MissingCommands.Clear);
            AssignCmdKey(Keys.Shift | Keys.Delete, Command.LineDelete);
            AssignCmdKey(Keys.Alt | Keys.Up, Command.MoveSelectedLinesUp);
            AssignCmdKey(Keys.Alt | Keys.Down, Command.MoveSelectedLinesDown);
            AssignCmdKey(Keys.Enter, Command.NewLine);
            AssignCmdKey(Keys.Shift | Keys.Enter, Command.NewLine);

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

            //margin click events
            MarginClick += (s, e) =>
            {
                if (e.Margin == 1)
                    ToggleBookmark(LineFromPosition(e.Position));
            };

            //scrollbar
            Layout += (s, e) =>
            {
                VerticalScrollbarVisible = this.VerticalScrollbarVisible();
            };

            //themes
            ApplyTheme(App.Theme);
            App.ThemeChanged += ApplyTheme;

            Logger.I("ScintillaNET Editor plugin loaded.");
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

            Markers[BookmarkMarker].SetForeColor(t.Workspace.Colour);
            for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++)
                Markers[i].SetForeColor(t.Workspace.Colour);
            SetFoldMarginHighlightColor(true, t.Workspace.Colour);
            SetFoldMarginColor(true, t.Workspace.Colour);
            CaretForeColor = t == null || !t.IsDark ? Color.Black : Color.White;
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
                SetSelectionBackColor(true, App.Theme == null || App.Theme.IsDark ? Styles[Style.Default].BackColor.Blend(userColour, 60)
                    : userColour.Blend(Styles[Style.Default].BackColor, 60));

                //line length indicator
                EdgeColor = App.Theme == null || App.Theme.IsDark ? Styles[Style.Default].BackColor.Blend(userColour, 32)
                    : userColour.Blend(Styles[Style.Default].BackColor, 32);
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
#if DEBUG
                if (newLexer != Lexer.Null)
                    Logger.V("Scintilla Lexer: {0}\nKeywords:\n{1}", newLexer, DescribeKeywordSets().SplitLines().Print());
#endif
                if (newLexer == Lexer.Null)
                {
                    Margins[2].Width = 0;
                    IndentationGuides = IndentView.None;
                }
                else
                {
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
        // HOTKEYS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Scintilla command identifiers missing from ScintillaNET's Commands enum.
        /// </summary>
        public enum MissingCommands : int
        {
            Cut = 2177,
            Copy = 2178,
            Paste = 2179,
            Clear = 2180
        }

        public void AssignCmdKey(Keys keyDefinition, MissingCommands sciCommand)
        {
            AssignCmdKey(keyDefinition, (Command)((int)sciCommand));
        }

        public void NullCmdKey(Keys key, bool nullBase = false)
        {
            key &= ~(Keys.Control | Keys.Alt | Keys.Shift);
            if (nullBase)
                AssignCmdKey(key, Command.Null);
            AssignCmdKey(Keys.Control | key, Command.Null);
            AssignCmdKey(Keys.Control | Keys.Alt | key, Command.Null);
            AssignCmdKey(Keys.Control | Keys.Shift | key, Command.Null);
            AssignCmdKey(Keys.Control | Keys.Shift | Keys.Alt | key, Command.Null);
        }

        /////////////////////////////////////////////////////////////////////
        // BOOKMARKS
        /////////////////////////////////////////////////////////////////////

        public void ToggleBookmark(int atLine)
        {
            var line = Lines[atLine];
            if ((line.MarkerGet() & BookmarkMask) == 0u)
                line.MarkerAdd(BookmarkMarker);
            else
                line.MarkerDelete(BookmarkMarker);
        }

        void IEditorTextBox.ToggleBookmark()
        {
            ToggleBookmark(CurrentLine);
        }

        public bool GoToPreviousBookmark(int fromLine, bool wrap = true)
        {
            if (fromLine <= 0)
                fromLine = Lines.Count;
            --fromLine;
            if (fromLine < 0 || fromLine >= Lines.Count)
                return false;

            var prevLine = Lines[fromLine].MarkerPrevious(BookmarkMask);
            if (prevLine != -1)
            {
                Lines[prevLine].Goto();
                return true;
            }
            else if (wrap)
                return GoToPreviousBookmark(-1, false);
            return false;
        }

        public bool GoToNextBookmark(int fromLine, bool wrap = true)
        {
            if (fromLine >= Lines.Count - 1)
                fromLine = -1;
            ++fromLine;
            if (fromLine < 0 || fromLine >= Lines.Count)
                return false;

            var nextLine = Lines[fromLine].MarkerNext(BookmarkMask);
            if (nextLine != -1)
            {
                Lines[nextLine].Goto();
                return true;
            }
            else if (wrap)
                return GoToNextBookmark(Lines.Count, false);
            return false;
        }

        void IEditorTextBox.NextBookmark()
        {
            GoToNextBookmark(CurrentLine);
        }

        void IEditorTextBox.PreviousBookmark()
        {
            GoToPreviousBookmark(CurrentLine);
        }

        /////////////////////////////////////////////////////////////////////
        // COMMENTING LINES
        /////////////////////////////////////////////////////////////////////

        private bool CommentRegion(int start, int end, int mode,
            out int firstCommentLine, out int firstCommentOffset,
            out int lastCommentLine, out int lastCommentOffset,
            out int actualMode)
        {
            start = start.Clamp(0, TextLength);
            end = end.Clamp(0, TextLength);
            firstCommentLine = firstCommentOffset = -1;
            lastCommentLine = lastCommentOffset = -1;
            mode = mode.Clamp(-1, 1);
            actualMode = mode;

            //get line numbers
            int firstLine = LineFromPosition(start > end ? end : start);
            if (firstLine == -1)
                return false;
            int lastLine = LineFromPosition(start > end ? start : end);
            if (lastLine == -1)
                lastLine = firstLine;

            //enumerate commentable lines
            List<int> allLines = new List<int>();
            for (int l = firstLine; l <= lastLine; ++l)
            {
                if (Lines[l].Length == 0 || Lines[l].Text.IsWhitespace())
                    continue;
                allLines.Add(l);
            }
            if (allLines.Count == 0)
                return false;

            //"automatic mode"
            if (mode == 0)
                actualMode = mode = currentLanguage.IsCommented(Lines[allLines[0]].Text) ? -1 : 1;
            bool insert = mode == 1;

            //enumerate modifiable lines
            int insertIndex = int.MaxValue;
            List<int> lines = new List<int>();
            foreach (var l in allLines)
            {
                var line = Lines[l].Text;
                if (currentLanguage.IsCommented(line) == insert)
                    continue;
                lines.Add(l);
                if (insert)
                    insertIndex = Math.Min(insertIndex, line.FirstNonWhitespaceIndex());
            }
            if (lines.Count == 0)
                return false;

            //modify lines
            BeginUndoAction();
            firstCommentLine = lines[0];
            lastCommentLine = lines[lines.Count-1];
            if (insert)
                firstCommentOffset = lastCommentOffset = insertIndex;
            for (int i = 0; i < lines.Count; ++i)
            {
                if (insert)
                    InsertText(Lines[lines[i]].Position + insertIndex, currentLanguage.CommentLine);
                else
                {
                    int offset = Lines[lines[i]].Text.FirstNonWhitespaceIndex();
                    DeleteRange(Lines[lines[i]].Position + offset, currentLanguage.CommentLine.Length);
                    if (i == 0)
                        firstCommentOffset = offset;
                    if (i == lines.Count - 1)
                        lastCommentOffset = offset;
                }
            }
            EndUndoAction();
            return true;
        }

        private void CommentSelection(int mode)
        {
            lock (currentLanguageLock)
            {
                if (currentLanguage == null || currentLanguage.CommentLine.Length == 0)
                    return;

                int delta = currentLanguage.CommentLine.Length;
                int caret = CurrentPosition;
                int caretLine = LineFromPosition(caret);
                int caretOffset = caret - Lines[caretLine].Position;
                int anchor = AnchorPosition;
                int anchorLine = LineFromPosition(anchor);
                int anchorOffset = anchor - Lines[anchorLine].Position;
                if (CommentRegion(caret, anchor, mode, out int firstLine, out int firstOffset,
                    out int lastLine, out int lastOffset, out int actualMode))
                {
                    if ((caretLine == firstLine && caretOffset >= firstOffset)
                        || (caretLine == lastLine && caretOffset >= lastOffset))
                        caretOffset += delta * actualMode;
                    CurrentPosition = Lines[caretLine].Position + caretOffset;
                    if ((anchorLine == firstLine && anchorOffset >= firstOffset)
                        || (anchorLine == lastLine && anchorOffset >= lastOffset))
                        anchorOffset += delta * actualMode;
                    AnchorPosition = Lines[anchorLine].Position + anchorOffset;
                }
            }
        }

        void IEditorTextBox.CommentSelection()
        {
            CommentSelection(1);
        }

        void IEditorTextBox.ToggleCommentSelection()
        {
            CommentSelection(0);
        }

        void IEditorTextBox.UncommentSelection()
        {
            CommentSelection(-1);
        }

        /////////////////////////////////////////////////////////////////////
        // ZOOM
        /////////////////////////////////////////////////////////////////////

        void IEditorTextBox.IncreaseZoom()
        {
            ZoomIn();
        }

        void IEditorTextBox.DecreaseZoom()
        {
            ZoomOut();
        }

        void IEditorTextBox.ResetZoom()
        {
            Zoom = 0;
        }

        /////////////////////////////////////////////////////////////////////
        // CASE CONVERSION
        /////////////////////////////////////////////////////////////////////

        void IEditorTextBox.UppercaseSelection()
        {
            ExecuteCmd(Command.Uppercase);
        }

        void IEditorTextBox.LowercaseSelection()
        {
            ExecuteCmd(Command.Lowercase);
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
 
 