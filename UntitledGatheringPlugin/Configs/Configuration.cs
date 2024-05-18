using Dalamud.Configuration;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UntitledGatheringPlugin.Configs
{
    internal class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public float MinDurability { get; set; } = 80f;

        public void Save()
        {
            var configJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Svc.PluginInterface.ConfigFile.FullName, configJson);
        }
    }
}
