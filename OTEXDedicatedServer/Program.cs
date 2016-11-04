using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OTEX
{
    class Program
    {
        static Server server = null;
        static volatile bool stop = false;

        static int Main(string[] args)
        {
            Console.CancelKeyPress += (s,e) => { stop = true; e.Cancel = true; };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => { if (server != null) server.Dispose(); };

            server = new Server(args[0]);
            server.Start();
            while (!stop)
                Thread.Sleep(100);
            server.Dispose();
            return 0;
        }
    }
}
