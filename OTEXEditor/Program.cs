using Marzersoft;
using Marzersoft.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
                //create plugin factory
                PluginFactory pluginFactory = new PluginFactory(
                    new Type[] { typeof(IEditorTextBox) }, Paths.PluginsDirectory);
                
                //check critical types that must have at least 1 plugin
                if (pluginFactory.Count<IEditorTextBox>() == 0)
                    throw new FileNotFoundException("No plugins implementing the IEditorTextBox type were found");

                //pass to form constructor
                return new object[] { pluginFactory };
            };

            //run applicaton
            App.Run(args);
        }
    }
}
