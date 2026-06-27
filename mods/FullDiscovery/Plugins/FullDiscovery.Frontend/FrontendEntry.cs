using System;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace FullDiscovery.Frontend
{
    [PluginConfig("FullDiscovery.Frontend", "WQMYH", "0.1.0")]
    public class FrontendEntry : TaiwuRemakePlugin
    {
        private static Harmony? _harmony;

        public override void Initialize()
        {
            try
            {
                FullDiscoveryDumper.LoadConfig();
                FullDiscoveryDumper.InitializeOutput();
                FullDiscoveryDumper.ExportStartupData();

                _harmony = new Harmony("com.fulldiscovery.frontend.readonly");
                FullDiscoveryDumper.InstallPatches(_harmony);

                AdaptableLog.Info("[FullDiscovery] Frontend loaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[FullDiscovery] Frontend initialize failed: {ex}");
            }
        }

        public override void Dispose()
        {
            try
            {
                _harmony?.UnpatchSelf();
                _harmony = null;
                FullDiscoveryDumper.WriteLog("Frontend unloaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[FullDiscovery] Frontend dispose failed: {ex}");
            }
        }
    }
}
