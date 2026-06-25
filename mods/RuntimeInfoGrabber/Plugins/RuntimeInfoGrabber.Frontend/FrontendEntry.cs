using System;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace RuntimeInfoGrabber.Frontend
{
    [PluginConfig("RuntimeInfoGrabber.Frontend", "WQMYH", "0.1.0")]
    public class FrontendEntry : TaiwuRemakePlugin
    {
        private static Harmony? _harmony;

        public override void Initialize()
        {
            try
            {
                DiscoveryDumper.LoadConfig();
                DiscoveryDumper.EnsureDumpDirectory();
                DiscoveryDumper.ExportStaticCatalogs();

                _harmony = new Harmony("com.runtimeinfograbber.frontend.discovery");
                DiscoveryDumper.InstallPatches(_harmony);

                AdaptableLog.Info("[RuntimeInfoGrabber] Frontend discovery plugin loaded successfully.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to load frontend plugin: {ex}");
            }
        }

        public override void Dispose()
        {
            try
            {
                _harmony?.UnpatchSelf();
                _harmony = null;

                AdaptableLog.Info("[RuntimeInfoGrabber] Frontend discovery plugin unloaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to unload frontend plugin: {ex}");
            }
        }
    }
}
