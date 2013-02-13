using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AC0KG.Minecraft.MineShell
{
    /// <summary>
    /// Handles hooking up the minecraft host and the remote terminal manager
    /// </summary>
    class RemoteShell
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("RemoteShell");

        static bool AuthFunc(string user, string pass, string host)
        {
            if (user == "root" && pass == "munch")
            {
                var msg = string.Format("Remote auth success for user \"{0}\" from {1}", user, host);
                log.Info(msg);
                RemoteTerminal.Broadcast(msg);
                return true;
            }
            else
            {
                var msg = string.Format("Remote auth failed for user \"{0}\" from {1}", user, host);
                log.Info(msg);
                RemoteTerminal.Broadcast(msg);
            }
            return false;
        }

        public static void NewRemoteUser(object sender, RemoteTerminalUserConnectedArgs args)
        {
            foreach (var l in MinecraftHost.instance.ConsoleHistory)
                args.Writer.WriteLine(l);
        }

        public static void Start()
        {
            MinecraftHost.instance.Start();

            // Set up the remote terminal server
            RemoteTerminal.Authenticator = AuthFunc;
            RemoteTerminal.NewUser += NewRemoteUser;
            RemoteTerminal.Start();

            // Link the remote terminal server and minecraft console
            MinecraftHost.instance.NewLine += (s, e) => RemoteTerminal.Broadcast(e.Value);
            RemoteTerminal.NewLine += (s, e) =>
                {
                    MinecraftHost.instance.SendCommandText(e.Line);
                    RemoteTerminal.Broadcast(string.Format("({0})->{1}", e.User, e.Line));
                };
        }

        public static void Stop(Action<int> requestMoreTime)
        {
            RemoteTerminal.Broadcast("MineShell stopping");
            RemoteTerminal.Stop();
            MinecraftHost.instance.Stop(requestMoreTime);
        }
    }
}
