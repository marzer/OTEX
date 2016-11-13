using Marzersoft;
using Marzersoft.Themes;
using System;

namespace OTEX
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
            //App.Mutex = true;
            App.AutoCheckForUpdates = false;
            App.TrayIcon = false;
            App.SplashForm = false;
            App.Theme = Theme.VisualStudioDark;
            App.Run(args);
        }
    }
}
