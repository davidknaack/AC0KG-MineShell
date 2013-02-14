using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using AC0KG.Utils;
using AC0KG.Utils.Win32Svc;

namespace AC0KG.Minecraft.MineShell
{
    // http://code.google.com/p/minecraft-service/

    static class Program
    {
        private static readonly log4net.ILog log; // don't init before log is configured

        static Program()
        {
            ConfigUtil.ConfigureLogger("", ConfigUtil.GetAppSetting("Log Dir"), "", "AC0KG MineShellLog.config");
            log = log4net.LogManager.GetLogger("MineShell Main");
        }

        private static void Banner()
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            //Console.WriteLine(@"   __  __ _            ____  _          _ _ ");
            //Console.WriteLine(@"  |  \/  (_)_ __   ___/ ___|| |__   ___| | |");
            //Console.WriteLine(@"  | |\/| | | '_ \ / _ \___ \| '_ \ / _ \ | |");
            //Console.WriteLine(@"  | |  | | | | | |  __/___) | | | |  __/ | |");
            //Console.WriteLine(@"  |_|  |_|_|_| |_|\___|____/|_| |_|\___|_|_|");
            //Console.WriteLine();                                                                                           
            Console.WriteLine(@"      _/      _/  _/                        _/_/_/  _/                  _/  _/   ");
            Console.WriteLine(@"     _/_/  _/_/      _/_/_/      _/_/    _/        _/_/_/      _/_/    _/  _/    ");
            Console.WriteLine(@"    _/  _/  _/  _/  _/    _/  _/_/_/_/    _/_/    _/    _/  _/_/_/_/  _/  _/     ");
            Console.WriteLine(@"   _/      _/  _/  _/    _/  _/              _/  _/    _/  _/        _/  _/      ");
            Console.WriteLine(@"  _/      _/  _/  _/    _/    _/_/_/  _/_/_/    _/    _/    _/_/_/  _/  _/       ");
                                                                               
                                                                               
            Console.ForegroundColor = c;
                                           
        }

        private static void Usage()
        {
            Console.WriteLine("AC0KG MineShell Minecraft service shell");
            Console.WriteLine("  Usage:");
            Console.WriteLine("    -(c)onsole   : run as a console app on the desktop");
            Console.WriteLine("    -(i)nstall   : install service");
            Console.WriteLine("    -(u)ninstall : uninstall service");
        }
        
        /// <summary>
        /// Check the command line for install/uninstall command, or unrecognized commands.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>True if anything is recognized as a parameter</returns>
        static bool CheckInstallCmd(string[] args)
        {
            // ghetto command-line processing
            if ((args != null)
                && (args.Length == 1)
                && (args[0].Length > 1)
                && (args[0][0] == '-' || args[0][0] == '/'))
            {
                switch (args[0].Substring(1).ToLower())
                {
                    default:
                        Usage();
                        return true;

                    case "console":
                    case "c":
                        return false;

                    case "install":
                    case "i":
                        log.Info("Service Install");
                        ServiceSelfInstaller.Install();
                        return true;

                    case "uninstall":
                    case "u":
                        log.Info("Service Uninstall");
                        ServiceSelfInstaller.Uninstall();
                        return true;
                }
            }

            return false;
        }


        static void Main(string[] args)
        {
            if (CheckInstallCmd(args))
                return;

            try
            {
                // UserInteractive might be true even if started by the Service Control Manager
                // if a service has Interact with desktop permission.
                if (Environment.UserInteractive || (args.Length != 0))
                {
                    Console.SetWindowSize(Math.Min(120, Console.LargestWindowWidth), 30);

                    log.Info("Running as console service");
                    Banner();
                    Console.WriteLine();
                    Console.WriteLine("Press enter to quit\n");

                    RemoteShell.Start();

                    // wait around.
                    Console.ReadLine();

                    RemoteShell.Stop((a) => { Console.Write("."); });
                }
                else
                {
                    log.Info("Running as Windows service");
                    var services = new ServiceBase[] { new AC0KG.Minecraft.MineShell.MinecraftService() };
                    ServiceBase.Run(services);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }
        }
    }
}
