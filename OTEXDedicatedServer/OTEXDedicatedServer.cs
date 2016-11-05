using Marzersoft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OTEX
{
    class OTEXDedicatedServer
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Application name.
        /// </summary>
        private static readonly string Name = "OTEX Dedicated Server";

        /// <summary>
        /// Application description.
        /// </summary>
        private static readonly string Description = "Dedicated server for OTEX collaborative text editor framework.";

        /// <summary>
        /// Splash text.
        /// </summary>
        private static string Splash
        {
            get
            {
                return string.Format("{0} - {1}{2}Mark Gillard, 2016 | mark.gillard@flinders.edu.au | marzersoft.com",
                    Name, Description, Environment.NewLine);
            }
        }

        /// <summary>
        /// Usage text.
        /// </summary>
        private static string Usage
        {
            get
            {
                return string.Format("usage: {0} <file path> [/PORT port] [/?]", Process.GetCurrentProcess().ProcessName.ToUpper());
            }
        }

        /// <summary>
        /// Help text.
        /// </summary>
        private static string Help
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Splash).AppendLine().AppendLine(Usage).AppendLine();
                sb.AppendLine(" <file path>: Path to the plain text file to collaboratively edit.");
                sb.AppendLine("  /PORT port: Port on which to listen for new OTEX client connections, between 1024 and 65535.");
                sb.AppendLine("              Defaults to 55555.");
                sb.AppendLine("          /?: Prints help and exits.");
                sb.AppendLine();
                sb.AppendLine("Arguments may appear in any order and are not case sensitive .");
                return sb.ToString();
            }
        }

        static OTEXServer server = null;
        static volatile bool stop = false;


        /////////////////////////////////////////////////////////////////////
        // MAIN
        /////////////////////////////////////////////////////////////////////

        static int Main(string[] args)
        {
            //handle command line arguments
            string filePath = "";
            ushort port = 55555;
            try
            {
                var arguments = App.ProcessArguments(args);

                //help
                if (arguments.Find((a) => { return a.Flag && a.Value.CompareTo("?") == 0; }) != null)
                {
                    Console.Error.WriteLine(Help);
                    return 0;
                }

                //key value pairs
                for (int i = 0; i < arguments.Count - 1; ++i)
                {
                    if (!arguments[i].Flag || arguments[i + 1].Flag)
                        continue;

                    if (arguments[i].Value.CompareTo("port") == 0)
                    {
                        port = ushort.Parse(arguments[i + 1].Value);
                        arguments[i].Handled = arguments[i + 1].Handled = true;
                    }
                }

                //single, non-flag arguments
                for (int i = 0; i < arguments.Count; ++i)
                {
                    if (arguments[i].Flag || arguments[i].Handled)
                        continue;
                    filePath = arguments[i].Value;
                }
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(string.Format("{0}: {1}", exc.GetType().FullName, exc.Message));
                Console.Error.WriteLine(Usage);
                return 1;
            }

            //attach early exit events
            Console.CancelKeyPress += (s, e) => { stop = true; e.Cancel = true; };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { if (server != null) server.Dispose(); };

            try
            {
                //create server
                server = new OTEXServer(filePath, port);

                //start server
                server.Start();
                Console.Out.WriteLine(string.Format("{0} started.", Name));
                Console.Out.WriteLine(string.Format("  File: {0}", filePath));
                Console.Out.WriteLine(string.Format("  Port: {0}", port));
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(string.Format("{0}: {1}", exc.GetType().FullName, exc.Message));
                Console.Error.WriteLine(Usage);
                return 2;
            }

            //loop, monitoring for exceptions
            while (!stop)
            {
                Thread.Sleep(100);
                var threadExceptions = server.ThreadExceptions;
                if (threadExceptions.Count > 0)
                {
                    foreach (var threadException in threadExceptions)
                        Console.Error.WriteLine(string.Format("{0}: {1}", threadException.InnerException.GetType().FullName,
                            threadException.InnerException.Message));

                }
            }

            //dispose server (also stops it)
            server.Dispose();

            //exit
            return 0;
        }
    }
}
