using Marzersoft;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace OTEX.Editor
{
    class DedicatedServer
    {
        /////////////////////////////////////////////////////////////////////
        // PROPERTIES/VARIABLES
        /////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Application name.
        /// </summary>
        private static readonly string Name = "OTEX Editor Server";

        /// <summary>
        /// Application description.
        /// </summary>
        private static readonly string Description = "Dedicated server for the OTEX collaborative text editor.";

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
                return string.Format("  {0} [/EDIT file] [/NEW file] [/TEMP name] [/PORT port] [/NAME name] {1}"
                                   + "             [/PASSWORD pass] [/PUBLIC] [/MAXCLIENTS max] [/BANLIST file] [/ID guid] [/?]",
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
                sb.AppendLine("      /EDIT path: Adds a document to the session in edit mode. If the file already");
                sb.AppendLine("                  exists, it will be read in as-is.");
                sb.AppendLine("                  Can appear more than once.");
                sb.AppendLine("       /NEW path: Adds a document to the session in replace mode; If the file already");
                sb.AppendLine("                  exists, it will be overwritten with a blank file.");
                sb.AppendLine("                  Can appear more than once.");
                sb.AppendLine("      /TEMP name: Adds a document to the session in temporary mode, meaning it will");
                sb.AppendLine("                  not be backed by any file on disk and will be lost when the server");
                sb.AppendLine("                  is terminated. Name is limited to 32 characters.");
                sb.AppendLine("                  Can appear more than once.");
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
                sb.AppendLine("       /BAN guid: Specify an ID for a banned client.");
                sb.AppendLine("                  Format: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX (where X == any");
                sb.AppendLine("                  Hexadecimal character).");
                sb.AppendLine("                  Can appear more than once.");
                sb.AppendLine("        /ID guid: Specify an ID for the server.");
                sb.AppendLine("                  Format: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX (where X == any");
                sb.AppendLine("                  Hexadecimal character). Omit to generate the ID randomly.");
                sb.AppendLine("              /?: Prints help and exits.");
                sb.AppendLine();
                sb.AppendLine("Arguments may appear in any order. /SWITCHES and file paths are not case-sensitive.");
                return sb.ToString();
            }
        }

        static Server server = null;
        static volatile bool stop = false;
        static volatile object consoleLock = new object();
        static bool requiresPassword = false;

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
            try
            {
                //create session
                Session session = new Session();

                //parse command line
                Arguments args = new Arguments(argsArr);
                if (args.Key("?"))
                {
                    Print(Console.Out, ConsoleColor.Cyan, Splash);
                    Console.Out.WriteLine();
                    Out(Help);
                    return 0;
                }
                ushort port = 0;
                if (args.Value("port", ref port))
                    session.Port = port;
                string str = null;
                if (args.Value("name", ref str))
                    session.Name = str;
                uint max = 0;
                if (args.Value("maxclients", ref max))
                    session.ClientLimit = max;
                if (args.Value("password", ref str))
                    session.Password = new Password(str);
                requiresPassword = session.Password != null;
                session.Public = args.Key("public");
                if (args.Value("banlist", ref str))
                    session.BanListPath = str;
                Guid instanceID = Guid.NewGuid();
                if (args.Value("id", ref str))
                    str.TryParse(out instanceID);

                string[] strs = null;
                if (args.Values("edit", ref strs))
                {
                    foreach (var f in strs)
                        session.AddDocument(f, Document.ConflictResolutionStrategy.Edit, 4);
                }
                if (args.Values("new", ref strs))
                {
                    foreach (var f in strs)
                        session.AddDocument(f, Document.ConflictResolutionStrategy.Replace, 4);
                }
                if (args.Values("temp", ref strs))
                {
                    foreach (var f in strs)
                        session.AddDocument(f);
                }
                if (args.Values("ban", ref strs))
                {
                    foreach (var b in strs)
                        if (str.TryParse(out var banId))
                            session.AddBan(banId);
                }

                //create server
                server = new Server(Shared.AppKey, instanceID);

                //attach server events
                server.OnThreadException += (s, e) =>
                {
                    Error("{0}: {1}", e.InnerException.GetType().FullName, e.InnerException.Message);
                };
                server.OnStarted += (s) =>
                {
                    Out("OTEX Server started.");
                    Out("         Name: {0}", s.Session.Name);
                    Out("         Port: {0}", s.Session.Port);
                    Out("     Password: {0}", requiresPassword);
                    Out("       Public: {0}", s.Session.Public);
                    Out("  Max clients: {0}", s.Session.ClientLimit);
                    foreach (var d in session.Documents)
                        Out("     Document: {0}", d.Value.Path);
                };
                server.OnClientConnected += (s,rc) =>
                {
                    Out("Client {0} connected.", rc.ID);
                };
                server.OnClientDisconnected += (s, rc) =>
                {
                    Out("Client {0} disconnected.", rc.ID);
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
                server.Start(session);
            }
            catch (Exception exc)
            {
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Error("{0}: {1}", exc.GetType().FullName, exc.Message);
                    throw;
                }
#endif
                Error("Error: {0}", exc.Message);
                Warning("usage:{0}{1}", Environment.NewLine, Usage);
                return 1;
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
