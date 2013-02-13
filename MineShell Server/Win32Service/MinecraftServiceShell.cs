using System;
using System.ServiceProcess;
using System.IO;

namespace AC0KG.Minecraft.MineShell
{
    public partial class MinecraftService : ServiceBase
    {
        public MinecraftService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            eventlog.WriteEntry("Starting Minecraft server");
            RemoteShell.Start();
        }

        protected override void OnStop()
        {
            eventlog.WriteEntry("Stopping Minecraft server");
            RemoteShell.Stop((ms) => { this.RequestAdditionalTime(ms); });
        }
    }
}
