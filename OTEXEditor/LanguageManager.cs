﻿using Marzersoft;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OTEX
{
    /// <summary>
    /// Class for managing programming language definitions for parsers.
    /// </summary>
    public sealed class LanguageManager : ThreadController, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Event fired when this language manager has finished loading languages.
        /// parameters: sender, language count
        /// </summary>
        public event Action<LanguageManager, int> OnLoaded;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Has this LanguageManager been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return disposed; }
        }
        private volatile bool disposed = false;

        /// <summary>
        /// Path to local languages syntax file.
        /// </summary>
        private static string LanguagesPath
        {
            get { return Path.Combine(App.ExecutableDirectoryPath, "..\\languages.xml"); }
        }

        /// <summary>
        /// URL to download languages syntax file.
        /// </summary>
        private static readonly string LanguagesURL
            = "https://raw.githubusercontent.com/notepad-plus-plus/notepad-plus-plus/master/PowerEditor/src/langs.model.xml";

        /// <summary>
        /// Syntax highlighting load thead.
        /// </summary>
        private volatile Thread languagesThread = null;

        /// <summary>
        /// All loaded syntax highlighting languages.
        /// </summary>
        private Dictionary<string, Language> languages = null;
        private readonly object languagesLock = new object();

        /// <summary>
        /// Get the language represented by the given file name or extension.
        /// If the string contains any '.' characters the substring to the right of the last one
        /// is used as the key. Otherwise it is treated as an extension already.
        /// </summary>
        public Language this[string key]
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().Name);

                key = (key ?? "").Trim().ToLower();
                if (key.Length > 0)
                {
                    var lastPeriod = key.LastIndexOf('.');
                    if (lastPeriod != -1)
                    {
                        if (lastPeriod == key.Length - 1)
                            return null;
                        key = key.Substring(lastPeriod + 1);
                    }
                }
                if (key.Length == 0)
                    return null;

                Language output = null;
                languagesLock.TryLock(() =>
                {
                    foreach (var kvp in languages)
                    {
                        if (kvp.Value.MatchesExtension(key))
                        {
                            output = kvp.Value;
                            break;
                        }
                    }
                });
                return output;
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public LanguageManager()
        {
            //
        }

        /////////////////////////////////////////////////////////////////////
        // LOADING
        /////////////////////////////////////////////////////////////////////

        public void Load()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().Name);
            if (languagesThread != null)
                return;

            languagesThread = new Thread(() =>
            {
                Dictionary<string, Language> newLanguages = new Dictionary<string, Language>();
                CaptureException(() =>
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

                            //query for Language nodes
                            var langQuery = from l in xml.Descendants("Language")
                                            where l.Attribute("name") != null
                                            orderby (string)l.Attribute("name")
                                            select l;

                            //create Lang wrappers
                            foreach (var l in langQuery)
                            {
                                var lang = new Language(l);
                                newLanguages.Add(lang.Name, lang);
                            }
                        }
                    }
                });

                int count = 0;
                lock (languagesLock)
                {
                    if (languages != null)
                        languages.Clear();
                    languages = newLanguages;
                    count = languages.Count;
                }
                OnLoaded?.Invoke(this, count);
                languagesThread = null;
            });
            languagesThread.IsBackground = false;
            languagesThread.Start();
        }

        /////////////////////////////////////////////////////////////////////
        // LANGUAGE CLASS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A syntax highlighting language.
        /// </summary>
        public sealed class Language
        {
            /// <summary>
            /// The name of this programming language, as defined in the scintilla langs file.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The delimiter for a single-line comment in this language's syntax, if any.
            /// </summary>
            public readonly string CommentLine;

            /// <summary>
            /// The delimiter for the beginning of a multi-line comment in this language's syntax, if any.
            /// </summary>
            public readonly string CommentStart;

            /// <summary>
            /// The delimiter for the end of a multi-line comment in this language's syntax, if any.
            /// </summary>
            public readonly string CommentEnd;

            /// <summary>
            /// File extensions used by this language.
            /// </summary>
            public IReadOnlyList<string> Extensions
            {
                get { return extensions.AsReadOnly(); }
            }
            private readonly List<string> extensions = new List<string>();
            private Regex rxExtensions = null;

            /// <summary>
            /// Collections of keywords used in this language, grouped by subgroups.
            /// </summary>
            public IReadOnlyList<string> this[string key]
            {
                get
                {
                    key = (key ?? "").Trim().ToLower();
                    if (key.Length == 0)
                        return null;

                    List<string> list = null;
                    if (keywords.TryGetValue(key, out list))
                        return list.AsReadOnly();
                    return null;
                }
            }
            private readonly Dictionary<string, List<string>> keywords
                 = new Dictionary<string, List<string>>();

            /// <summary>
            /// Names of all keyword groups contained in this language (used as keys in [] accessor).
            /// </summary>
            public IReadOnlyList<string> KeywordGroups
            {
                get { return keywordGroups.AsReadOnly(); }
            }
            private List<string> keywordGroups = null;

            /// <summary>
            /// Creates a language from it's langs.model.xml node.
            /// </summary>
            internal Language(XElement node)
            {
                //name
                Name = ((string)node.Attribute("name")).Trim().ToLower();

                //file extensions
                var tokens = Text.REGEX_WHITESPACE.Split(((string)node.Attribute("ext")).Trim());
                if (tokens.Length > 0)
                    extensions.AddRange(tokens);

                //comments
                CommentLine = (string)node.Attribute("commentLine");
                CommentStart = (string)node.Attribute("commentStart");
                CommentEnd = (string)node.Attribute("commentEnd");

                //keywords
                var keywordNodes = from k in node.Descendants("Keywords")
                                   where k.Attribute("name") != null
                                   orderby (string)k.Attribute("name")
                                   select k;
                foreach (var k in keywordNodes)
                {
                    tokens = Text.REGEX_WHITESPACE.Split(k.Value.Trim());
                    if (tokens.Length == 0)
                        continue;

                    var key = ((string)k.Attribute("name")).Trim().ToLower();
                    List<string> list = null;
                    if (!keywords.TryGetValue(key, out list))
                        keywords[key] = list = new List<string>();
                    list.AddRange(tokens);
                }
                keywordGroups = keywords.Keys.ToList();
            }

            /// <summary>
            /// Checks if a given file extension matches this language.
            /// </summary>
            internal bool MatchesExtension(string ext)
            {
                if (extensions.Count == 0 || (ext = (ext ?? "")).Length == 0)
                    return false;
                if (rxExtensions == null)
                    rxExtensions = extensions.RegexSelector();
                return rxExtensions.IsMatch(ext);
            }
        }

        /////////////////////////////////////////////////////////////////////
        // DISPOSE
        /////////////////////////////////////////////////////////////////////

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                if (languagesThread != null)
                    languagesThread.Join();
                lock (languages)
                {
                    languages.Clear();
                }
                ClearEventListeners();
            }
        }

        protected override void ClearEventListeners()
        {
            base.ClearEventListeners();
            OnLoaded = null;
        }
    }
}
