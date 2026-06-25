using System;
using GameData.Utilities;
using TaiwuModdingLib.Core.Plugin;

namespace RuntimeInfoGrabber.Backend
{
    [PluginConfig("RuntimeInfoGrabber.Backend", "WQMYH", "0.1.0")]
    public class BackendEntry : TaiwuRemakePlugin
    {
        public override void Initialize()
        {
            try
            {
                AdaptableLog.Info("[RuntimeInfoGrabber] Backend plugin loaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to load backend plugin: {ex}");
            }
        }

        public override void Dispose()
        {
            try
            {
                AdaptableLog.Info("[RuntimeInfoGrabber] Backend plugin unloaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to unload backend plugin: {ex}");
            }
        }
    }
}
