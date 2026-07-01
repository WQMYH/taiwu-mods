using System;
using System.IO;
using System.Text;
using NLog;

namespace RespectTheStrongBackend;

internal static class ModLog
{
    private const long MaxLogBytes = 10 * 1024 * 1024;
    private static readonly object Sync = new();
    private static readonly Logger GameLog = LogManager.GetCurrentClassLogger();
    private static readonly string SessionId = Guid.NewGuid().ToString("N")[..8];
    private static string _filePath;

    internal static string FilePath => _filePath ??= ResolveFilePath();

    internal static void StartSession()
    {
        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                RotateIfNeeded();
                File.AppendAllText(FilePath,
                    $"{Environment.NewLine}================ SESSION {SessionId} START {DateTime.Now:O} ================{Environment.NewLine}",
                    new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                GameLog.Error(ex, "[RespectTheStrongBackend] Cannot initialize dedicated log file.");
            }
        }
        Info("Logger", $"dedicated log initialized: {FilePath}");
    }

    internal static void EndSession() =>
        Info("Logger", $"session {SessionId} completed");

    internal static void Info(string feature, string message) => Write("INFO", feature, message, null);
    internal static void Warn(string feature, string message) => Write("WARN", feature, message, null);
    internal static void Error(string feature, string message, Exception exception = null) =>
        Write("ERROR", feature, message, exception);

    internal static void Debug(string feature, string message)
    {
        if (ModSettings.EnableDebugLog)
            Write("DEBUG", feature, message, null);
    }

    private static void Write(string level, string feature, string message, Exception exception)
    {
        string line = $"{DateTime.Now:O} [{level}] [session:{SessionId}] [thread:{Environment.CurrentManagedThreadId}] [{feature}] {message}";
        if (exception != null)
            line += Environment.NewLine + exception;

        try
        {
            switch (level)
            {
                case "ERROR": GameLog.Error(exception, "[RespectTheStrongBackend] [{0}] {1}", feature, message); break;
                case "WARN": GameLog.Warn("[RespectTheStrongBackend] [{0}] {1}", feature, message); break;
                case "DEBUG": GameLog.Debug("[RespectTheStrongBackend] [{0}] {1}", feature, message); break;
                default: GameLog.Info("[RespectTheStrongBackend] [{0}] {1}", feature, message); break;
            }
        }
        catch
        {
            // Dedicated logging must never break gameplay.
        }

        lock (Sync)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, line + Environment.NewLine, new UTF8Encoding(false));
            }
            catch
            {
                // There is no safe secondary sink beyond NLog.
            }
        }
    }

    private static string ResolveFilePath()
    {
        string backend = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string gameRoot = string.Equals(Path.GetFileName(backend), "Backend", StringComparison.OrdinalIgnoreCase)
            ? Directory.GetParent(backend)?.FullName ?? backend
            : backend;
        return Path.GetFullPath(Path.Combine(gameRoot, "Logs", "RespectTheStrongBackend.log"));
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(FilePath) || new FileInfo(FilePath).Length < MaxLogBytes)
            return;
        string archive = Path.Combine(
            Path.GetDirectoryName(FilePath)!,
            $"RespectTheStrongBackend.{DateTime.Now:yyyyMMdd-HHmmss}.log");
        File.Move(FilePath, archive);
    }
}
