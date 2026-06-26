using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GameData.Utilities;
using UnityEngine;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal sealed class ExecutorConfig
    {
        private const string ConfigFileName = "Config.lua";
        private const string SettingsFileName = "Settings.Lua";
        private const string ModDirectoryName = "AutoMonthlyEvent.Executor";

        public bool EnableAutoExecute { get; private set; } = false;
        public bool DryRun { get; private set; } = false;
        public string UnknownPolicy { get; private set; } = "WaitPlayer";
        public bool EnableRequestCategory { get; private set; } = false;
        public bool EnableResultCategory { get; private set; } = false;
        public bool EnableFamilyCategory { get; private set; } = false;
        public bool EnableSocialContestCategory { get; private set; } = false;
        public bool EnableFrontendAutoSelectCategory { get; private set; } = false;
        public bool EnableBackendInterceptorCategory { get; private set; } = false;
        private bool _enableFrontendKeywordSelect = false;
        private bool _enableFrontendRememberSelection = false;
        private bool _enableFrontendMemorySelect = false;
        private bool _enableFrontendSingleOptionContinue = false;
        private bool _enableAnySingleOptionContinue = false;
        public bool EnableFrontendKeywordSelect => EnableFrontendAutoSelectCategory && _enableFrontendKeywordSelect;
        public bool EnableFrontendRememberSelection => _enableFrontendRememberSelection;
        public bool EnableFrontendMemorySelect => EnableFrontendAutoSelectCategory && _enableFrontendMemorySelect;
        public bool EnableFrontendSingleOptionContinue => EnableFrontendAutoSelectCategory && _enableFrontendSingleOptionContinue;
        public bool EnableAnySingleOptionContinue => EnableFrontendAutoSelectCategory && _enableAnySingleOptionContinue;
        private bool _autoContinueWhitelistedResults = false;
        public bool AutoContinueWhitelistedResults => EnableResultCategory && _autoContinueWhitelistedResults;
        public string RequestDirection { get; private set; } = "NpcToTaiwu";
        public int RequestRelationMode { get; private set; } = 3;
        public int FallbackFavorabilityThreshold { get; private set; } = 15000;
        private bool _enableMonthlyRequest = false;
        private bool _enableResourceRequest = false;
        private bool _enableTeaWineItemRequest = false;
        private bool _enableRequestResultContinue = false;
        private bool _enableGuidanceResultContinue = false;
        private bool _enableAdoptAbandonedBaby = false;
        private bool _enablePrenatalEducation = false;
        private bool _enablePrenatalEducationResultContinue = false;
        private bool _enableBirthNaming = false;
        public bool EnableMonthlyRequest => EnableRequestCategory && _enableMonthlyRequest;
        public bool EnableResourceRequest => EnableRequestCategory && _enableResourceRequest;
        public bool EnableTeaWineItemRequest => EnableRequestCategory && _enableTeaWineItemRequest;
        public bool EnableRequestResultContinue => EnableResultCategory && _enableRequestResultContinue;
        public bool EnableGuidanceResultContinue => EnableResultCategory && _enableGuidanceResultContinue;
        public bool EnableAdoptAbandonedBaby => EnableFamilyCategory && _enableAdoptAbandonedBaby;
        public bool EnablePrenatalEducation => EnableFamilyCategory && _enablePrenatalEducation;
        public bool EnablePrenatalEducationResultContinue => EnableFamilyCategory && _enablePrenatalEducation && _enablePrenatalEducationResultContinue;
        public bool EnableBirthNaming => EnableFamilyCategory && _enableBirthNaming;
        public int PrenatalEducationChoice { get; private set; } = 3;
        public int AdoptionMaxChildAge { get; private set; } = 3;
        public bool TaiwuBirthUseOwnSurname { get; private set; } = true;
        public bool PartnerBirthUseMotherSurname { get; private set; } = false;
        public bool BirthFallbackManualNaming { get; private set; } = false;
        public string BirthGenerationCharacter { get; private set; } = string.Empty;
        public string BirthGivenNameSuffix { get; private set; } = string.Empty;
        public bool EnablePresetCustomDialogSkip { get; private set; } = false;
        public bool EnableCustomDialogSkip { get; private set; } = false;
        public string CustomDialogSkipSuspendHotkey { get; private set; } = "Ctrl+A";
        public bool EnableActionLog { get; private set; } = true;
        public bool EnableDebugLog { get; private set; } = true;
        public string LogDirectory { get; private set; } = "Logs";
        public string ActionLogFileName { get; private set; } = "executor_actions.jsonl";
        public string HumanLogFileName { get; private set; } = "executor_actions.log";
        public string DebugLogFileName { get; private set; } = "executor_debug.jsonl";
        public string DebugHumanLogFileName { get; private set; } = "executor_debug.log";
        public HashSet<ushort> AllowedRelationTypes { get; } = new HashSet<ushort> { 1024, 1, 2, 8, 16, 64, 128, 512, 8192 };
        public HashSet<sbyte> AllowedAdoptionBehaviorTypes { get; } = new HashSet<sbyte> { 0, 1, 2 };

        public string GameRootPath { get; private set; } = string.Empty;
        public string ModDirectoryPath { get; private set; } = string.Empty;

        public static ExecutorConfig Load()
        {
            var config = new ExecutorConfig();
            config.GameRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            config.ModDirectoryPath = Path.Combine(config.GameRootPath, "Mod", ModDirectoryName);
            string configPath = Path.Combine(config.ModDirectoryPath, ConfigFileName);
            string settingsPath = Path.Combine(config.ModDirectoryPath, SettingsFileName);

            try
            {
                if (!File.Exists(configPath))
                {
                    AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Config not found at {configPath}; using executor defaults.");
                }
                else
                {
                    ApplyContent(config, File.ReadAllText(configPath));
                }

                if (File.Exists(settingsPath))
                    ApplyContent(config, File.ReadAllText(settingsPath));
                else
                    AdaptableLog.Warning($"[AutoMonthlyEvent.Executor] Settings not found at {settingsPath}; using Config.lua/default values.");

                AdaptableLog.Info($"[AutoMonthlyEvent.Executor] Config loaded. EnableAutoExecute={config.EnableAutoExecute}, DryRun={config.DryRun}, EnableActionLog={config.EnableActionLog}, EnableDebugLog={config.EnableDebugLog}, LogDirectory={config.LogDirectory}, MonthlyRequest={config.EnableMonthlyRequest}, RequestResultContinue={config.EnableRequestResultContinue}, GuidanceResultContinue={config.EnableGuidanceResultContinue}, FamilyCategory={config.EnableFamilyCategory}, FrontendSingleOptionContinue={config.EnableFrontendSingleOptionContinue}");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[AutoMonthlyEvent.Executor] Failed to load config: {ex}");
            }

            return config;
        }

        private static void ApplyContent(ExecutorConfig config, string content)
        {
            content = StripLuaLineComments(content);
            config.EnableAutoExecute = ReadBool(content, nameof(EnableAutoExecute), config.EnableAutoExecute);
            config.DryRun = ReadBool(content, nameof(DryRun), config.DryRun);
            config.UnknownPolicy = ReadString(content, nameof(UnknownPolicy), config.UnknownPolicy);
            config.EnableRequestCategory = ReadBool(content, nameof(EnableRequestCategory), config.EnableRequestCategory);
            config.EnableResultCategory = ReadBool(content, nameof(EnableResultCategory), config.EnableResultCategory);
            config.EnableFamilyCategory = ReadBool(content, nameof(EnableFamilyCategory), config.EnableFamilyCategory);
            config.EnableSocialContestCategory = ReadBool(content, nameof(EnableSocialContestCategory), config.EnableSocialContestCategory);
            config.EnableFrontendAutoSelectCategory = ReadBool(content, nameof(EnableFrontendAutoSelectCategory), config.EnableFrontendAutoSelectCategory);
            config.EnableBackendInterceptorCategory = ReadBool(content, nameof(EnableBackendInterceptorCategory), config.EnableBackendInterceptorCategory);
            config._enableFrontendKeywordSelect = ReadBool(content, nameof(EnableFrontendKeywordSelect), config._enableFrontendKeywordSelect);
            config._enableFrontendRememberSelection = ReadBool(content, nameof(EnableFrontendRememberSelection), config._enableFrontendRememberSelection);
            config._enableFrontendMemorySelect = ReadBool(content, nameof(EnableFrontendMemorySelect), config._enableFrontendMemorySelect);
            config._enableFrontendSingleOptionContinue = ReadBool(content, nameof(EnableFrontendSingleOptionContinue), config._enableFrontendSingleOptionContinue);
            config._enableAnySingleOptionContinue = ReadBool(content, nameof(EnableAnySingleOptionContinue), config._enableAnySingleOptionContinue);
            config._autoContinueWhitelistedResults = ReadBool(content, nameof(AutoContinueWhitelistedResults), config._autoContinueWhitelistedResults);
            config.RequestDirection = ReadString(content, nameof(RequestDirection), config.RequestDirection);
            config.RequestRelationMode = NormalizeRequestRelationMode(ReadInt(content, nameof(RequestRelationMode), config.RequestRelationMode));
            ResetAllowedRelationTypesFromMode(config);
            config.FallbackFavorabilityThreshold = ReadInt(content, nameof(FallbackFavorabilityThreshold), config.FallbackFavorabilityThreshold);
            config._enableMonthlyRequest = ReadBool(content, nameof(EnableMonthlyRequest), config._enableMonthlyRequest);
            config._enableResourceRequest = ReadBool(content, nameof(EnableResourceRequest), config._enableResourceRequest);
            config._enableTeaWineItemRequest = ReadBool(content, nameof(EnableTeaWineItemRequest), config._enableTeaWineItemRequest);
            config._enableRequestResultContinue = ReadBool(content, nameof(EnableRequestResultContinue), config._enableRequestResultContinue);
            config._enableGuidanceResultContinue = ReadBool(content, nameof(EnableGuidanceResultContinue), config._enableGuidanceResultContinue);
            config._enableAdoptAbandonedBaby = ReadBool(content, nameof(EnableAdoptAbandonedBaby), config._enableAdoptAbandonedBaby);
            config._enablePrenatalEducation = ReadBool(content, nameof(EnablePrenatalEducation), config._enablePrenatalEducation);
            config._enablePrenatalEducationResultContinue = ReadBool(content, nameof(EnablePrenatalEducationResultContinue), config._enablePrenatalEducationResultContinue);
            config._enableBirthNaming = ReadBool(content, nameof(EnableBirthNaming), config._enableBirthNaming);
            config.PrenatalEducationChoice = NormalizePrenatalEducationChoice(ReadInt(content, nameof(PrenatalEducationChoice), config.PrenatalEducationChoice));
            config.AdoptionMaxChildAge = ReadInt(content, nameof(AdoptionMaxChildAge), config.AdoptionMaxChildAge);
            config.TaiwuBirthUseOwnSurname = ReadBool(content, nameof(TaiwuBirthUseOwnSurname), config.TaiwuBirthUseOwnSurname);
            config.PartnerBirthUseMotherSurname = ReadBool(content, nameof(PartnerBirthUseMotherSurname), config.PartnerBirthUseMotherSurname);
            config.BirthFallbackManualNaming = ReadBool(content, nameof(BirthFallbackManualNaming), config.BirthFallbackManualNaming);
            config.BirthGenerationCharacter = NormalizeOneChar(ReadString(content, nameof(BirthGenerationCharacter), config.BirthGenerationCharacter));
            config.BirthGivenNameSuffix = NormalizeOneChar(ReadString(content, nameof(BirthGivenNameSuffix), config.BirthGivenNameSuffix));
            config.EnablePresetCustomDialogSkip = ReadBool(content, nameof(EnablePresetCustomDialogSkip), config.EnablePresetCustomDialogSkip);
            config.EnableCustomDialogSkip = ReadBool(content, nameof(EnableCustomDialogSkip), config.EnableCustomDialogSkip);
            config.CustomDialogSkipSuspendHotkey = ReadString(content, nameof(CustomDialogSkipSuspendHotkey), config.CustomDialogSkipSuspendHotkey).Trim();
            config.EnableActionLog = ReadBool(content, nameof(EnableActionLog), config.EnableActionLog);
            config.EnableDebugLog = ReadBool(content, nameof(EnableDebugLog), config.EnableDebugLog);
            config.LogDirectory = SanitizeRelativePath(ReadString(content, nameof(LogDirectory), config.LogDirectory), "Logs");
            config.ActionLogFileName = SanitizeFileName(ReadString(content, nameof(ActionLogFileName), config.ActionLogFileName), "executor_actions.jsonl");
            config.HumanLogFileName = SanitizeFileName(ReadString(content, nameof(HumanLogFileName), config.HumanLogFileName), "executor_actions.log");
            config.DebugLogFileName = SanitizeFileName(ReadString(content, nameof(DebugLogFileName), config.DebugLogFileName), "executor_debug.jsonl");
            config.DebugHumanLogFileName = SanitizeFileName(ReadString(content, nameof(DebugHumanLogFileName), config.DebugHumanLogFileName), "executor_debug.log");

            List<int> relations = ReadIntList(content, nameof(AllowedRelationTypes));
            if (relations.Count > 0)
            {
                config.AllowedRelationTypes.Clear();
                foreach (int relation in relations)
                {
                    if (relation >= 0 && relation <= ushort.MaxValue)
                        config.AllowedRelationTypes.Add((ushort)relation);
                }
            }

            List<int> behaviorTypes = ReadIntList(content, nameof(AllowedAdoptionBehaviorTypes));
            if (behaviorTypes.Count > 0)
            {
                config.AllowedAdoptionBehaviorTypes.Clear();
                foreach (int behaviorType in behaviorTypes)
                {
                    if (behaviorType >= 0 && behaviorType <= 4)
                        config.AllowedAdoptionBehaviorTypes.Add((sbyte)behaviorType);
                }
            }
        }

        private static string StripLuaLineComments(string content)
        {
            return Regex.Replace(content, "--.*$", string.Empty, RegexOptions.Multiline);
        }

        private static bool ReadBool(string content, string key, bool fallback)
        {
            Match match = Regex.Match(content, key + "\\s*=\\s*(true|false)", RegexOptions.IgnoreCase);
            return match.Success ? string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase) : fallback;
        }

        private static int ReadInt(string content, string key, int fallback)
        {
            Match match = Regex.Match(content, key + "\\s*=\\s*\"?(-?\\d+)\"?", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int value) ? value : fallback;
        }

        private static string ReadString(string content, string key, string fallback)
        {
            Match match = Regex.Match(content, key + "\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : fallback;
        }

        private static List<int> ReadIntList(string content, string key)
        {
            var result = new List<int>();
            Match match = Regex.Match(content, key + "\\s*=\\s*\\{([^}]*)\\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string valueText;
            if (match.Success)
            {
                valueText = match.Groups[1].Value;
            }
            else
            {
                Match stringMatch = Regex.Match(content, key + "\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
                if (!stringMatch.Success)
                    return result;
                valueText = stringMatch.Groups[1].Value;
            }

            foreach (Match item in Regex.Matches(valueText, "-?\\d+"))
            {
                if (int.TryParse(item.Value, out int value))
                    result.Add(value);
            }
            return result;
        }

        private static string SanitizeRelativePath(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) || value.Contains(".."))
                return fallback;
            return value.Trim().Trim('\\', '/');
        }

        private static int NormalizeRequestRelationMode(int value)
        {
            return value >= 1 && value <= 3 ? value : 3;
        }

        private static void ResetAllowedRelationTypesFromMode(ExecutorConfig config)
        {
            config.AllowedRelationTypes.Clear();
            ushort[] relations;
            switch (config.RequestRelationMode)
            {
                case 1:
                    relations = new ushort[] { 1024, 1, 2 };
                    break;
                case 2:
                    relations = new ushort[] { 1024, 1, 2, 64, 128, 512 };
                    break;
                default:
                    relations = new ushort[] { 1024, 1, 2, 64, 128, 512, 8192 };
                    break;
            }

            foreach (ushort relation in relations)
                config.AllowedRelationTypes.Add(relation);
        }

        private static int NormalizePrenatalEducationChoice(int value)
        {
            return value >= 1 && value <= 3 ? value : 1;
        }

        private static string SanitizeFileName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                if (value.IndexOf(invalid) >= 0)
                    return fallback;
            }
            return value;
        }

        private static string NormalizeOneChar(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.Trim();
            return value.Length <= 1 ? value : value.Substring(0, 1);
        }
    }
}
