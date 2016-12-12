using Marzersoft;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OTEX.Editor
{
    /// <summary>
    /// Class for managing icons for supported file types (based on seti-ui).
    /// </summary>
    public sealed class IconManager : ThreadController, IDisposable
    {
        /////////////////////////////////////////////////////////////////////
        // EVENTS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Event fired when this icon manager has finished loading icons.
        /// parameters: sender, icon count, extension count
        /// </summary>
        public event Action<IconManager, int, int> OnLoaded;

        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        private static readonly RegexCache IconSetRegex
            = new RegexCache(@"^\s*\.icon-set\(\s*'\.([a-z0-9_\-]+?)'\s*,\s*'(.+?)'\s*,\s*@(.+?)\);\s*$",
                RegexOptions.IgnoreCase);

        /// <summary>
        /// Has this IconManager been disposed?
        /// </summary>
        public bool IsDisposed
        {
            get { return disposed; }
        }
        private volatile bool disposed = false;

        /// <summary>
        /// Load thead.
        /// </summary>
        private volatile Thread thread = null;

        /// <summary>
        /// All loaded images.
        /// </summary>
        private readonly Dictionary<string, IconData> icons
            = new Dictionary<string, IconData>();
        private readonly object iconsLock = new object();

        /// <summary>
        /// Get the language represented by the given file name or extension.
        /// If the string contains any '.' characters the substring to the right of the last one
        /// is used as the key. Otherwise it is treated as an extension already.
        /// </summary>
        public Image this[string key]
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

                Image output = null;
                iconsLock.TryLock(() =>
                {
                    foreach (var kvp in icons)
                    {
                        output = kvp.Value.Image(key);
                        if (output != null)
                            break;
                    }
                });
                return output;
            }
        }

        /////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR
        /////////////////////////////////////////////////////////////////////

        public IconManager()
        {
            //
        }

        /////////////////////////////////////////////////////////////////////
        // LOADING
        /////////////////////////////////////////////////////////////////////

        public void Load(string path)
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().Name);
            if (thread != null)
                return;

            thread = new Thread(() =>
            {
                Dictionary<string, IconData> newIcons = new Dictionary<string, IconData>();
                if (!CaptureException(() =>
                {
                    //read mappings
                    var dir = Path.GetDirectoryName(path);
                    var mappings = File.ReadAllText(path, path.DetectEncoding())
                        .StripComments("//", "/*", "*/")
                        .SplitLines();

                    foreach (var line in mappings)
                    {
                        //parse mapping
                        var m = IconSetRegex.Match(line);
                        if (!m.Success || m.Groups[3].Equals("ignore"))
                            continue;

                        //get icon
                        if (!newIcons.TryGetValue(m.Groups[2].Value, out var icon))
                            newIcons[m.Groups[2].Value] = icon = new IconData(Path.Combine(dir, m.Groups[2].Value + ".svg"));

                        //add format
                        icon.Add(m.Groups[1].Value, m.Groups[3].Value);
                    }
                }))
                {
                    int count = 0, extensions = 0;
                    lock (iconsLock)
                    {
                        foreach (var kvp in icons)
                            kvp.Value.Dispose();
                        icons.Clear();

                        foreach (var kvp in newIcons)
                        {
                            icons[kvp.Key] = kvp.Value;
                            extensions += kvp.Value.ExtensionCount;
                        }
                        count = icons.Count;
                    }

                    OnLoaded?.Invoke(this, count, extensions);
                }
                thread = null;
            });
            thread.IsBackground = false;
            thread.Start();
        }

        /////////////////////////////////////////////////////////////////////
        // ICON DATA CLASS
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A file type icon.
        /// </summary>
        private sealed class IconData : IDisposable
        {
            private readonly HashSet<string> extensions = new HashSet<string>();
            private readonly Dictionary<string, Color> colours = new Dictionary<string, Color>();
            private readonly Dictionary<Color, Image> images = new Dictionary<Color, Image>();
            private readonly Lazy<RegexCache> rxExtensions;
            private readonly SvgDocument svg;
            private readonly float ratio;

            public int ExtensionCount { get { return extensions.Count; } }

            public IconData(string svgFile)
            {
                svg = SvgDocument.Open(svgFile);

                RectangleF r = svg.Bounds;
                svg.ViewBox = new SvgViewBox(r.X, r.Y, r.Width, r.Height);
                var sz = svg.GetDimensions();
                SizeF size = new SizeF(16.0f * (sz.Width / r.Width), 16.0f * (sz.Height / r.Height));
                float ratio = sz.Height / sz.Height;
                svg.Width = new SvgUnit(SvgUnitType.Pixel, size.Width * (sz.Height > sz.Width ? ratio : 1.0f));
                svg.Height = new SvgUnit(SvgUnitType.Pixel, size.Height * (sz.Width > sz.Height ? ratio : 1.0f));

                //foreach (var c in svg.Children)
                  //  c.Fill = new SvgColourServer(Color.White);

                rxExtensions = new Lazy<RegexCache>(() => { return extensions.RegexSelector(); }, true);
            }

            public void Add(string ext, string colour)
            {
                extensions.Add(ext = ext.Trim().ToLower());
                switch (colour)
                {
                    case "blue": colours[ext] = ColorTranslator.FromHtml("#519aba"); break;
                    case "grey": colours[ext] = ColorTranslator.FromHtml("#4d5a5e"); break;
                    case "green": colours[ext] = ColorTranslator.FromHtml("#8dc14"); break;
                    case "orange": colours[ext] = ColorTranslator.FromHtml("#e37933"); break;
                    case "pink": colours[ext] = ColorTranslator.FromHtml("#f55385"); break;
                    case "purple": colours[ext] = ColorTranslator.FromHtml("#a074c4"); break;
                    case "red": colours[ext] = ColorTranslator.FromHtml("#cc3e44"); break;
                    case "yellow": colours[ext] = ColorTranslator.FromHtml("#cbcb41"); break;
                    default:
                        /*case "white": */ colours[ext] = ColorTranslator.FromHtml("#d4d7d6"); break;
                }
            }

            public Image Image(string ext)
            {
                if (extensions.Count == 0 || (ext = (ext ?? "").Trim().ToLower()).Length == 0)
                    return null;
                if (!rxExtensions.Value.IsMatch(ext))
                    return null;
                var col = colours[ext];
                if (!images.TryGetValue(col, out var img))
                {
                    foreach (var c in svg.Children)
                        c.Fill = new SvgColourServer(col);
                    images[col] = img = svg.Draw();
                }
                return img;
            }

            public void Dispose()
            {
                foreach (var kvp in images)
                    kvp.Value.Dispose();
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
                if (thread != null)
                    thread.Join();
                lock (iconsLock)
                {
                    icons.Clear();
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
