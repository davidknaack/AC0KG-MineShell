using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AC0KG.Utils;

using System.Net.Sockets;
using System.Net;

namespace AC0KG.Minecraft.Host
{
    public class MinecraftHost
    {
        // simple singleton
        private MinecraftHost(){ }
        public static readonly MinecraftHost instance = new MinecraftHost();

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("Minecraft Host");
        private static readonly log4net.ILog logcon = log4net.LogManager.GetLogger("con");
        
        private Task workTask;
        private Process server;

        /// <summary>
        /// Start the Minecraft server and set up the task that handles catching the output
        /// </summary>
        internal void Start()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
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
            logcon.Info(line);

            try
            {
                foreach (var s in ScriptHost.ProcessServerLine(line))
                    Console.WriteLine(s);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }

/***********************************************************************
 
  Copyright (c) 2010 Alex Zaitsev
  All rights reserved.
 
  Simple C# telnet server sample 
 
  You can use this code freely for any commercial or non-commercial
  purpose. However if you use this code in your program, you should
  add the string "Contains code by Alex Zaitsev, www.az3749.narod.ru"
  in your copyright notice text.
 
***********************************************************************/
    /*
 
 
     class AsyncRedirect
     {
         readonly byte[] buf = new byte[4096];
         readonly Stream r, w;
         readonly AsyncCallback AsyncCallback_;
         
         public AsyncRedirect(Stream Read, Stream Write) 
         { 
             r = Read; 
             w = Write; 
             AsyncCallback_ = this.AsyncCallback; 
         }
         
         void AsyncCallback(IAsyncResult ar)
         {
             if (!ar.IsCompleted) return;
             int n = 0;
         
             try { n = r.EndRead(ar); }
             catch (Exception e) {
                  log.Info("EndRead failed:{0}", e);
             }
             
             if (n > 0)+-+-
             {
                 w.Write(buf, 0, n);
                 w.Flush();
                 BeginRead();
             }
             else
             {
                 log.Info("read 0 bytes,finished");
                 w.Close();
             }
         }
         
         public IAsyncResult BeginRead()
         {
             return r.BeginRead(buf, 0, buf.Length, AsyncCallback_, null);
         }

         static void Main(string[] args)
         {
             var psi = new ProcessStartInfo("cmd.exe");
             psi.RedirectStandardInput = psi.RedirectStandardOutput = true;
             psi.UseShellExecute = false;
             
             var tcpListener = new TcpListener(IPAddress.Any, 23);
             tcpListener.Start();
             while (true)
             {
                 var tcpClient = tcpListener.AcceptTcpClient();
                 var clientStream = tcpClient.GetStream();
                 
                 var p = Process.Start(psi);
         
                 var Pro = new AsyncRedirect(p.StandardOutput.BaseStream, clientStream);
                 var Tcp = new AsyncRedirect(clientStream, p.StandardInput.BaseStream);
                 Pro.BeginRead();
                 Tcp.BeginRead();
             }
         }

     };*/
}
