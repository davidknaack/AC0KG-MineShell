using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AC0KG.Utils;
using System.Reflection;

namespace AC0KG.Minecraft.MineShell
{
    public sealed class MinecraftHost
    {
        // simple singleton
        private MinecraftHost(){ }
        public static readonly MinecraftHost instance = new MinecraftHost();

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("Minecraft Host");
        private static readonly log4net.ILog logcon = log4net.LogManager.GetLogger("con");
        
        private Task workTask;
        private Process server;
        private RingBuffer<string> consoleHist = new RingBuffer<string>(100);
        public IEnumerable<string> ConsoleHistory
        {
            get { return consoleHist; }
            private set { ; }
        }

        /// <summary>
        /// Called when a new line has been received from the minecraft server
        /// </summary>
        public event EventHandler<EventArgs<string>> NewLine;

        private void OnNewLine(string line)
        {
            var nle = NewLine;
            if (nle != null)
                nle(this, new EventArgs<string>(line));
        }
        
        /// <summary>
        /// Start the Minecraft server and set up the task that handles catching the output
        /// </summary>
        internal void Start()
        {
            // The current directory needs to be set to the location of the executable.
            // When running as a service the current directory will likely be elsewhere.
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            Environment.CurrentDirectory = Path.GetDirectoryName(assemblyPath);
            server = LaunchServer();
            workTask = Task.Factory.StartNew(() =>
            {
                while (!server.HasExited)
                    ProcessLine(server.StandardError.ReadLine());
            });
        }

        /// <summary>
        /// Send a stop command to the Minecraft server and wait for it to exit.
        /// </summary>
        /// <param name="requestMoreTime">Action to call to request more time from the SCM</param>
        internal void Stop(Action<int> requestMoreTime)
        {
            if (requestMoreTime == null)
                throw new ArgumentNullException("requestMoreTime");

            log.Info("sending stop");
            server.StandardInput.WriteLine("stop");

            // wait for the task to complete, periodically requesting more time from the SCM
            while (!workTask.Wait(1000))
                requestMoreTime(1000);
        }

        /// <summary>
        /// Send a command to the server. 
        /// </summary>
        /// <param name="cmd"></param>
        public void SendCommandText(string cmd)
        {
            try
            {
                log.InfoFormat("Issuing command: \"{0}\"", cmd);
                server.StandardInput.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        /// <summary>
        /// Spawn the java process with the configured command line.
        /// </summary>
        /// <returns></returns>
        private Process LaunchServer()
        {
            var p = new Process();
            p.StartInfo.FileName = Path.Combine( ConfigUtil.GetAppSetting("Java.exe Path", ""), "java.exe" );
            p.StartInfo.Arguments = ConfigUtil.GetAppSetting("Java Params", "-Xmx1024M -Xms1024M -jar server.jar -nojline nogui");
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            p.EnableRaisingEvents = true;
            p.Exited += (s, e) => { log.Info("Server Exited"); };
            p.Start();
            return p;
        }

        private void ProcessLine(string line)
        {
            if (line == null) return;

            // strip the log message prefix
            var lineParts = line.Split(new[] { "[INFO] ", "[WARNING] " }, 2, StringSplitOptions.None);
            line = lineParts[lineParts.Length - 1];

            // log the text
            if (ConfigUtil.GetAppSetting("Log console messages", "F") == "T")
              logcon.Info(line);

            // Keep a history to display when a remote user connects
            // todo: may need to keep track of the source of lines added to the buffer, so commands entered
            // by remote users appear on all active remote consoles.
            consoleHist.Add(line);
            OnNewLine(line);

            try
            {
                // process the line through the user script 
                // and send any output back to the server
                foreach (var s in ScriptHost.ProcessServerLine(line))
                    SendCommandText(s);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}
