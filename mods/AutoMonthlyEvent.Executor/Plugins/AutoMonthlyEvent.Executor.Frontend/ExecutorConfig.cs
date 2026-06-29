using System;
using System.Collections.Generic;
using System.IO;
using GameData.Utilities;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal sealed class ExecutorConfig
    {
        public bool EnableAutoExecute { get; private set; }
        public bool DryRun { get; private set; }
        public bool EnableRequestCategory { get; private set; }
        public bool EnableResultCategory { get; private set; }
        public bool EnableFamilyCategory { get; private set; }
        public bool EnableFrontendAutoSelectCategory { get; private set; }

        private bool _enableFrontendKeywordSelect;
        private bool _enableFrontendRememberSelection;
        private bool _enableFrontendMemorySelect;
        private bool _enableFrontendSingleOptionContinue;
        private bool _enableAnySingleOptionContinue;
        private bool _autoContinueWhitelistedResults;
        private bool _enableMonthlyRequest;
        private bool _enableResourceRequest = true;
        private bool _enableTeaWineItemRequest = true;
        private bool _enableRequestResultContinue;
        private bool _enableGuidanceResultContinue;
        private bool _enableAdoptAbandonedBaby;
        private bool _enablePrenatalEducation;
        private bool _enablePrenatalEducationResultContinue;
        private bool _enableBirthNaming;

        public bool EnableFrontendKeywordSelect => EnableFrontendAutoSelectCategory && _enableFrontendKeywordSelect;
        public bool EnableFrontendRememberSelection => _enableFrontendRememberSelection;
        public bool EnableFrontendMemorySelect => EnableFrontendAutoSelectCategory && _enableFrontendMemorySelect;
        public bool EnableFrontendSingleOptionContinue => EnableFrontendAutoSelectCategory && _enableFrontendSingleOptionContinue;
        public bool EnableAnySingleOptionContinue => EnableFrontendAutoSelectCategory && _enableAnySingleOptionContinue;
        public bool AutoContinueWhitelistedResults => EnableResultCategory && _autoContinueWhitelistedResults;
        public bool EnableMonthlyRequest => EnableRequestCategory && _enableMonthlyRequest;
        public bool EnableResourceRequest => EnableRequestCategory && _enableResourceRequest;
        public bool EnableTeaWineItemRequest => EnableRequestCategory && _enableTeaWineItemRequest;
        public bool EnableRequestResultContinue => EnableResultCategory && _enableRequestResultContinue;
        public bool EnableGuidanceResultContinue => EnableResultCategory && _enableGuidanceResultContinue;
        public bool EnableAdoptAbandonedBaby => EnableFamilyCategory && _enableAdoptAbandonedBaby;
        public bool EnablePrenatalEducation => EnableFamilyCategory && _enablePrenatalEducation;
        public bool EnablePrenatalEducationResultContinue =>
            EnableFamilyCategory && _enablePrenatalEducation && _enablePrenatalEducationResultContinue;
        public bool EnableBirthNaming => EnableFamilyCategory && _enableBirthNaming;

        public int RequestRelationMode { get; private set; } = 3;
        public int FallbackFavorabilityThreshold { get; private set; } = 15000;
        public int PrenatalEducationChoice { get; private set; } = 3;
        public int AdoptionMaxChildAge { get; private set; } = 3;
        public bool TaiwuBirthUseOwnSurname { get; private set; } = true;
        public bool PartnerBirthUseMotherSurname { get; private set; }
        public bool BirthFallbackManualNaming { get; private set; }
        public string BirthGenerationCharacter { get; private set; } = string.Empty;
        public string BirthGivenNameSuffix { get; private set; } = string.Empty;
        public bool EnableCustomDialogSkip { get; private set; }
        public string CustomDialogSkipSuspendHotkey { get; private set; } = "Ctrl+A";
        public bool EnableActionLog { get; private set; } = true;
        public bool EnableDebugLog { get; private set; } = true;
        public string LogDirectory { get; private set; } = "Logs";
        public string ActionLogFileName { get; } = "executor_actions.jsonl";
        public string HumanLogFileName { get; } = "executor_actions.log";
        public string DebugLogFileName { get; } = "executor_debug.jsonl";
        public string DebugHumanLogFileName { get; } = "executor_debug.log";
        public HashSet<ushort> AllowedRelationTypes { get; } = new HashSet<ushort>();
        public HashSet<sbyte> AllowedAdoptionBehaviorTypes { get; } = new HashSet<sbyte> { 0, 1, 2 };
        public string ModDirectoryPath { get; private set; } = string.Empty;

        public static ExecutorConfig Load(string modIdStr)
        {
            var config = new ExecutorConfig();
            ApplySettings(config, modIdStr);
            ResolveModDirectory(config, modIdStr);

            AdaptableLog.Info(
                $"[AutoMonthlyEvent.Executor] Frontend settings loaded from ModManager. " +
                $"ModId={modIdStr}, ModDirectory={config.ModDirectoryPath}, " +
                $"EnableAutoExecute={config.EnableAutoExecute}, DryRun={config.DryRun}, " +
                $"RequestCategory={config.EnableRequestCategory}, MonthlyRequest={config.EnableMonthlyRequest}, " +
                $"ResultCategory={config.EnableResultCategory}, FamilyCategory={config.EnableFamilyCategory}, " +
                $"FrontendAutoSelectCategory={config.EnableFrontendAutoSelectCategory}, " +
                $"CustomDialogSkip={config.EnableCustomDialogSkip}");
            return config;
        }

        private static void ApplySettings(ExecutorConfig config, string modIdStr)
        {
            config.EnableAutoExecute = ReadSetting(modIdStr, nameof(EnableAutoExecute), config.EnableAutoExecute);
            config.DryRun = ReadSetting(modIdStr, nameof(DryRun), config.DryRun);
            config.EnableActionLog = ReadSetting(modIdStr, nameof(EnableActionLog), config.EnableActionLog);
            config.EnableDebugLog = ReadSetting(modIdStr, nameof(EnableDebugLog), config.EnableDebugLog);
            config.LogDirectory = SanitizeRelativePath(
                ReadSetting(modIdStr, nameof(LogDirectory), config.LogDirectory), "Logs");

            config.EnableRequestCategory = ReadSetting(modIdStr, nameof(EnableRequestCategory), config.EnableRequestCategory);
            config._enableMonthlyRequest = ReadSetting(modIdStr, nameof(EnableMonthlyRequest), config._enableMonthlyRequest);
            config._enableResourceRequest = ReadSetting(modIdStr, nameof(EnableResourceRequest), config._enableResourceRequest);
            config._enableTeaWineItemRequest = ReadSetting(modIdStr, nameof(EnableTeaWineItemRequest), config._enableTeaWineItemRequest);
            config.RequestRelationMode = NormalizeRequestRelationMode(
                ReadSetting(modIdStr, nameof(RequestRelationMode), config.RequestRelationMode));
            config.FallbackFavorabilityThreshold = ReadSetting(
                modIdStr, nameof(FallbackFavorabilityThreshold), config.FallbackFavorabilityThreshold);
            ResetAllowedRelationTypes(config);

            config.EnableResultCategory = ReadSetting(modIdStr, nameof(EnableResultCategory), config.EnableResultCategory);
            config._autoContinueWhitelistedResults = ReadSetting(
                modIdStr, nameof(AutoContinueWhitelistedResults), config._autoContinueWhitelistedResults);
            config._enableRequestResultContinue = ReadSetting(
                modIdStr, nameof(EnableRequestResultContinue), config._enableRequestResultContinue);
            config._enableGuidanceResultContinue = ReadSetting(
                modIdStr, nameof(EnableGuidanceResultContinue), config._enableGuidanceResultContinue);

            config.EnableFamilyCategory = ReadSetting(modIdStr, nameof(EnableFamilyCategory), config.EnableFamilyCategory);
            config._enableAdoptAbandonedBaby = ReadSetting(
                modIdStr, nameof(EnableAdoptAbandonedBaby), config._enableAdoptAbandonedBaby);
            config.AdoptionMaxChildAge = ReadSetting(modIdStr, nameof(AdoptionMaxChildAge), config.AdoptionMaxChildAge);
            string behaviorTypes = ReadSetting(
                modIdStr, nameof(AllowedAdoptionBehaviorTypes), "0,1,2");
            ApplyAdoptionBehaviorTypes(config, behaviorTypes);
            config._enablePrenatalEducation = ReadSetting(
                modIdStr, nameof(EnablePrenatalEducation), config._enablePrenatalEducation);
            config.PrenatalEducationChoice = NormalizePrenatalEducationChoice(
                ReadSetting(modIdStr, nameof(PrenatalEducationChoice), config.PrenatalEducationChoice));
            config._enablePrenatalEducationResultContinue = ReadSetting(
                modIdStr, nameof(EnablePrenatalEducationResultContinue), config._enablePrenatalEducationResultContinue);
            config._enableBirthNaming = ReadSetting(modIdStr, nameof(EnableBirthNaming), config._enableBirthNaming);
            config.TaiwuBirthUseOwnSurname = ReadSetting(
                modIdStr, nameof(TaiwuBirthUseOwnSurname), config.TaiwuBirthUseOwnSurname);
            config.PartnerBirthUseMotherSurname = ReadSetting(
                modIdStr, nameof(PartnerBirthUseMotherSurname), config.PartnerBirthUseMotherSurname);
            config.BirthFallbackManualNaming = ReadSetting(
                modIdStr, nameof(BirthFallbackManualNaming), config.BirthFallbackManualNaming);
            config.BirthGenerationCharacter = NormalizeOneChar(
                ReadSetting(modIdStr, nameof(BirthGenerationCharacter), config.BirthGenerationCharacter));
            config.BirthGivenNameSuffix = NormalizeOneChar(
                ReadSetting(modIdStr, nameof(BirthGivenNameSuffix), config.BirthGivenNameSuffix));

            config.EnableFrontendAutoSelectCategory = ReadSetting(
                modIdStr, nameof(EnableFrontendAutoSelectCategory), config.EnableFrontendAutoSelectCategory);
            config._enableFrontendKeywordSelect = ReadSetting(
                modIdStr, nameof(EnableFrontendKeywordSelect), config._enableFrontendKeywordSelect);
            config._enableFrontendRememberSelection = ReadSetting(
                modIdStr, nameof(EnableFrontendRememberSelection), config._enableFrontendRememberSelection);
            config._enableFrontendMemorySelect = ReadSetting(
                modIdStr, nameof(EnableFrontendMemorySelect), config._enableFrontendMemorySelect);
            config._enableFrontendSingleOptionContinue = ReadSetting(
                modIdStr, nameof(EnableFrontendSingleOptionContinue), config._enableFrontendSingleOptionContinue);
            config._enableAnySingleOptionContinue = ReadSetting(
                modIdStr, nameof(EnableAnySingleOptionContinue), config._enableAnySingleOptionContinue);
            config.EnableCustomDialogSkip = ReadSetting(
                modIdStr, nameof(EnableCustomDialogSkip), config.EnableCustomDialogSkip);
            config.CustomDialogSkipSuspendHotkey = ReadSetting(
                modIdStr, nameof(CustomDialogSkipSuspendHotkey), config.CustomDialogSkipSuspendHotkey).Trim();
        }

        private static bool ReadSetting(string modIdStr, string key, bool fallback)
        {
            bool value = fallback;
            try
            {
                if (!global::ModManager.GetSetting(modIdStr, key, ref value))
                    AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Frontend setting not found: {key}");
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Failed to read frontend setting {key}: {ex.Message}");
            }
            return value;
        }

        private static int ReadSetting(string modIdStr, string key, int fallback)
        {
            int value = fallback;
            try
            {
                if (!global::ModManager.GetSetting(modIdStr, key, ref value))
                    AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Frontend setting not found: {key}");
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Failed to read frontend setting {key}: {ex.Message}");
            }
            return value;
        }

        private static string ReadSetting(string modIdStr, string key, string fallback)
        {
            string value = fallback;
            try
            {
                if (!global::ModManager.GetSetting(modIdStr, key, ref value))
                    AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Frontend setting not found: {key}");
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Failed to read frontend setting {key}: {ex.Message}");
            }
            return value;
        }

        private static void ResolveModDirectory(ExecutorConfig config, string modIdStr)
        {
            try
            {
                var modInfo = global::ModManager.GetModInfo(modIdStr);
                if (modInfo != null && !string.IsNullOrWhiteSpace(modInfo.DirectoryName))
                    config.ModDirectoryPath = Path.GetFullPath(modInfo.DirectoryName);
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Failed to resolve mod directory from ModInfo: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(config.ModDirectoryPath))
                return;

            try
            {
                config.ModDirectoryPath = Path.Combine(
                    Path.GetFullPath(global::ModManager.GetModRootFolder()),
                    "AutoMonthlyEvent.Executor");
                AdaptableLog.Warning(
                    $"[AutoMonthlyEvent.Executor] Using fallback frontend mod directory: {config.ModDirectoryPath}");
            }
            catch (Exception ex)
            {
                config.ModDirectoryPath = string.Empty;
                AdaptableLog.Warning(
                    $"[AutoMonthlyEvent.Executor] File logs and dialog memory are unavailable: {ex.Message}");
            }
        }

        private static void ResetAllowedRelationTypes(ExecutorConfig config)
        {
            config.AllowedRelationTypes.Clear();
            ushort[] values;
            switch (config.RequestRelationMode)
            {
                case 1:
                    values = new ushort[] { 1024, 1, 2 };
                    break;
                case 2:
                    values = new ushort[] { 1024, 1, 2, 64, 128, 512 };
                    break;
                default:
                    values = new ushort[] { 1024, 1, 2, 64, 128, 512, 8192 };
                    break;
            }

            foreach (ushort value in values)
                config.AllowedRelationTypes.Add(value);
        }

        private static void ApplyAdoptionBehaviorTypes(ExecutorConfig config, string text)
        {
            var parsed = new HashSet<sbyte>();
            foreach (string part in text.Split(','))
            {
                if (sbyte.TryParse(part.Trim(), out sbyte value) && value >= 0 && value <= 4)
                    parsed.Add(value);
            }

            if (parsed.Count == 0)
                return;

            config.AllowedAdoptionBehaviorTypes.Clear();
            foreach (sbyte value in parsed)
                config.AllowedAdoptionBehaviorTypes.Add(value);
        }

        private static int NormalizeRequestRelationMode(int value)
        {
            return value >= 1 && value <= 3 ? value : 3;
        }

        private static int NormalizePrenatalEducationChoice(int value)
        {
            return value >= 1 && value <= 3 ? value : 3;
        }

        private static string NormalizeOneChar(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            value = value.Trim();
            return value.Length <= 1 ? value : value.Substring(0, 1);
        }

        private static string SanitizeRelativePath(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) || value.Contains(".."))
                return fallback;
            return value.Trim().Trim('\\', '/');
        }
    }
}
