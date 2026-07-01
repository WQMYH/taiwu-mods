using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GameData.Utilities;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal sealed class MonthlyAutomationSettings
    {
        public bool Enabled = true;
        public int RelationMode = 2;
        public int FavorabilityThreshold = 25000;
        public int AdoptionMode = 2; // 0=拒绝，1=收养，2=按原有年龄与立场筛选
        public int AdoptionMaxAge = 3;
        public int GuidanceChoice = 1;
        public int PregnancyChoice = 1;
        public int PrenatalChoice = 3;
        public int BenevolenceChoice = 1;
        public int RecruitChoice = 1;
        public string GenerationCharacter = string.Empty;
        public string GivenNameCharacter = string.Empty;
        public bool EnableRequests = true;
        public bool EnableFamily = true;
        public bool EnableSocial = true;
        public bool EnableResultSkip = true;

        private static readonly object SyncRoot = new object();
        private static MonthlyAutomationSettings _current = new MonthlyAutomationSettings();
        private static string _filePath = string.Empty;

        public static MonthlyAutomationSettings Current => _current;
        public static string FilePath => _filePath;

        public static void Initialize(string modDirectory)
        {
            _filePath = Path.Combine(modDirectory, "UserData", "settings.json");
            Reload();
        }

        public static void Reload()
        {
            lock (SyncRoot)
            {
                var value = new MonthlyAutomationSettings();
                try
                {
                    if (File.Exists(_filePath))
                    {
                        string json = File.ReadAllText(_filePath, Encoding.UTF8);
                        value.Enabled = ReadBool(json, nameof(Enabled), value.Enabled);
                        value.RelationMode = Clamp(ReadInt(json, nameof(RelationMode), value.RelationMode), 1, 3);
                        value.FavorabilityThreshold = Clamp(ReadInt(json, nameof(FavorabilityThreshold), value.FavorabilityThreshold), 0, 60000);
                        value.AdoptionMode = Clamp(ReadInt(json, nameof(AdoptionMode), value.AdoptionMode), 0, 2);
                        value.AdoptionMaxAge = Clamp(ReadInt(json, nameof(AdoptionMaxAge), value.AdoptionMaxAge), 0, 18);
                        value.GuidanceChoice = Positive(ReadInt(json, nameof(GuidanceChoice), value.GuidanceChoice));
                        value.PregnancyChoice = Positive(ReadInt(json, nameof(PregnancyChoice), value.PregnancyChoice));
                        value.PrenatalChoice = Positive(ReadInt(json, nameof(PrenatalChoice), value.PrenatalChoice));
                        value.BenevolenceChoice = Positive(ReadInt(json, nameof(BenevolenceChoice), value.BenevolenceChoice));
                        value.RecruitChoice = Positive(ReadInt(json, nameof(RecruitChoice), value.RecruitChoice));
                        value.GenerationCharacter = OneChar(ReadString(json, nameof(GenerationCharacter), value.GenerationCharacter));
                        value.GivenNameCharacter = OneChar(ReadString(json, nameof(GivenNameCharacter), value.GivenNameCharacter));
                        value.EnableRequests = ReadBool(json, nameof(EnableRequests), value.EnableRequests);
                        value.EnableFamily = ReadBool(json, nameof(EnableFamily), value.EnableFamily);
                        value.EnableSocial = ReadBool(json, nameof(EnableSocial), value.EnableSocial);
                        value.EnableResultSkip = ReadBool(json, nameof(EnableResultSkip), value.EnableResultSkip);
                    }
                    _current = value;
                    Save();
                    ActionLogger.Debug("monthly-settings-loaded", string.Empty, "settings", $"path={_filePath}; enabled={value.Enabled}");
                }
                catch (Exception ex)
                {
                    _current = value;
                    AdaptableLog.Warning("[AutoMonthlyEvent.Executor] 月度自动化设置读取失败，使用安全默认值：" + ex);
                }
            }
        }

        public static void Save()
        {
            lock (SyncRoot)
            {
                if (string.IsNullOrEmpty(_filePath))
                    return;
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                string temp = _filePath + ".tmp";
                string backup = _filePath + ".bak";
                File.WriteAllText(temp, ToJson(_current), new UTF8Encoding(false));
                if (File.Exists(_filePath))
                    File.Copy(_filePath, backup, true);
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
                File.Move(temp, _filePath);
            }
        }

        private static string ToJson(MonthlyAutomationSettings s)
        {
            return "{\n"
                + $"  \"Version\": 1,\n  \"Enabled\": {JsonBool(s.Enabled)},\n"
                + $"  \"EnableRequests\": {JsonBool(s.EnableRequests)},\n  \"EnableFamily\": {JsonBool(s.EnableFamily)},\n"
                + $"  \"EnableSocial\": {JsonBool(s.EnableSocial)},\n  \"EnableResultSkip\": {JsonBool(s.EnableResultSkip)},\n"
                + $"  \"RelationMode\": {s.RelationMode},\n  \"FavorabilityThreshold\": {s.FavorabilityThreshold},\n"
                + $"  \"AdoptionMode\": {s.AdoptionMode},\n  \"AdoptionMaxAge\": {s.AdoptionMaxAge},\n"
                + $"  \"GuidanceChoice\": {s.GuidanceChoice},\n  \"PregnancyChoice\": {s.PregnancyChoice},\n"
                + $"  \"PrenatalChoice\": {s.PrenatalChoice},\n  \"BenevolenceChoice\": {s.BenevolenceChoice},\n"
                + $"  \"RecruitChoice\": {s.RecruitChoice},\n"
                + $"  \"GenerationCharacter\": \"{Escape(s.GenerationCharacter)}\",\n"
                + $"  \"GivenNameCharacter\": \"{Escape(s.GivenNameCharacter)}\"\n}}\n";
        }

        private static bool ReadBool(string json, string key, bool fallback)
        {
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return m.Success ? string.Equals(m.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase) : fallback;
        }

        private static int ReadInt(string json, string key, int fallback)
        {
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
            return m.Success && int.TryParse(m.Groups[1].Value, out int value) ? value : fallback;
        }

        private static string ReadString(string json, string key, string fallback)
        {
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") : fallback;
        }

        private static string OneChar(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Substring(0, 1);
        private static int Positive(int value) => value < 1 ? 1 : value;
        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        private static string JsonBool(bool value) => value ? "true" : "false";
        private static string Escape(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
