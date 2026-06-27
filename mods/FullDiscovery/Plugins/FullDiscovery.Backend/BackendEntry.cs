using System;
using GameData.Utilities;
using TaiwuModdingLib.Core.Plugin;

namespace FullDiscovery.Backend
{
    [PluginConfig("FullDiscovery.Backend", "WQMYH", "0.1.0")]
    public class BackendEntry : TaiwuRemakePlugin
    {
        public override void Initialize()
        {
            try
            {
                AdaptableLog.Info("[FullDiscovery] Backend loaded. This plugin is read-only.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[FullDiscovery] Backend initialize failed: {ex}");
            }
        }

        public override void Dispose()
        {
            try
            {
                AdaptableLog.Info("[FullDiscovery] Backend unloaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[FullDiscovery] Backend dispose failed: {ex}");
            }
        }
    }
}
