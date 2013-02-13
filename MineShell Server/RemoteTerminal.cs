using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using AC0KG.Utils;
using System.Threading.Tasks;
using System.Threading;

namespace AC0KG.Minecraft.MineShell
{
    public class RemoteTerminalNewLineArgs : EventArgs
    {
        public string Line;
        public string User;
        public RemoteTerminalNewLineArgs(string user, string line)
        {
            Line = line;
            User = user;
        }
    }

    public class RemoteTerminalUserConnectedArgs : EventArgs
    {
        public string User;
        public StreamWriter Writer;
        public RemoteTerminalUserConnectedArgs(string user, StreamWriter writer)
        {
            User = user;
            Writer = writer;
        }
    }

    sealed class RemoteTerminal
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("Remote Terminal");

        private static CancellationTokenSource cte = new CancellationTokenSource();

        private static List<StreamWriter> clientWriters = new List<StreamWriter>();

        /// <summary>
        /// Called when a new line has been received from the minecraft server
        /// </summary>
        public static event EventHandler<RemoteTerminalNewLineArgs> NewLine;

        /// <summary>
        /// Called when a new user has connected.
        /// </summary>
        public static event EventHandler<RemoteTerminalUserConnectedArgs> NewUser;

        /// <summary>
        /// Authentication function, receives user, password, and host,  return true or false.
        /// </summary>
        public static Func<string, string, string, bool> Authenticator;

        private static void OnNewLine(string user, string line)
        {
            if (line == null)
                return;

            // Not sure why I'm getting nulls at the beginning of all but the first line.
            // Possibly something to do with the client (putty), or just how tcp streams work.
            // Whatever, can't have 'em there, so trim those off.
            line = line.TrimStart(new[]{'\0'});

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(line))
                return;

            var nle = NewLine;
            if (nle != null)
                nle(null, new RemoteTerminalNewLineArgs(user, line));
        }

        private static void OnNewUser(string user, StreamWriter writer)
        {
            var nue = NewUser;
            if (nue != null)
                nue(null, new RemoteTerminalUserConnectedArgs(user, writer));
        }
        
        public static void Stop()
        {
            cte.Cancel();
        }

        public static void Start()
        {          
            int iport;
            var sport = ConfigUtil.GetAppSetting("Remote Console Port");
            if (!int.TryParse(sport, out iport))
                log.Error("Unable to read Remote Console Port setting: " + sport);

            var tcpListener = new TcpListener(IPAddress.Any, iport);
            tcpListener.Start();
            Task.Factory.StartNew(() =>
            {
                log.Debug("Waiting for clients");
                while (!cte.Token.WaitHandle.WaitOne(100))
                {
                    if (tcpListener.Pending())
                    {
                        log.Debug("New client");
                        Task.Factory.StartNew(() => { ClientHandler(tcpListener.AcceptTcpClient()); });
                    }
                }
                log.Debug("closing listening socket");
                tcpListener.Stop();
            }, cte.Token);
        }

        /// <summary>
        /// Send line to all connected clients
        /// </summary>
        /// <param name="line"></param>
        public static void Broadcast(string line)
        {
            foreach (var client in RemoteTerminal.clientWriters)
                try
                {
                    client.WriteLine(line);
                }
                catch
                { }
        }

        private static void ClientHandler(TcpClient tcpClient)
        {
            try
            {
                log.Debug("Remote client connected");

                var clientStream = tcpClient.GetStream();
                var reader = new StreamReader(clientStream);
                var writer = new StreamWriter(clientStream);
                writer.AutoFlush = true;

                // this authentication portion could all be broken out 
                // into a separate module, but this is good enough for now.
                writer.Write("user:");
                var user = reader.ReadLine();
                writer.Write("\r\npass:");
                var pass = reader.ReadLine().TrimStart("\0".ToCharArray());
                var rHost = tcpClient.Client.RemoteEndPoint.ToString();

                string msg;
                if (Authenticator != null)
                    if (Authenticator(user, pass, rHost))
                        writer.WriteLine("Authenticated as " + user);
                    else
                    {
                        writer.WriteLine("Authentication failed");
                        Thread.Sleep(2000); // some clients will close the window straight away, so for usability, give it a moment before disconnect.
                        tcpClient.Close();
                        return;
                    }

                clientWriters.Add(writer);

                OnNewUser(user, writer);

                while (tcpClient.Connected)
                {
                    try
                    {
                        OnNewLine(string.Format("{0}@{1}", user, rHost), reader.ReadLine());
                    }
                    catch ( IOException )
                    {
                        log.Debug("socket closed?");
                        break;
                    }
                }

                clientWriters.Remove(writer);
                tcpClient.Close();

                msg = string.Format("Remote user disconneted \"{0}@{1}\"", user, rHost);
                log.Info(msg);
                Broadcast(msg);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}
