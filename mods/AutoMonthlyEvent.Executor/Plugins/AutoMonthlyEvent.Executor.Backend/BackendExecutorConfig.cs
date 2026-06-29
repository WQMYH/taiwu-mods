using System;
using System.IO;
using GameData.Domains;
using NLog;

namespace AutoMonthlyEvent.Executor.Backend
{
    internal sealed class BackendExecutorConfig
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public bool EnableAutoExecute { get; private set; }
        public bool EnableRequestCategory { get; private set; }
        public bool EnableFamilyCategory { get; private set; }
        public bool EnableBackendItemRequestInterceptor { get; private set; }
        public bool EnableBackendGiftInterceptor { get; private set; }
        public bool EnableActionLog { get; private set; } = true;
        public bool EnableDebugLog { get; private set; } = true;
        public string LogDirectory { get; private set; } = "Logs";
        public string ModDirectoryPath { get; private set; } = string.Empty;

        public bool EffectiveItemRequestInterceptor => EnableAutoExecute && EnableRequestCategory && EnableBackendItemRequestInterceptor;
        public bool EffectiveGiftInterceptor => EnableAutoExecute && EnableRequestCategory && EnableBackendGiftInterceptor;

        public static BackendExecutorConfig Load(string modIdStr)
        {
            var config = new BackendExecutorConfig();
            try
            {
                config.EnableAutoExecute = ReadSetting(modIdStr, nameof(EnableAutoExecute), config.EnableAutoExecute);
                config.EnableRequestCategory = ReadSetting(modIdStr, nameof(EnableRequestCategory), config.EnableRequestCategory);
                config.EnableFamilyCategory = ReadSetting(modIdStr, nameof(EnableFamilyCategory), config.EnableFamilyCategory);
                config.EnableBackendItemRequestInterceptor = ReadSetting(modIdStr, nameof(EnableBackendItemRequestInterceptor), config.EnableBackendItemRequestInterceptor);
                config.EnableBackendGiftInterceptor = ReadSetting(modIdStr, nameof(EnableBackendGiftInterceptor), config.EnableBackendGiftInterceptor);
                config.EnableActionLog = ReadSetting(modIdStr, nameof(EnableActionLog), config.EnableActionLog);
                config.EnableDebugLog = ReadSetting(modIdStr, nameof(EnableDebugLog), config.EnableDebugLog);
                config.LogDirectory = ReadSetting(modIdStr, nameof(LogDirectory), config.LogDirectory);
                config.LogDirectory = SanitizeRelativePath(config.LogDirectory, "Logs");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"[AutoMonthlyEvent.Executor] Failed to load backend settings for ModId={modIdStr}.");
            }

            try
            {
                config.ModDirectoryPath = DomainManager.Mod.GetModDirectory(modIdStr);
            }
            catch (Exception ex)
            {
                config.ModDirectoryPath = string.Empty;
                Logger.Warn(ex, "[AutoMonthlyEvent.Executor] Backend file logging is unavailable because the mod directory could not be resolved.");
            }

            Logger.Info($"[AutoMonthlyEvent.Executor] Backend settings loaded from ModDomain. ModId={modIdStr}, ModDirectory={config.ModDirectoryPath}, EnableAutoExecute={config.EnableAutoExecute}, RequestCategory={config.EnableRequestCategory}, FamilyCategory={config.EnableFamilyCategory}, Item={config.EnableBackendItemRequestInterceptor}, Gift={config.EnableBackendGiftInterceptor}, LogDirectory={config.LogDirectory}");
            return config;
        }

        private static bool ReadSetting(string modIdStr, string key, bool fallback)
        {
            bool value = fallback;
            try
            {
                if (!DomainManager.Mod.GetSetting(modIdStr, key, ref value))
                    Logger.Warn($"[AutoMonthlyEvent.Executor] Backend setting not found: {key}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"[AutoMonthlyEvent.Executor] Failed to read backend setting {key}.");
            }
            return value;
        }

        private static string ReadSetting(string modIdStr, string key, string fallback)
        {
            string value = fallback;
            try
            {
                if (!DomainManager.Mod.GetSetting(modIdStr, key, ref value))
                    Logger.Warn($"[AutoMonthlyEvent.Executor] Backend setting not found: {key}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"[AutoMonthlyEvent.Executor] Failed to read backend setting {key}.");
            }
            return value;
        }

        private static string SanitizeRelativePath(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) || value.Contains(".."))
                return fallback;
            return value.Trim().Trim('\\', '/');
        }
    }
}
