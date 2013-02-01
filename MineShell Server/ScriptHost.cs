using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptLibrary;
using System.IO;
using System.Reflection;
using AC0KG.Utils;

namespace AC0KG
{
    static class ScriptHost
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("Script Host");

        private static Assembly script;
        private static Func<string, List<string>> _ProcessServerLine;
        private static Action<List<string>> _SetOpsList;

        static ScriptHost()
        {
            LoadScript();
        }


        /// <summary>
        /// Attempt to load the script identifed by the value of the 'Script File' key in
        /// the application config file.
        /// </summary>
        public static void LoadScript()
        {
            try
            {
                var fileName = ConfigUtil.GetAppSetting("Script File", "");
                log.InfoFormat("Loading script file: {0}", fileName);
                var code = File.ReadAllText(fileName);
                log.InfoFormat("Compiling script, length: {0}", code.Length);
                script = CSScript.LoadCode(code);

                log.Info("Getting method ProcessServerLine");
                var p = script.GetStaticMethod("*.ProcessServerLine", "");
                _ProcessServerLine = (l) => { return (List<string>)p(l); };

                log.Info("Getting method SetOpsList");
                var setOpsList = script.GetStaticMethod("*.SetOpsList", new List<string>());
                _SetOpsList = (l) => { setOpsList(l); };
            }
            catch (Exception ex)
            {
                _ProcessServerLine = null;
                _SetOpsList = null;
                script = null;

                log.Error(ex);
            }

        }


        /// <summary>
        /// If the script was successfully compiled and loaded, set the value
        /// of the script object's ops list.
        /// </summary>
        /// <param name="ops">A list of usernames who are ops. The content of the ops.txt file.</param>
        public static void SetOpsList(List<string> ops)
        {
            try
            {
                if (_SetOpsList != null)
                    _SetOpsList(ops);
            }
            catch( Exception ex )
            {
                log.Error(ex);
            }
        }


        /// <summary>
        /// If the script was successfully compiled and loaded, send the input line
        /// to be processed. The output from the script will be returned. 
        /// </summary>
        /// <param name="input">The text to send to the script, a line output from the server.</param>
        /// <returns>A list of commands that are to be sent to the server</returns>
        public static List<string> ProcessServerLine(string input)
        {
            try
            {
                List<string> result = null;

                if (_ProcessServerLine != null)
                    result = _ProcessServerLine(input);

                return result ?? new List<string>();
            }
            catch (Exception ex )
            {
                log.Error(ex);
                return new List<string>();
            }
        }
    }
}
