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
using AC0KG.Utils;

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
            var users = ConfigUtil.GetAppSetting("Remote Users").Split(",".ToCharArray());
            var pwds = ConfigUtil.GetAppSetting("Remote User Passwords").Split(",".ToCharArray());
            var authed = false;

            if (users.Length != pwds.Length)
            {
                authed = false;
                log.Info("Remote user count does not match password count, all access is denied. Fix the config settings.");
            }
            else if (users.Length == 0)
            {
                authed = true;
                user = "<no user>";
                log.Info("No remote users have been configured, access is unrestricted! Read the config file.");
            } 
            else 
            {
                var i = 0;
                while ((i<users.Length) && !((users[i] == user) && (pwds[i] == pass)))
                    i++;
                authed = i != users.Length;
            }

            if (authed)
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
            // Send the recent history to the user
            foreach (var l in MinecraftHost.instance.ConsoleHistory)
                args.Writer.WriteLine(l);
        }

        public static void Start()
        {
            log.Debug("Start MinecraftHost");
            MinecraftHost.instance.Start();

            // Set up the remote terminal server
            log.Debug("Start RemoteTerminal");
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
            log.Debug("Stop");
            RemoteTerminal.Broadcast("MineShell stopping");
            RemoteTerminal.Stop();
            MinecraftHost.instance.Stop(requestMoreTime);
        }
    }
}
