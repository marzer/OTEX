using Marzersoft;
using Marzersoft.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OTEX.Editor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //run the 64-bit version if necessary
            if (Environment.Is64BitOperatingSystem
                && !Environment.Is64BitProcess
                && !Debugger.IsAttached)
            {
                try
                {
#if DEBUG
                    string dist = "Debug";
#else
                string dist = "Release";
#endif
                    string exe = Path.GetFileName(App.ExecutablePath);
                    var x64Path = Path.Combine(App.ExecutableDirectory,
                        Shared.InSolution ? "..\\..\\x64\\" + dist + "\\" + exe : "..\\x64\\" + exe);
                    if (File.Exists(x64Path))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo(x64Path);

                        if (args.Length > 0)
                        {
                            var ar = (string[])args.Clone();
                            for (int i = 0; i < ar.Length; ++i)
                            {
                                ar[i] = Regex.Replace(ar[i], @"(\\*)" + "\"", @"$1$1\" + "\"");
                                ar[i] = "\"" + Regex.Replace(ar[i], @"(\\+)$", @"$1$1") + "\"";
                            }
                            psi.Arguments = string.Join(" ", ar);
                        }

                        if (Path.GetFullPath(Environment.CurrentDirectory).ToLower()
                            .Equals(Path.GetFullPath(App.ExecutableDirectory).ToLower()))
                            psi.WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(x64Path));
                        else
                            psi.WorkingDirectory = Environment.CurrentDirectory;

                        Process.Start(psi);
                        return;
                    }
                }
                catch (Exception) { }
            }

            App.MainFormType = typeof(EditorForm);
            App.Name = "OTEX Editor";
            App.Description = "Editor client for OTEX collaborative text editor framework.";
            App.Website = "https://github.com/marzer/OTEX/";
            App.AutoCheckForUpdates = false;
            App.TrayIcon = false;
            App.SplashForm = false;

            //enumerate plugin assemblies
            App.Initialization = () =>
            {
                //create plugin directory if necessary
                var dir = Path.Combine(App.ExecutableDirectory, "Plugins");
                Directory.CreateDirectory(dir);
                
                //create plugin factory
                PluginFactory pluginFactory = new PluginFactory(
                    new Type[] { typeof(IEditorTextBox) }, dir, true);
                
                //pass to form constructor
                return new object[] { pluginFactory };
            };

            //run applicaton
            App.Run(args);
        }
    }
}
