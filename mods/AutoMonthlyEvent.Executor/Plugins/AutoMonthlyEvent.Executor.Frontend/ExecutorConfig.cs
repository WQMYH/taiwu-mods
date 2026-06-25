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
        public bool AutoContinueWhitelistedResults { get; private set; } = true;
        public string RequestDirection { get; private set; } = "NpcToTaiwu";
        public int FallbackFavorabilityThreshold { get; private set; } = 15000;
        public bool EnableResourceRequest { get; private set; } = true;
        public bool EnableTeaWineItemRequest { get; private set; } = true;
        public bool EnableRequestResultContinue { get; private set; } = true;
        public bool EnableGuidanceResultContinue { get; private set; } = true;
        public bool EnableAdoptAbandonedBaby { get; private set; } = true;
        public int AdoptionMaxChildAge { get; private set; } = 3;
        public bool EnableActionLog { get; private set; } = true;
        public string LogDirectory { get; private set; } = "Logs";
        public string ActionLogFileName { get; private set; } = "executor_actions.jsonl";
        public string HumanLogFileName { get; private set; } = "executor_actions.log";
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

                AdaptableLog.Info($"[AutoMonthlyEvent.Executor] Config loaded. EnableAutoExecute={config.EnableAutoExecute}, DryRun={config.DryRun}, EnableActionLog={config.EnableActionLog}, LogDirectory={config.LogDirectory}");
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
            config.AutoContinueWhitelistedResults = ReadBool(content, nameof(AutoContinueWhitelistedResults), config.AutoContinueWhitelistedResults);
            config.RequestDirection = ReadString(content, nameof(RequestDirection), config.RequestDirection);
            config.FallbackFavorabilityThreshold = ReadInt(content, nameof(FallbackFavorabilityThreshold), config.FallbackFavorabilityThreshold);
            config.EnableResourceRequest = ReadBool(content, nameof(EnableResourceRequest), config.EnableResourceRequest);
            config.EnableTeaWineItemRequest = ReadBool(content, nameof(EnableTeaWineItemRequest), config.EnableTeaWineItemRequest);
            config.EnableRequestResultContinue = ReadBool(content, nameof(EnableRequestResultContinue), config.EnableRequestResultContinue);
            config.EnableGuidanceResultContinue = ReadBool(content, nameof(EnableGuidanceResultContinue), config.EnableGuidanceResultContinue);
            config.EnableAdoptAbandonedBaby = ReadBool(content, nameof(EnableAdoptAbandonedBaby), config.EnableAdoptAbandonedBaby);
            config.AdoptionMaxChildAge = ReadInt(content, nameof(AdoptionMaxChildAge), config.AdoptionMaxChildAge);
            config.EnableActionLog = ReadBool(content, nameof(EnableActionLog), config.EnableActionLog);
            config.LogDirectory = SanitizeRelativePath(ReadString(content, nameof(LogDirectory), config.LogDirectory), "Logs");
            config.ActionLogFileName = SanitizeFileName(ReadString(content, nameof(ActionLogFileName), config.ActionLogFileName), "executor_actions.jsonl");
            config.HumanLogFileName = SanitizeFileName(ReadString(content, nameof(HumanLogFileName), config.HumanLogFileName), "executor_actions.log");

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
    }
}
