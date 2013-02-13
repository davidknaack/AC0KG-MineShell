using System;
using System.Collections.Generic;

public class MinecraftScript
{
    private class MinecraftUser
    {
        public string username;
        public bool isAdmin = false;
        public List<String> chat = new List<string>();
        public List<String> cheats = new List<string>();
    }

    private static List<string> ops = new List<string>();
    private static Dictionary<string, MinecraftUser> users = new Dictionary<string, MinecraftUser>();

    public static void SetOpsList(List<string> opsList)
    {
        ops = opsList;
    }

    public static List<string> ProcessServerLine(string line)
    {

        // Console.WriteLine("script got: " + line);

        // the contents of 'commands' will be sent to the Minecraft server console
        var commands = new List<string>();
        var splitLine = line.Split(" ".ToCharArray());
       
        // Help text may trigger certain commands.
        // This is a bad hack, the 'line.Contains' tests should really be improved.
        if (line.StartsWith("/")) 
            return commands;

        //=======================================================
        if (line.Contains("logged in with entity id"))
        {
            users.Add(splitLine[3], new MinecraftUser()
            {
                username = splitLine[3],
                isAdmin = ops.Contains(splitLine[3].ToLower())
            });
        }

        
        //=======================================================
        if (line.Contains("lost connection: "))
        {
            users.Remove(splitLine[3]);
        }


        //=======================================================
        if (line.Contains("<"))
        {
            var part = line.Substring(line.IndexOf("<") + 1);
            var username = part.Substring(0, part.IndexOf(">"));
            var chatmessage = part.Substring(part.IndexOf(">") + 1);
            users[username].chat.Add(chatmessage);
        }


        //=======================================================
        if (line.ToLower().Contains(": give"))
        {
            var username = splitLine[3].Replace(":", "");
            var cheat = line.Split(":".ToCharArray())[3];
            users[username].cheats.Add(cheat);
        }


        //=======================================================
        if (line.ToLower().Contains(": set time to"))
        {
            var username = splitLine[3].Replace(":", "");
            var cheat = line.Split(":".ToCharArray())[3];
            users[username].cheats.Add(cheat);
        }


        //=======================================================
        if (line.ToLower().Contains("issued server command: "))
        {
            var username = splitLine[3];
            var command = line.Split(":".ToCharArray())[3].Trim();
            var commandpart = command.Split(" ".ToCharArray());
            switch (commandpart[0])
            {
                case "day":
                    commands.Add("time set 0");
                    break;

                case "night":
                    commands.Add("time set 14000");
                    break;

                case "showcheats":
                    if (commandpart.Length < 2)
                        commands.Add("say Missing parameter: username");
                    else
                        foreach (var cheat in users[commandpart[1]].cheats)
                            commands.Add(string.Format("say {0}: {1}", commandpart[1], cheat));
                    break;
            }
        }
        return commands;
    }
}