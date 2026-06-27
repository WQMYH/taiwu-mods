using System;
using System.IO;
using System.Text;
using NLog;

namespace AutoMonthlyEvent.Executor.Backend
{
    internal static class BackendActionLogger
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static BackendExecutorConfig? _config;
        private static string _logFilePath = string.Empty;

        public static void Configure(BackendExecutorConfig config)
        {
            _config = config;
            _logFilePath = string.IsNullOrWhiteSpace(config.ModDirectoryPath)
                ? string.Empty
                : Path.Combine(config.ModDirectoryPath, config.LogDirectory, "backend_interceptor.log");
        }

        public static void Log(string eventType, string decision, string reason, int selfCharId = -1, int targetCharId = -1, Exception? exception = null)
        {
            BackendExecutorConfig? config = _config;
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 类型={eventType} 决策={decision} self={selfCharId} target={targetCharId} 原因={reason}";
            if (exception != null)
                line += " 异常=" + exception.GetType().Name + ": " + exception.Message;

            Logger.Info("[AutoMonthlyEvent.Executor] " + line);
            if (config == null || !config.EnableActionLog || string.IsNullOrEmpty(_logFilePath))
                return;

            try
            {
                string? directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "[AutoMonthlyEvent.Executor] Failed to write backend action log.");
            }
        }
    }
}
