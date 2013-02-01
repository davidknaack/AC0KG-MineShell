using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Reflection;
using log4net.Appender;

namespace AC0KG.Utils
{
    public static class ConfigUtil
    {
        /// <summary>
        /// Load a config setting and return the value if it exists, or return the specified default value if it does not.
        /// </summary>
        /// <param name="Key">Name of AppSettings key</param>
        /// <param name="defVal">Default value</param>
        /// <returns></returns>
        public static string GetAppSetting(string Key, string defVal = "")
        {
            try
            {
                var ovr = ConfigurationManager.AppSettings[Key];
                return ovr ?? defVal;
            }
            catch
            {
                return defVal;
            }
        }

        /// <summary>
        /// Configure log4net logging system, must be called before logging will work.
        /// </summary>
        /// <param name="exe">Name of exe doing the logging, without file extension. 
        /// Used to name the log file. Navigate to the function for example code to get this.</param>
        public static void ConfigureLogger(string exe = "", string logDir = "", string configDir = "", string configFileName = "log4net.config")
        {
            // Here's a couple of typical ways to call this
            //  string exe = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
            //  string exe = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            //  LogConfig.ConfigureLogger(exe);                

            if (string.IsNullOrEmpty(exe))
                exe = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
            if (string.IsNullOrEmpty(logDir))
                logDir = Path.GetDirectoryName(exe);
            if (string.IsNullOrEmpty(configDir))
                configDir = Path.GetDirectoryName(exe);

            log4net.Config.XmlConfigurator.Configure(new FileInfo(Path.Combine(configDir, configFileName)));

            var log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            foreach (var a in log.Logger.Repository.GetAppenders().Where(a => a is FileAppender).Select(a => (FileAppender)a))
            {
                // Point file appenders to the log file directory and set the name to the current EXE name
                // Todo: seems like there should be a good way to make this automatic with the config file.
                a.File = Path.Combine(logDir, string.Format("{0}.txt", exe));
                a.ActivateOptions();
            }
        }

    }
}
