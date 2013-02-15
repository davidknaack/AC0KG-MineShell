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
using System.Collections.Generic;
using CSScriptLibrary;
using System.IO;
using System.Reflection;
using AC0KG.Utils;

namespace AC0KG.Minecraft.MineShell
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
                log.DebugFormat("Compiling script, length: {0}", code.Length);
                script = CSScript.LoadCode(code);

                log.Debug("Getting method ProcessServerLine");
                var p = script.GetStaticMethod("*.ProcessServerLine", "");
                _ProcessServerLine = (l) => { return (List<string>)p(l); };

                log.Debug("Getting method SetOpsList");
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
