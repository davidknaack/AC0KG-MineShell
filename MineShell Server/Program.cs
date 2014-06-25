/****************************************************************************
*    Copyright 2013 David Knaack
*    This file is part of AC0KG-MineShell
*
*    AC0KG-MineShell is free software: you can redistribute it and/or modify
*    it under the terms of the GNU General Public License as published by
*    the Free Software Foundation, either version 3 of the License, or
*    (at your option) any later version.
*
*    AC0KG-MineShell is distributed in the hope that it will be useful,
*    but WITHOUT ANY WARRANTY; without even the implied warranty of
*    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*    GNU General Public License for more details.
*
*    You should have received a copy of the GNU General Public License
*    along with AC0KG-MineShell.  If not, see <http://www.gnu.org/licenses/>.
****************************************************************************/
using System;
using System.ServiceProcess;
using AC0KG.Utils;
using System.ComponentModel;
using AC0KG.WindowsService;



namespace AC0KG.Minecraft.MineShell
{
    // This project started as a fork of minecraft-service, which can be found at:
    // http://code.google.com/p/minecraft-service/

    [System.ComponentModel.DesignerCategory("")]
    [ServiceName("MinecraftServer")]
    class Service : ServiceShell { }

    [RunInstaller(true)]
    [ServiceName("MinecraftServer", DisplayName = "Minecraft Server", Description = "Service shell for Minecraft server")]
    public class Installer : InstallerShell { }
    
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
        
        static void Main(string[] args)
        {
            if (ServiceShell.ProcessInstallOptions(args))
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
                }

                Service.StartService<Service>(
                    RemoteShell.Start,
                    () => RemoteShell.Stop((a) => { Console.Write("."); }),
                    Environment.UserInteractive);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
            }
        }
    }
}
