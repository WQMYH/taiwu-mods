using System;
using System.IO;
using FrameWork;
using GameData.Utilities;
using GongfaFramework.Test.Contracts;
using GongfaFramework.Test.Runtime;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;

namespace GongfaFramework.Test.Frontend;

[PluginConfig("GongfaFramework.test.Frontend", "WQMYH", "0.1.0.0")]
public sealed class FrontendEntry : TaiwuRemakePlugin
{
    private GameObject _host;
    internal static string ModId = "GongfaFramework.test";

    public override void Initialize()
    {
        ModId = ModIdStr;
        try
        {
            FrontendRuntime.Initialize(ModIdStr);
            _host = new GameObject("GongfaFramework.test.UI");
            UnityEngine.Object.DontDestroyOnLoad(_host);
            _host.hideFlags = HideFlags.HideAndDontSave;
            _host.AddComponent<FrameworkPanel>();
        }
        catch (Exception ex)
        {
            AdaptableLog.Error("[GongfaFramework.test] 前端初始化失败：" + ex);
            throw;
        }
    }

    public override void OnModSettingUpdate() => FrontendRuntime.ReloadSettings(ModIdStr);

    public override void Dispose()
    {
        if (_host != null) UnityEngine.Object.Destroy(_host);
        FrontendRuntime.Log("前端已卸载。");
    }
}

internal static class FrontendRuntime
{
    internal static string ModDirectory = "";
    internal static string LogPath = "";
    internal static bool EnableLogging = true;
    internal static bool EnableDefinitionLoading = true;
    internal static ParsedHotkey Hotkey = ParsedHotkey.Parse("Ctrl+F8");
    internal static KeyCode MainKey = KeyCode.F8;
    internal static GongfaSnapshot Snapshot = new GongfaSnapshot();
    internal static ValidationResult LoadResult = new ValidationResult();
    internal static string BackendHash = "";
    internal static int BackendCount;
    internal static string BackendErrors = "";
    internal static string Status = "正在初始化……";

    internal static void Initialize(string modId)
    {
        var modInfo = global::ModManager.GetModInfo(modId);
        if (modInfo == null || string.IsNullOrWhiteSpace(modInfo.DirectoryName))
            throw new InvalidOperationException("无法通过 ModManager 定位 MOD 当前目录。");
        ModDirectory = Path.GetFullPath(modInfo.DirectoryName);
        EnsureDirectories();
        ReloadSettings(modId);
        LoadResult = EnableDefinitionLoading
            ? DefinitionLoader.LoadAndApply(Path.Combine(ModDirectory, "Definitions"))
            : new ValidationResult { Success = true, Message = "外部定义加载已关闭。" };
        Snapshot = SnapshotService.Capture("Frontend");
        Status = $"已读取 {Snapshot.Records.Count} 项功法。";
        Log($"前端快照：Count={Snapshot.Records.Count}, Hash={Snapshot.Hash}; {LoadResult.Message}");
        BackendBridge.RequestSummary();
    }

    internal static void ReloadSettings(string modId)
    {
        global::ModManager.GetSetting(modId, "EnableLogging", ref EnableLogging);
        global::ModManager.GetSetting(modId, "EnableDefinitionLoading", ref EnableDefinitionLoading);
        string value = "Ctrl+F8";
        global::ModManager.GetSetting(modId, "FrameworkPanelHotkey", ref value);
        ParsedHotkey parsed = ParsedHotkey.Parse(value);
        if (parsed.Valid && Enum.TryParse(parsed.Key, true, out KeyCode key))
        {
            Hotkey = parsed;
            MainKey = key;
        }
        else
        {
            Status = $"无效快捷键“{value}”，继续使用 {Hotkey.Normalized}。";
            AdaptableLog.Warning("[GongfaFramework.test] " + Status);
        }
    }

    internal static bool IsHotkeyPressed()
    {
        if (!Input.GetKeyDown(MainKey)) return false;
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        return ctrl == Hotkey.Ctrl && alt == Hotkey.Alt && shift == Hotkey.Shift;
    }

    internal static bool HashMatches =>
        !string.IsNullOrWhiteSpace(BackendHash) && Snapshot.Hash == BackendHash &&
        Snapshot.Records.Count == BackendCount;

    internal static void EnsureDirectories()
    {
        foreach (string relative in new[]
                 {
                     "Definitions", "Definitions/user", "UserData", "UserData/drafts",
                     "UserData/imports", "UserData/exports", "UserData/backups", "UserData/logs"
                 })
            Directory.CreateDirectory(Path.Combine(ModDirectory, relative.Replace('/', Path.DirectorySeparatorChar)));
        LogPath = Path.Combine(ModDirectory, "UserData", "logs", "frontend.log");
    }

    internal static void Log(string message)
    {
        AdaptableLog.Info("[GongfaFramework.test] " + message);
        if (!EnableLogging) return;
        try { File.AppendAllText(LogPath, $"{DateTime.Now:O} [INFO] {message}{Environment.NewLine}"); }
        catch { }
    }

    internal static void Error(string message, Exception ex = null)
    {
        AdaptableLog.Error("[GongfaFramework.test] " + message + (ex == null ? "" : "\n" + ex));
        try { File.AppendAllText(LogPath, $"{DateTime.Now:O} [ERROR] {message}{Environment.NewLine}{ex}{Environment.NewLine}"); }
        catch { }
    }
}
