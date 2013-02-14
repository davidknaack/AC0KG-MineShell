using System;
using System.ServiceProcess;
using System.IO;

namespace AC0KG.Minecraft.MineShell
{
    public partial class MinecraftService : ServiceBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("MinecraftService");

        public MinecraftService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Info("Start");
            RemoteShell.Start();
        }

        protected override void OnStop()
        {
            log.Info("Stop");
            RemoteShell.Stop((ms) => { this.RequestAdditionalTime(ms); });
        }
    }
}
