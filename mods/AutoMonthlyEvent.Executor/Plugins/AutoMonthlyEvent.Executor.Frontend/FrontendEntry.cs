using System;
using System.Collections.Generic;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace AutoMonthlyEvent.Executor.Frontend
{
    [PluginConfig("AutoMonthlyEvent.Executor.Frontend", "AutoMonthlyEvent.Executor", "0.1.0")]
    public sealed class FrontendEntry : TaiwuRemakePlugin, IAsyncMethodRequestHandler
    {
        private static Harmony? _harmony;
        private readonly HashSet<int> _asyncRequests = new HashSet<int>();

        public override void Initialize()
        {
            try
            {
                ReloadSettings();

                _harmony = new Harmony("com.auto.monthlyevent.executor.frontend");
                EventExecutionController.InstallPatches(_harmony);
                MonthlyAutomationController.Install(_harmony);

                AdaptableLog.Info("[AutoMonthlyEvent.Executor] Frontend executor plugin loaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[AutoMonthlyEvent.Executor] Failed to load frontend plugin: {ex}");
            }
        }

        public override void OnModSettingUpdate()
        {
            try
            {
                ReloadSettings();
                AdaptableLog.Info("[AutoMonthlyEvent.Executor] Frontend settings reloaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[AutoMonthlyEvent.Executor] Failed to reload frontend settings: {ex}");
            }
        }

        private void ReloadSettings()
        {
            ExecutorConfig config = ExecutorConfig.Load(ModIdStr);
            MonthlyAutomationSettings.Initialize(config.ModDirectoryPath);
            ActionLogger.Configure(config);
            RelationConditionResolver.Configure(this, config);
            EventExecutionController.Configure(config);
        }

        public override void Dispose()
        {
            try
            {
                _harmony?.UnpatchSelf();
                MonthlyAutomationController.Reset();
                MonthlySettingsPanel.Dispose();
                _harmony = null;
                ClearAsyncMethodCalls();
                AdaptableLog.Info("[AutoMonthlyEvent.Executor] Frontend executor plugin unloaded.");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[AutoMonthlyEvent.Executor] Failed to unload frontend plugin: {ex}");
            }
        }

        public void RegisterAsyncMethodCall(int requestId)
        {
            _asyncRequests.Add(requestId);
        }

        public void ClearAsyncMethodCalls()
        {
            _asyncRequests.Clear();
        }
    }
}
