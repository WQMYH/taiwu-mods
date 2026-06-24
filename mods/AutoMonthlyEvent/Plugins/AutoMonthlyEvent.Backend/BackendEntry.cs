using System;
using GameData.Utilities;
using TaiwuModdingLib.Core.Plugin;

namespace AutoMonthlyEvent.Backend
{
    [PluginConfig("AutoMonthlyEvent.Backend", "AutoMonthlyEvent", "0.1.0")]
    public class BackendEntry : TaiwuRemakePlugin
    {
        public override void Initialize()
        {
            try
            {
                AdaptableLog.Info("[AutoMonthlyEvent] Backend plugin loaded. Discovery mode is handled by frontend.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[AutoMonthlyEvent] Failed to load backend plugin: {ex}");
            }
        }

        public override void Dispose()
        {
            try
            {
                AdaptableLog.Info("[AutoMonthlyEvent] Backend plugin unloaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[AutoMonthlyEvent] Failed to unload backend plugin: {ex}");
            }
        }
    }
}
