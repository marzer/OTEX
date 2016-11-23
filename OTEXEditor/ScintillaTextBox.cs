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

namespace OTEX
{
    public class ScintillaTextBox : Scintilla, IThemeable, IEditorTextBox
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
                    value = Color.FromArgb(255, value);
                if (userColour.Equals(value))
                    return;

                userColour = value;
                SetSelectionBackColor(true, userColour);
                CaretLineBackColor = userColour;
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

            /*
            HotkeysMapping.Remove(Keys.Control | Keys.H); //remove default "replace" (CTRL + H, wtf?)
            HotkeysMapping[Keys.Control | Keys.R] = FCTBAction.ReplaceDialog; // CTRL + R for replace
            */

            if (IsDesignMode)
                return;

            ZoomChanged += (s, e) =>
            {
                Margins[0].Width = TextWidth(Style.LineNumber, "00000") + 2;
            };

            Insert += (s, e) =>
            {
                Logger.I("inserted {0} characters at {1}", e.Text.Length, e.Position);
                if (Disposing || IsDisposed || !diffGeneration)
                    return;
                OnInsertion?.Invoke(this, e.Position, e.Text);
            };

            Delete += (s, e) =>
            {
                Logger.I("deleted {0} characters at {1}", e.Text.Length, e.Position);
                if (Disposing || IsDisposed || !diffGeneration)
                    return;
                OnDeletion?.Invoke(this, e.Position, e.Text.Length);
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


                    newLexer = Lexer.Python;
                }
            }
            else
                newLexer = Lexer.Null;


            if (oldLexer != newLexer)
            {
                Lexer = newLexer;
                if (newLexer == Lexer.Null)
                    Margins[2].Width = 0;
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
        /// Delete text without altering the current user's caret position or selection range.
        /// </summary>
        /// <param name="offset">Deletion start position</param>
        /// <param name="length">Deletion length</param>
        void IEditorTextBox.DeleteText(int offset, int length)
        {
            DeleteRange(offset, length);
        }

        /////////////////////////////////////////////////////////////////////
        // CLEAR UNDO
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Calls Scintilla's EmptyUndoBuffer().
        /// </summary>
        public void ClearUndo()
        {
            EmptyUndoBuffer();
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