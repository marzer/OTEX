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
        private string previousText = "";

        /// <summary>
        /// URL to download languages syntax file.
        /// </summary>
        private static readonly string LanguagesURL
            = "https://raw.githubusercontent.com/notepad-plus-plus/notepad-plus-plus/master/PowerEditor/src/langs.model.xml";

        /// <summary>
        /// Path to local languages syntax file.
        /// </summary>
        private static string LanguagesPath
        {
            get { return Path.Combine(App.ExecutableDirectoryPath, "..\\languages.xml"); }
        }

        /// <summary>
        /// Syntax highlighting load thead.
        /// </summary>
        private volatile Thread languagesThread = null;

        /// <summary>
        /// All loaded syntax highlighting languages.
        /// </summary>
        private readonly Dictionary<string, Lang> languages
            = new Dictionary<string, Lang>();

        /// <summary>
        /// The file extension for the purposes of determining the language for syntax highlighting.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LanguageFileExtension
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
                value = (value ?? "").Trim().ToLower();
                if (value.Length > 0 && value[0] == '.')
                    value = value.Substring(1);
                if (!value.Equals(currentLanguage))
                {
                    currentLanguage = value;
                    languages.TryLock(UpdateSelectedLanguage);
                }
            }
        }
        private string currentLanguage = "";

        /// <summary>
        /// currently selected language
        /// </summary>
        private Lang currentLang = null;

        /// <summary>
        /// Style for comment
        /// </summary>
        internal TextStyle CommentStyle
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                if (commentStyle == null)
                    commentStyle = new TextStyle(new SolidBrush(Color.FromArgb(87, 166, 74)), null, FontStyle.Regular);
                return commentStyle;
            }
        }
        private TextStyle commentStyle = null;

        /// <summary>
        /// Style for keywords
        /// </summary>
        internal TextStyle KeywordStyle
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                if (keywordStyle == null)
                    keywordStyle = new TextStyle(new SolidBrush(Color.FromArgb(86,156,214)), null, FontStyle.Regular);
                return keywordStyle;
            }
        }
        private TextStyle keywordStyle = null;

        /// <summary>
        /// Style for types
        /// </summary>
        internal TextStyle TypeStyle
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                if (typeStyle == null)
                    typeStyle = new TextStyle(new SolidBrush(Color.FromArgb(78,201,176)), null, FontStyle.Regular);
                return typeStyle;
            }
        }
        private TextStyle typeStyle = null;

        /// <summary>
        /// Style for literals
        /// </summary>
        internal TextStyle LiteralStyle
        {
            get
            {
                if (IsDisposed || Disposing)
                    throw new ObjectDisposedException(GetType().Name);
                if (literalStyle == null)
                    literalStyle = new TextStyle(new SolidBrush(Color.FromArgb(214, 157, 133)), null, FontStyle.Regular);
                return literalStyle;
            }
        }
        private TextStyle literalStyle = null;

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public EditorTextBox()
        {
            //setup
            ReservedCountOfLineNumberChars = 4;
            WordWrap = true;
            WordWrapAutoIndent = true;
            WordWrapMode = WordWrapMode.WordWrapControlWidth;
            TabLength = 4;
            LineInterval = 2;
            HotkeysMapping.Remove(Keys.Control | Keys.H); //remove default "replace" (CTRL + H, wtf?)
            HotkeysMapping[Keys.Control | Keys.R] = FCTBAction.ReplaceDialog; // CTRL + R for replace
            HotkeysMapping[Keys.Control | Keys.Y] = FCTBAction.Undo; // CTRL + Y for undo
            this.Language = Language.Custom;

            if (IsDesignMode)
                return;

            //cache previous text
            TextChanging += (s, e) =>
            {
                if (!e.Cancel)
                    previousText = Text;
            };

            //diff, highlighting
            TextChanged += (s, e) =>
            {
                if (Disposing || IsDisposed)
                    return;

                //generate diffs
                if (diffGeneration && DiffsGenerated == null)
                {
                    var currentText = Text;
                    var diffs = Diff.Calculate(previousText.ToCharArray(), currentText.ToCharArray());
                    DiffsGenerated?.Invoke(this, previousText, currentText, diffs);
                }

                //update syntax highlighting
                languages.TryLock(ApplySelectedLanguage);
            };

            //themes
            ApplyTheme(App.Theme);
            App.ThemeChanged += ApplyTheme;
        }

        /////////////////////////////////////////////////////////////////////
        // SYNTAX HIGLIGHTING
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A syntax highlighting language.
        /// </summary>
        private class Lang
        {
            public readonly string Name;
            public readonly EditorTextBox Editor;
            private readonly HashSet<string> extensions = new HashSet<string>();
            private readonly HashSet<string> types = new HashSet<string>();
            private readonly HashSet<string> keywords = new HashSet<string>();
            private Regex rxExtensions = null, rxTypes = null, rxKeywords = null;
            private Regex rxCommentLine = null, rxComment = null, rxStrings = null;
            private string commentLine = null, commentStart = null, commentEnd = null;

            private Regex RxExtensions
            {
                get
                {
                    if (rxExtensions == null && extensions.Count > 0)
                    {
                        rxExtensions = extensions.RegexSelector();
                        extensions.Clear();
                    }
                    return rxExtensions;
                }
            }

            private Regex RxComment
            {
                get
                {
                    if (rxComment == null && commentStart != null && commentEnd != null)
                    {
                        rxComment = new Regex(Regex.Escape(commentStart) + ".*?" + Regex.Escape(commentEnd),
                            RegexOptions.Singleline | RegexOptions.Compiled);
                        commentStart = commentEnd = null;
                    }
                    return rxComment;
                }
            }

            private Regex RxCommentLine
            {
                get
                {
                    if (rxCommentLine == null && commentLine != null)
                    {
                        rxCommentLine = new Regex(Regex.Escape(commentLine) + ".*$",RegexOptions.Compiled);
                        commentLine = null;
                    }
                    return rxCommentLine;
                }
            }

            private Regex RxTypes
            {
                get
                {
                    if (rxTypes == null && types.Count > 0)
                    {
                        rxTypes = types.RegexSelector(true, true, @"\b(", @")\b");
                        types.Clear();
                    }
                    return rxTypes;
                }
            }

            private Regex RxKeywords
            {
                get
                {
                    if (rxKeywords == null && keywords.Count > 0)
                    {
                        rxKeywords = keywords.RegexSelector(true, true, @"\b(", @")\b");
                        keywords.Clear();
                    }
                    return rxKeywords;
                }
            }

            private Regex RxStrings
            {
                get
                {
                    if (rxStrings == null)
                        rxStrings = new Regex(@"(?:""[^""\\]*(?:\\.[^""\\]*)*"")"
                            + @"|(?:'[^'\\]*(?:\\.[^'\\]*)*')", RegexOptions.Compiled);
                    return rxStrings;
                }
            }

            public Lang(EditorTextBox editor, XElement node)
            {
                //editor
                Editor = editor;
                
                //name
                Name = (string)node.Attribute("name");

                //file extensions
                var tokens = Marzersoft.Text.REGEX_WHITESPACE.Split(((string)node.Attribute("ext")));
                foreach (var token in tokens)
                    extensions.Add(token);

                //comments
                commentLine = (string)node.Attribute("commentLine");
                commentStart = (string)node.Attribute("commentStart");
                commentEnd = (string)node.Attribute("commentEnd");

                //<Keyword> tags
                var keywordTags = from k in node.Descendants("Keywords")
                                  where k.Attribute("name") != null
                                  orderby (string)k.Attribute("name")
                                  select k;

                //types
                var types = from k in keywordTags
                            where ((string)k.Attribute("name")).ToLower().IndexOf("type") > -1
                            select k;
                foreach (var t in types)
                {
                    tokens = Marzersoft.Text.REGEX_WHITESPACE.Split(t.Value);
                    foreach (var token in tokens)
                        this.types.Add(token);
                }

                //keywords/instructions
                var keywords = from k in keywordTags
                            where ((string)k.Attribute("name")).ToLower().IndexOf("instre") > -1
                            select k;
                foreach (var k in keywords)
                {
                    tokens = Marzersoft.Text.REGEX_WHITESPACE.Split(k.Value);
                    foreach (var token in tokens)
                        this.keywords.Add(token);
                }
            }

            public bool Matches(string ext)
            {
                return RxExtensions.IsMatch(ext);
            }

            private static Range[] Difference(Range a, Range b)
            {
                Range low = a.Start < b.Start ? a : b;
                Range high = low == a ? b : a;

                // -----
                //         ------
                if (high.Start >= low.End)
                    return new Range[] { new Range(a.tb, low.Start, high.End) };

                // --------------
                // --------------
                if (a.Start == b.Start && a.End == b.End)
                    return null;

                //must be some overlap... 
                Range shortest = a.Length < b.Length ? a : b;
                Range longest = shortest == a ? b : a;

                // -------
                // --------------
                if (shortest.Start == longest.Start)
                    return new Range[] { new Range(a.tb, shortest.End, longest.End) };
                
                //        -------
                // --------------
                if (shortest.End == longest.End)
                    return new Range[] { new Range(a.tb, longest.Start, shortest.Start) };

                //     -------
                // --------------
                if (shortest.Start > longest.Start && longest.End > shortest.End)
                    return new Range[]
                    {
                        new Range(a.tb, longest.Start, shortest.Start),
                        new Range(a.tb, shortest.End, longest.End)
                    };

                //     ----------
                // ----------
                return new Range[]
                {
                    new Range(a.tb, low.Start, high.Start),
                    new Range(a.tb, low.End, high.End)
                };
            }

            //*** assumes stencils are entirely contained by range ***
            private static Range[] Stencil(Range range, List<Range> stencils)
            {
                if (stencils.Count == 1)
                    return Difference(range, stencils[0]);

                List<Range> output = new List<Range>();

                //do start
                var diff = Difference(range, stencils[0]);
                if (diff != null && diff.Length == 2)
                    output.Add(diff[0]);

                //do middle
                for (int i = 1; i < stencils.Count; ++i)
                    output.Add(new Range(range.tb, stencils[i - 1].End, stencils[i].Start));

                //do end
                diff = Difference(range, stencils[stencils.Count-1]);
                if (diff != null && diff.Length == 2)
                    output.Add(diff[1]);

                return output.ToArray();
            }

            private static bool Merge(Range left, Range right)
            {
                if (left.End == right.Start)
                {
                    left.End = right.End;
                    return true;
                }
                return false;
            }

            private static void ApplyStyle(ref List<Range> haystack, Regex regex, Func<TextStyle> styleGetter, bool linewise)
            {
                if (regex == null)
                    return;

                //get ranges
                List<Range> nonMatches = new List<Range>();
                List<Range> matches = new List<Range>();
                foreach (var range in haystack)
                {
                    //get matches
                    var newMatches = (linewise ? range.GetRangesByLines(regex)
                        : range.GetRanges(regex)).ToList();
                    if (newMatches.Count == 0)
                    {
                        nonMatches.Add(range);
                        continue;
                    }

                    //merge adjacent ranges
                    int s = 0;
                    while (s < (newMatches.Count - 1))
                    {
                        if (Merge(newMatches[s], newMatches[s + 1]))
                            newMatches.RemoveAt(s + 1);
                        else
                            ++s;
                    }
                    
                    //add matches
                    matches.AddRange(newMatches);

                    //get inverse for non-matches
                    nonMatches.AddRange(Stencil(range, newMatches));
                }
                if (matches.Count == 0)
                    return;

                //set input search ranges list to leftover non-matches
                haystack = nonMatches;

                //apply style to matches
                var style = styleGetter();
                foreach (var match in matches)
                    match.SetStyle(style);
            }

            public void Highlight(Range range)
            {
                //normalize input range
                range.Normalize();
                List<Range> ranges = new List<Range>() { range };

                //style multiline comments
                ApplyStyle(ref ranges, RxComment, () => { return Editor.CommentStyle; }, false);

                //style single-line comments
                ApplyStyle(ref ranges, RxCommentLine, () => { return Editor.CommentStyle; }, true);

                //style string literals
                ApplyStyle(ref ranges, RxStrings, () => { return Editor.LiteralStyle; }, true);

                //style keywords
                ApplyStyle(ref ranges, RxKeywords, () => { return Editor.KeywordStyle; }, true);

                //style types
                ApplyStyle(ref ranges, RxTypes, () => { return Editor.TypeStyle; }, true);
            }
        }

        public void LoadLanguages()
        {
            if (Disposing || IsDisposed || languagesThread != null || languages.Count > 0)
                return;

            languagesThread = new Thread(() =>
            {
                try
                {
                    //download langs file if necessary
                    byte[] fileData = null;
                    if (!File.Exists(LanguagesPath)
                        || (DateTime.UtcNow - File.GetCreationTimeUtc(LanguagesPath)).TotalDays >= 14.0)
                        File.WriteAllBytes(LanguagesPath, fileData = LanguagesURL.DownloadData());

                    //read file
                    if (fileData == null)
                        fileData = File.ReadAllBytes(LanguagesPath);

                    //parse file
                    using (MemoryStream stream = new MemoryStream(fileData, false))
                    {
                        using (StreamReader reader = new StreamReader(stream, fileData.DetectEncoding()))
                        {
                            var xml = XDocument.Load(reader);

                            lock (languages)
                            {
                                //clear languages
                                languages.Clear();

                                //query for Language nodes
                                var langQuery = from l in xml.Descendants("Language")
                                                where l.Attribute("name") != null
                                                orderby (string)l.Attribute("name")
                                                select l;

                                //create Lang wrappers
                                foreach (var l in langQuery)
                                {
                                    var lang = new Lang(this, l);
                                    languages.Add(lang.Name, lang);
                                }

                                //update current language selection
                                UpdateSelectedLanguage();
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    if (!Disposing && !IsDisposed)
                        Logger.ErrorMessage(this, "Error initializing syntax highlighter:\n\n{0}: {1}",
                            exc.InnerException.GetType().Name, exc.InnerException.Message);
                }
                languagesThread = null;
            });
            languagesThread.IsBackground = false;
            languagesThread.Start();
        }

        private void UpdateSelectedLanguage()
        {
            currentLang = null;
            foreach (var kvp in languages)
            {
                if (kvp.Value.Matches(currentLanguage))
                {
                    currentLang = kvp.Value;
                    break;
                }
            }
            if (currentLang == null)
                currentLang = languages["normal"]; //txt
            ApplySelectedLanguage();
        }

        private void ApplySelectedLanguage()
        {
            if (currentLang == null)
                throw new NullReferenceException("languages[\"normal\"] is null!");
            this.Execute(() =>
            {
                ClearStyle(StyleIndex.All);
                currentLang.Highlight(Range);
            }, false);
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
                        if (commentStyle != null)
                        {
                            commentStyle.Dispose();
                            commentStyle = null;
                        }
                        if (literalStyle != null)
                        {
                            literalStyle.Dispose();
                            literalStyle = null;
                        }
                        if (typeStyle != null)
                        {
                            typeStyle.Dispose();
                            typeStyle = null;
                        }
                        if (keywordStyle != null)
                        {
                            keywordStyle.Dispose();
                            keywordStyle = null;
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
