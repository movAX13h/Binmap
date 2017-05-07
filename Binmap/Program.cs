using System;
using System.IO;

namespace Binmap
{
#if WINDOWS || LINUX
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            string initialFile = "";
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1])) initialFile = args[1];

            using (var app = new Main(initialFile))
            {
                app.Run();
            }
        }
    }
#endif
}
