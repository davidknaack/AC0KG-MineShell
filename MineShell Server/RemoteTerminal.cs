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
            log.DebugFormat("OnNewLine: {0}:{1}", user, line);

            if (line == null)
            {
                log.WarnFormat("Received null from {0}", user);
                return;
            }

            // Compliant Telnet clients will send a null after bare carrage returns,
            // so trim those off if they are present.
            line = line.TrimStart(new[]{'\0'});

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(line))
                return;

            var nle = NewLine;
            if (nle != null)
                nle(null, new RemoteTerminalNewLineArgs(user, line));
        }

        private static void OnNewUser(string user, StreamWriter writer)
        {
            log.DebugFormat("OnNewUser: {0}", user);

            var nue = NewUser;
            if (nue != null)
                nue(null, new RemoteTerminalUserConnectedArgs(user, writer));
        }
        
        public static void Stop()
        {
            log.DebugFormat("Stop");
            cte.Cancel();
        }

        public static void Start()
        {
            log.DebugFormat("Start");
            int iport;
            var sport = ConfigUtil.GetAppSetting("Remote Console Port");
            if (!int.TryParse(sport, out iport))
                log.Error("Unable to read Remote Console Port setting: " + sport);

            var tcpListener = new TcpListener(IPAddress.Any, iport);
            tcpListener.Start();
            Task.Factory.StartNew(() =>
            {
                log.InfoFormat("Waiting for remote connections on {0}", tcpListener.LocalEndpoint);
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
            log.DebugFormat("Broadcast: {0}", line);

            try
            {
                foreach (var client in RemoteTerminal.clientWriters.ToArray())
                    try
                    {
                        client.WriteLine(line);
                    }
                    catch
                    { }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

        private static void ClientHandler(TcpClient tcpClient)
        {
            try
            {
                log.Debug("Remote client connected");

                var clientStream = tcpClient.GetStream();
                using (var reader = new StreamReader(clientStream))
                using (var writer = new StreamWriter(clientStream))
                {
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
                    try
                    {

                        OnNewUser(user, writer);

                        while (true)
                        {
                            try
                            {
                                var line = reader.ReadLine();
                                if (line == null)
                                    break;
                                else
                                    OnNewLine(string.Format("{0}@{1}", user, rHost), line);
                            }
                            catch (IOException e)
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        clientWriters.Remove(writer);
                    }

                    log.Info(msg=string.Format("Remote user disconnected \"{0}@{1}\"", user, rHost));
                    Broadcast(msg);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                tcpClient.Close();
            }
        }
    }
}
