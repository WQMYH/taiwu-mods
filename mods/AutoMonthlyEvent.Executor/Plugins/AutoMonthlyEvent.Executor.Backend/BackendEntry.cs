using System;
using HarmonyLib;
using NLog;
using TaiwuModdingLib.Core.Plugin;

namespace AutoMonthlyEvent.Executor.Backend
{
    [PluginConfig("AutoMonthlyEvent.Executor.Backend", "AutoMonthlyEvent.Executor", "0.1.0")]
    public sealed class BackendEntry : TaiwuRemakePlugin
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Harmony? _harmony;

        public override void Initialize()
        {
            try
            {
                ReloadSettings();

                _harmony = new Harmony("com.auto.monthlyevent.executor.backend");
                BackendEventInterceptor.Install(_harmony);

                Logger.Info("[AutoMonthlyEvent.Executor] Backend plugin loaded.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[AutoMonthlyEvent.Executor] Failed to load backend plugin.");
            }
        }

        public override void OnModSettingUpdate()
        {
            try
            {
                ReloadSettings();
                Logger.Info("[AutoMonthlyEvent.Executor] Backend settings reloaded.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[AutoMonthlyEvent.Executor] Failed to reload backend settings.");
            }
        }

        private void ReloadSettings()
        {
            BackendExecutorConfig config = BackendExecutorConfig.Load(ModIdStr);
            BackendActionLogger.Configure(config);
            BackendEventInterceptor.Configure(config);
        }

        public override void Dispose()
        {
            try
            {
                _harmony?.UnpatchSelf();
                _harmony = null;
                Logger.Info("[AutoMonthlyEvent.Executor] Backend plugin unloaded.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "[AutoMonthlyEvent.Executor] Failed to unload backend plugin.");
            }
        }
    }
}
