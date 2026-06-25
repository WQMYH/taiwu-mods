using System;
using System.IO;
using System.Text;
using GameData.Utilities;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal static class ActionLogger
    {
        private static ExecutorConfig? _config;
        private static string _logFilePath = string.Empty;
        private static string _humanLogFilePath = string.Empty;

        public static void Configure(ExecutorConfig config)
        {
            _config = config;
            _logFilePath = Path.Combine(config.ModDirectoryPath, config.LogDirectory, config.ActionLogFileName);
            _humanLogFilePath = Path.Combine(config.ModDirectoryPath, config.LogDirectory, config.HumanLogFileName);
        }

        public static void Log(EventDecision decision)
        {
            ExecutorConfig? config = _config;
            if (config == null || !config.EnableActionLog)
                return;

            try
            {
                string? directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(_logFilePath, decision.ToJsonLine() + Environment.NewLine, Encoding.UTF8);
                File.AppendAllText(_humanLogFilePath, decision.ToReadableLine() + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[AutoMonthlyEvent.Executor] Failed to write action log: {ex}");
            }
        }

        public static string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length + 8);
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
