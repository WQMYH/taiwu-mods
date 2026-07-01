using System;
using System.IO;
using System.Linq;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Mod;
using GongfaFramework.Test.Contracts;
using GongfaFramework.Test.Runtime;
using NLog;
using TaiwuModdingLib.Core.Plugin;

namespace GongfaFramework.Test.Backend;

[PluginConfig("GongfaFramework.test.Backend", "WQMYH", "0.1.0.0")]
public sealed class BackendEntry : TaiwuRemakePlugin
{
    private static readonly Logger GameLog = LogManager.GetCurrentClassLogger();
    private static string _modDirectory = "";
    private static string _logPath = "";
    private static bool _enableLogging = true;
    private static GongfaSnapshot _snapshot = new GongfaSnapshot();
    private static ValidationResult _loadResult = new ValidationResult();

    public override void Initialize()
    {
        try
        {
            _modDirectory = Path.GetFullPath(DomainManager.Mod.GetModDirectory(ModIdStr));
            DomainManager.Mod.GetSetting(ModIdStr, "EnableLogging", ref _enableLogging);
            EnsureDirectories();
            Log("后端初始化。ModDirectory=" + _modDirectory);

            bool enableDefinitions = true;
            DomainManager.Mod.GetSetting(ModIdStr, "EnableDefinitionLoading", ref enableDefinitions);
            _loadResult = enableDefinitions
                ? DefinitionLoader.LoadAndApply(Path.Combine(_modDirectory, "Definitions"))
                : new ValidationResult { Success = true, Message = "外部定义加载已关闭。" };
            Log(_loadResult.Message);
            foreach (string error in _loadResult.Errors) Error(error);

            _snapshot = SnapshotService.Capture("Backend");
            RegisterMethods();
            Log($"后端快照完成：Count={_snapshot.Records.Count}, Hash={_snapshot.Hash}");
        }
        catch (Exception ex)
        {
            Error("后端初始化失败：" + ex);
            throw;
        }
    }

    public override void Dispose() => Log("后端已卸载。");

    private void RegisterMethods()
    {
        DomainManager.Mod.AddModMethod(ModIdStr, "GetSnapshotSummary",
            (Func<DataContext, SerializableModData, SerializableModData>)GetSnapshotSummary);
        DomainManager.Mod.AddModMethod(ModIdStr, "GetSnapshotPage",
            (Func<DataContext, SerializableModData, SerializableModData>)GetSnapshotPage);
        DomainManager.Mod.AddModMethod(ModIdStr, "ValidateDefinition",
            (Func<DataContext, SerializableModData, SerializableModData>)ValidateDefinition);
    }

    private static SerializableModData GetSnapshotSummary(DataContext context, SerializableModData request)
    {
        var result = NewResult(true, "后端快照已返回。");
        result.Set("Hash", _snapshot.Hash);
        result.Set("Count", _snapshot.Records.Count);
        result.Set("Errors", string.Join("\n", _snapshot.Errors.Concat(_loadResult.Errors)));
        result.Set("LoadMessage", _loadResult.Message ?? "");
        return result;
    }

    private static SerializableModData GetSnapshotPage(DataContext context, SerializableModData request)
    {
        int start = 0;
        int count = 50;
        request?.Get("Start", out start);
        request?.Get("Count", out count);
        start = Math.Max(0, start);
        count = Math.Clamp(count, 1, 100);
        GongfaRecord[] records = _snapshot.Records.Skip(start).Take(count).ToArray();
        var result = NewResult(true, $"返回 {records.Length} 项。");
        result.Set("Json", FrameworkJson.Serialize(records));
        result.Set("Total", _snapshot.Records.Count);
        return result;
    }

    private static SerializableModData ValidateDefinition(DataContext context, SerializableModData request)
    {
        string json = "";
        string extension = ".json";
        request?.Get("Json", out json);
        request?.Get("Extension", out extension);
        ValidationResult validation = DefinitionLoader.ValidateText(json ?? "", extension ?? ".json");
        var result = NewResult(validation.Success, validation.Message);
        result.Set("Json", FrameworkJson.Serialize(validation));
        return result;
    }

    private static SerializableModData NewResult(bool success, string message)
    {
        var result = new SerializableModData();
        result.Set("Success", success);
        result.Set("Message", message ?? "");
        return result;
    }

    private static void EnsureDirectories()
    {
        foreach (string relative in new[]
                 {
                     "Definitions", "UserData", "UserData/drafts", "UserData/imports",
                     "UserData/exports", "UserData/backups", "UserData/logs"
                 })
            Directory.CreateDirectory(Path.Combine(_modDirectory, relative.Replace('/', Path.DirectorySeparatorChar)));
        _logPath = Path.Combine(_modDirectory, "UserData", "logs", "backend.log");
    }

    private static void Log(string message)
    {
        GameLog.Info("[GongfaFramework.test] " + message);
        if (_enableLogging) Append("INFO", message);
    }

    private static void Error(string message)
    {
        GameLog.Error("[GongfaFramework.test] " + message);
        Append("ERROR", message);
    }

    private static void Append(string level, string message)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_logPath))
                File.AppendAllText(_logPath, $"{DateTime.Now:O} [{level}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}
