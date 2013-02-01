using System;
using System.ServiceProcess;
using System.IO;
using AC0KG.Minecraft.Host;

namespace AC0KG.Minecraft.ServiceShell
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
            MinecraftHost.instance.Start();
        }

        protected override void OnStop()
        {
            eventlog.WriteEntry("Stopping Minecraft server");
            MinecraftHost.instance.Stop((ms) => { this.RequestAdditionalTime(ms); });
        }
    }
}
