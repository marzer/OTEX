using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Editor
{
    public static class SyntaxColours
    {
        /// <summary>
        /// Colour of comments.
        /// </summary>
        public static Color Comments
        {
            get { return Color.FromArgb(87, 166, 74); }
        }

        /// <summary>
        /// Colour of keywords.
        /// </summary>
        public static Color Keywords
        {
            get { return Color.FromArgb(86, 156, 214); }
        }

        /// <summary>
        /// Colour of types.
        /// </summary>
        public static Color Types
        {
            get { return Color.FromArgb(78, 201, 176); }
        }

        /// <summary>
        /// Colour of string literals.
        /// </summary>
        public static Color Strings
        {
            get { return Color.FromArgb(214, 157, 133); }
        }

        /// <summary>
        /// Colour of numeric literals.
        /// </summary>
        public static Color Numbers
        {
            get { return Color.FromArgb(181, 206, 168); }
        }

        /// <summary>
        /// Colour of preprocessor directives.
        /// </summary>
        public static Color PreprocessorDirectives
        {
            get { return Color.FromArgb(155, 155, 155); }
        }

        /// <summary>
        /// Colour of documentation flags (e.g. C#'s /// xml comments)
        /// </summary>
        public static Color Documentation
        {
            get { return Color.FromArgb(0, 100, 0); }
        }
    }
}
