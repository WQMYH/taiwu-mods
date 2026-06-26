using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AutoMonthlyEvent.Executor.Backend
{
    internal sealed class BackendExecutorConfig
    {
        private const string ModDirectoryName = "AutoMonthlyEvent.Executor";

        public bool EnableAutoExecute { get; private set; }
        public bool EnableBackendInterceptorCategory { get; private set; }
        public bool EnableBackendItemRequestInterceptor { get; private set; }
        public bool EnableBackendGiftInterceptor { get; private set; }
        public bool EnableBackendMakeLoveInterceptor { get; private set; }
        public bool EnableActionLog { get; private set; } = true;
        public bool EnableDebugLog { get; private set; } = true;
        public string LogDirectory { get; private set; } = "Logs";
        public string ModDirectoryPath { get; private set; } = string.Empty;

        public bool EffectiveItemRequestInterceptor => EnableAutoExecute && EnableBackendInterceptorCategory && EnableBackendItemRequestInterceptor;
        public bool EffectiveGiftInterceptor => EnableAutoExecute && EnableBackendInterceptorCategory && EnableBackendGiftInterceptor;
        public bool EffectiveMakeLoveInterceptor => EnableAutoExecute && EnableBackendInterceptorCategory && EnableBackendMakeLoveInterceptor;

        public static BackendExecutorConfig Load()
        {
            var config = new BackendExecutorConfig();
            string gameRoot = ResolveGameRoot();
            config.ModDirectoryPath = Path.Combine(gameRoot, "Mod", ModDirectoryName);
            ApplyFile(config, Path.Combine(config.ModDirectoryPath, "Config.lua"));
            ApplyFile(config, Path.Combine(config.ModDirectoryPath, "Settings.Lua"));
            return config;
        }

        private static string ResolveGameRoot()
        {
            string baseDir = AppContext.BaseDirectory;
            string fromBackend = Path.GetFullPath(Path.Combine(baseDir, ".."));
            if (Directory.Exists(Path.Combine(fromBackend, "Mod")))
                return fromBackend;

            string fromCurrent = Directory.GetCurrentDirectory();
            if (Directory.Exists(Path.Combine(fromCurrent, "Mod")))
                return fromCurrent;

            return fromBackend;
        }

        private static void ApplyFile(BackendExecutorConfig config, string path)
        {
            if (!File.Exists(path))
                return;

            string content = Regex.Replace(File.ReadAllText(path), "--.*$", string.Empty, RegexOptions.Multiline);
            config.EnableAutoExecute = ReadBool(content, nameof(EnableAutoExecute), config.EnableAutoExecute);
            config.EnableBackendInterceptorCategory = ReadBool(content, nameof(EnableBackendInterceptorCategory), config.EnableBackendInterceptorCategory);
            config.EnableBackendItemRequestInterceptor = ReadBool(content, nameof(EnableBackendItemRequestInterceptor), config.EnableBackendItemRequestInterceptor);
            config.EnableBackendGiftInterceptor = ReadBool(content, nameof(EnableBackendGiftInterceptor), config.EnableBackendGiftInterceptor);
            config.EnableBackendMakeLoveInterceptor = ReadBool(content, nameof(EnableBackendMakeLoveInterceptor), config.EnableBackendMakeLoveInterceptor);
            config.EnableActionLog = ReadBool(content, nameof(EnableActionLog), config.EnableActionLog);
            config.EnableDebugLog = ReadBool(content, nameof(EnableDebugLog), config.EnableDebugLog);
            config.LogDirectory = SanitizeRelativePath(ReadString(content, nameof(LogDirectory), config.LogDirectory), "Logs");
        }

        private static bool ReadBool(string content, string key, bool fallback)
        {
            Match match = Regex.Match(content, key + "\\s*=\\s*(true|false)", RegexOptions.IgnoreCase);
            return match.Success ? string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase) : fallback;
        }

        private static string ReadString(string content, string key, string fallback)
        {
            Match match = Regex.Match(content, key + "\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : fallback;
        }

        private static string SanitizeRelativePath(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) || value.Contains(".."))
                return fallback;
            return value.Trim().Trim('\\', '/');
        }
    }
}
