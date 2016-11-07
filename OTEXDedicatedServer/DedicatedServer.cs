using Marzersoft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OTEX
{
    class DedicatedServer
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
                return string.Format("usage: {0} <file path> [/PORT port] [/PASSWORD pass] [/?]", Process.GetCurrentProcess().ProcessName.ToUpper());
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
                sb.AppendLine("   <file path>: Path to the plain text file to collaboratively edit.");
                sb.AppendLine("    /PORT port: Port on which to listen for new OTEX client connections, between 1024 and 65535.");
                sb.AppendLine("                Defaults to 55555.");
                sb.AppendLine("/PASSWORD pass: Password required to connect to this server.");
                sb.AppendLine("            /?: Prints help and exits.");
                sb.AppendLine();
                sb.AppendLine("Arguments may appear in any order.");
                return sb.ToString();
            }
        }

        static Server server = null;
        static volatile bool stop = false;
        static volatile object consoleLock = new object();

        /////////////////////////////////////////////////////////////////////
        // CONSOLE HELPER FUNCTIONS
        /////////////////////////////////////////////////////////////////////

        private static void Write(TextWriter writer, ConsoleColor? colour, string text, params object[] args)
        {
            if (args != null && args.Length > 0)
                text = string.Format(text, args);

            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Log(0, "", text + "\n");
            lock (consoleLock)
            {
                if (colour.HasValue)
                    Console.ForegroundColor = colour.Value;
                else
                    Console.ResetColor();
                writer.WriteLine(text);
            }
        }

        private static void Out(string text, params object[] args)
        {
            Write(Console.Out, null, text, args);
        }

        private static void Warning(string text, params object[] args)
        {
            Write(Console.Error, ConsoleColor.Yellow, text, args);
        }

        private static void Error(string text, params object[] args)
        {
            Write(Console.Error, ConsoleColor.Red, text, args);
        }

        /////////////////////////////////////////////////////////////////////
        // MAIN
        /////////////////////////////////////////////////////////////////////

        static int Main(string[] args)
        {
            //handle command line arguments
            string filePath = "";
            ushort port = 55555;
            Password password = null;
            try
            {
                var arguments = App.ProcessArguments(args);

                //help
                if (arguments.Find((a) => { return a.Flag && a.Value.CompareTo("?") == 0; }) != null)
                {
                    Out(Help);
                    return 0;
                }

                //key value pairs
                for (int i = 0; i < arguments.Count - 1; ++i)
                {
                    if (!arguments[i].Flag || arguments[i + 1].Flag)
                        continue;

                    bool match = false;
                    if (match = (arguments[i].Value.CompareTo("port") == 0))
                        port = ushort.Parse(arguments[i + 1].Value);
                    else if (match = (arguments[i].Value.CompareTo("password") == 0))
                        password = new Password(arguments[i + 1].Value);

                    if (match)
                        arguments[i].Handled = arguments[i + 1].Handled = true;
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
                Error("{0}: {1}", exc.GetType().FullName, exc.Message);
                Out(Usage);
                return 1;
            }


            //create server
            server = new Server();

            //attach server events
            server.OnInternalException += (s, e) =>
            {
                Error("{0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
            };
            server.OnStarted += (s) =>
            {
                Out("Server started.");
                Out("  File: {0}", s.FilePath);
                Out("  Port: {0}", s.Port);
            };
            server.OnClientConnected += (s,id) =>
            {
                Out("Client {0} connected.", id);
            };
            server.OnClientDisconnected += (s, id) =>
            {
                Out("Client {0} disconnected.", id);
            };
            server.OnStopped += (s) =>
            {
                Out("Server stopped.");
            };
            server.OnFileSynchronized += (s) =>
            {
                Out("File synchronized.");
            };
            Console.CancelKeyPress += (s, e) =>
            {
                stop = true;
                e.Cancel = true;
            };

            //start server
            try
            {
                server.Start(filePath, port, password);
            }
            catch (Exception exc)
            {
                Error("{0}: {1}", exc.GetType().FullName, exc.Message);
                Out(Usage);
                return 2;
            }

            //loop, waiting for stop
            while (!stop)
                Thread.Sleep(250);

            //dispose server (also stops it)
            server.Dispose();

            //exit
            return 0;
        }
    }
}
