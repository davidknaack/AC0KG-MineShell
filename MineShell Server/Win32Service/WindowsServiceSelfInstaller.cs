using System;
using System.Text;
using System.Reflection;
using System.Configuration.Install;

namespace AC0KG.Utils.Win32Svc
{
   
    public static class ServiceSelfInstaller
    {
        private static readonly string exePath = Assembly.GetEntryAssembly().Location;

        public static bool Install()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/LogToConsole=true", exePath });
            }
            catch (Exception e)
            {
                // Service install is normally done from the command line, so it isn't too out-there to
                // do console-based UI output here. However, this probably should catch everything and
                // output it in a new exception rather than assuming that there will be a console, and
                // that it is ok to write errors to the console;
                Console.Error.WriteLine(e);
                return false;
            }
            return true;
        }

        public static bool Uninstall()
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/LogToConsole=false", "/u", exePath });
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return false;
            }
            return true;
        }
    }
}

