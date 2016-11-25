using Marzersoft;
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
            get
            {
                return App.Theme != null && !App.Theme.IsDark
                    ? Color.FromArgb(0, 128, 0) : Color.FromArgb(87, 166, 74);
            }
        }

        /// <summary>
        /// Colour of keywords.
        /// </summary>
        public static Color Keywords
        {
            get
            {
                return App.Theme != null && !App.Theme.IsDark
                    ? Color.FromArgb(0, 0, 255) : Color.FromArgb(86, 156, 214);
            }
        }

        /// <summary>
        /// Colour of types.
        /// </summary>
        public static Color Types
        {
            get
            {
                return App.Theme != null && !App.Theme.IsDark
                    ? Color.FromArgb(43, 145, 175) : Color.FromArgb(78, 201, 176);
            }
        }

        /// <summary>
        /// Colour of string literals.
        /// </summary>
        public static Color Strings
        {
            get
            {
                return App.Theme != null && !App.Theme.IsDark
                    ? Color.FromArgb(163, 21, 21) : Color.FromArgb(214, 157, 133);
            }
        }

        /// <summary>
        /// Colour of numeric literals.
        /// </summary>
        public static Color Numbers
        {
            get
            {
                return App.Theme != null && !App.Theme.IsDark
                    ? Color.FromArgb(0, 0, 0) : Color.FromArgb(181, 206, 168);
            }
        }

        /// <summary>
        /// Colour of preprocessor directives.
        /// </summary>
        public static Color PreprocessorDirectives
        {
            get
            {
                return App.Theme != null && !App.Theme.IsDark
                    ? Color.FromArgb(128,128,128) : Color.FromArgb(155, 155, 155);
            }
        }

        /// <summary>
        /// Colour of documentation flags (e.g. C#'s /// xml comments)
        /// </summary>
        public static Color Documentation
        {
            get
            {
                return PreprocessorDirectives.Darken(0.4f).Blend(Comments, 100);
            }
        }
    }
}
