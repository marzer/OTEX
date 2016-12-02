using Marzersoft;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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
                StringBuilder sb = new StringBuilder();
                return string.Format("  {0} [file] [/EDIT|/NEW] [/PORT port] [/NAME name] [/PASSWORD pass] [/PUBLIC]{1}"
                                   + "             [/MAXCLIENTS max] [/BANLIST file] [/?]",
                    Process.GetCurrentProcess().ProcessName.ToUpper(), Environment.NewLine);
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
                sb.AppendLine(Usage).AppendLine();
                sb.AppendLine("            file: Path to the plain text file to collaboratively edit.");
                sb.AppendLine("                  Omitting file path will create a temporary session");
                sb.AppendLine("                  (client edits will be lost when the server is shut down).");
                sb.AppendLine("           /EDIT: If a file already exists at the given path, edit it");
                sb.AppendLine("                  (do not overwrite with a new file). This is default.");
                sb.AppendLine("            /NEW: Opposite of /EDIT.");
                sb.AppendLine("      /PORT port: Port on which to listen for new OTEX TCP client connections.");
  sb.AppendLine(string.Format("                  Can be any value between 1024-{0} or {1}-65535.",
      Server.AnnouncePorts.First - 1, Server.AnnouncePorts.Last + 1));
  sb.AppendLine(string.Format("                  Default is {0}.", Server.DefaultPort));
                sb.AppendLine("      /NAME name: A friendly name for the server.");
                sb.AppendLine("                  Limit of 32 characters (overflow is truncated).");
                sb.AppendLine("  /PASSWORD pass: Password required to connect to this server.");
                sb.AppendLine("                  Must be between 6 and 32 characters.");
                sb.AppendLine("                  Omit to allow clients to connect without a password.");
                sb.AppendLine("         /PUBLIC: Regularly broadcast the presence of this server to OTEX clients.");
                sb.AppendLine(" /MAXCLIENTS max: Maximum number of clients to allow. Defaults to 10, caps at 100.");
                sb.AppendLine("   /BANLIST file: Text file containing a newline-delimited list of banned client IDs.");
                sb.AppendLine("              /?: Prints help and exits.");
                sb.AppendLine();
                sb.AppendLine("Arguments may appear in any order. /SWITCHES and file paths are not case-sensitive.");
                return sb.ToString();
            }
        }

        static Server server = null;
        static volatile bool stop = false;
        static volatile object consoleLock = new object();

        /////////////////////////////////////////////////////////////////////
        // CONSOLE HELPER FUNCTIONS
        /////////////////////////////////////////////////////////////////////

        private static void Print(TextWriter writer, ConsoleColor? colour, string text, params object[] args)
        {
            if (args != null && args.Length > 0)
                text = string.Format(text, args);

            if (Debugger.IsAttached)
                Debugger.Log(0, "", text + "\n");
            lock (consoleLock)
            {
                if (colour.HasValue)
                    Console.ForegroundColor = colour.Value;
                writer.WriteLine(text);
                if (colour.HasValue)
                    Console.ResetColor();
            }
        }

        private static void Out(string text, params object[] args)
        {
            Print(Console.Out, null, text, args);
        }

        private static void Warning(string text, params object[] args)
        {
            Print(Console.Error, ConsoleColor.Yellow, text, args);
        }

        private static void Error(string text, params object[] args)
        {
            Print(Console.Error, ConsoleColor.Red, text, args);
        }

        /////////////////////////////////////////////////////////////////////
        // MAIN
        /////////////////////////////////////////////////////////////////////

        static int Main(string[] argsArr)
        {
            //handle command line arguments
            var startParams = new Server.StartParams()
            {
                ReplaceTabsWithSpaces = 4
            };

            //parse command line
            Arguments args = new Arguments(argsArr);
            if (args.Key("?"))
            {
                Print(Console.Out, ConsoleColor.Cyan, Splash);
                Console.Out.WriteLine();
                Out(Help);
                return 0;
            }
            args.Key("port", ref startParams.Port);
            args.Key("name", ref startParams.Name);
            args.Key("maxclients", ref startParams.MaxClients);
            string pw = null;
            if (args.Key("password", ref pw))
                startParams.Password = new Password(pw);
            args.Boolean("edit", "new", ref startParams.EditMode);
            startParams.Public = args.Key("public");
            args.Key("name", ref startParams.Name);
            args.Key("banlist", ref startParams.BanListPath);
            var path = args.OrphanedValues.LastOrDefault();
            if (path != null)
                startParams.FilePath = path.Value;

            //create server
            server = new Server();

            //attach server events
            server.OnThreadException += (s, e) =>
            {
                Error("{0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
            };
            server.OnStarted += (s) =>
            {
                Out("OTEX Server started.");
                Out("         Name: {0}", s.Name);
                Out("         File: {0}", s.FilePath);
                Out("         Port: {0}", s.Port);
                Out("     Password: {0}", s.RequiresPassword);
                Out("       Public: {0}", s.Public);
                Out(" Line endings: {0}", s.FileLineEndings.Equals("\r\n") ? "CRLF" : (s.FileLineEndings.Equals("\r") ? "CR" : "LF"));
                Out("  Max clients: {0}", s.MaxClients);
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
            Console.CancelKeyPress += (s, e) =>
            {
                stop = true;
                e.Cancel = true;
            };

            //start server
            try
            {
                server.Start(startParams);
            }
            catch (Exception exc)
            {
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Error("{0}: {1}", exc.GetType().FullName, exc.Message);
                    throw;
                }
#endif
                Error("Error: {0}", exc.Message);
                Warning("usage:{0}{1}", Environment.NewLine, Usage);
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
