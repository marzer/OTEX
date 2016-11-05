using Marzersoft;
using Marzersoft.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            App.MainFormType = typeof(OTEXEditorForm);
            App.Name = "OTEX Editor";
            App.Description = "Editor client for OTEX collaborative text editor framework.";
            //App.Mutex = true;
            App.AutoCheckForUpdates = false;
            App.TrayIcon = false;
            App.SplashForm = false;
            App.Theme = Theme.VisualStudioDark;
            App.Run(args);
        }
    }
}
