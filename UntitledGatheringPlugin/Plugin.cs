using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Commands;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using UntitledGatheringPlugin.Configs;
using UntitledGatheringPlugin.Managers;
using UntitledGatheringPlugin.UI;
using ObjectManager = UntitledGatheringPlugin.Managers.ObjectManager;

namespace UntitledGatheringPlugin
{
    public class Plugin : IDalamudPlugin, IDisposable
    {
        public static string Name => "Untitled Gathering Plugin";

        internal static Configuration Config;

        private static WindowSystem WindowSystem;
        internal static DebugWindow DebugWindow;

        internal static ObjectManager ObjectManager;
        internal static MovementManager MovementManager;
        public Plugin(DalamudPluginInterface pluginInterface)
        {
            ECommonsMain.Init(pluginInterface, this, ECommons.Module.ObjectFunctions, ECommons.Module.ObjectLife, ECommons.Module.DalamudReflector);
            try
            {
                var configs = JsonConvert.DeserializeObject<Configuration>(
                    File.ReadAllText(Svc.PluginInterface.ConfigFile.FullName))
                    ?? new Configuration();

                Config = configs;
            }
            catch (Exception ex)
            {
                Svc.Log.Warning(ex, "Failed to load config");
                Config = new Configuration();
            }

            ObjectManager = new ObjectManager();
            MovementManager = new MovementManager();

            WindowSystem = new WindowSystem(Name);
            DebugWindow = new DebugWindow();

            WindowSystem.AddWindow(DebugWindow);

            Svc.PluginInterface.UiBuilder.Draw += OnDraw;
            Svc.PluginInterface.UiBuilder.OpenConfigUi += OnConfigUiOpen;
            Svc.Framework.Update += OnUpdate;
        }

        private void OnUpdate(IFramework framework)
        {
            MovementManager.Update();
            ObjectManager.Update();
        }

        [Cmd("/ugp", "Open debug menu", true, true)]
        public void OpenDebugMenu(string command, string args)
        {
            DebugWindow.IsOpen = true;
        }

        private void OnConfigUiOpen()
        {
            DebugWindow!.IsOpen = true;
        }

        private void OnDraw()
        {
            if (Svc.GameGui.GameUiHidden) return;
            WindowSystem.Draw();
        }

        public void Dispose()
        {
            Config.Save();
            ECommonsMain.Dispose();
        }

    }
}
