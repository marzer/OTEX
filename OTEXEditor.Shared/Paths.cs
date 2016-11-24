using Marzersoft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTEX.Editor
{
    public static class Paths
    {
        /// <summary>
        /// Path to plugins directory.
        /// </summary>
        public static string PluginsDirectory
        {
            get { return Path.Combine(App.ExecutableDirectoryPath, "Plugins"); }
        }

        /// <summary>
        /// Path to local languages syntax file.
        /// </summary>
        public static string LanguagesFile
        {
            get { return Path.Combine(App.ExecutableDirectoryPath, "..\\languages.xml"); }
        }
    }
}
