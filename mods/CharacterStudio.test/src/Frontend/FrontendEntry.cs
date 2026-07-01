using GameData.Domains.Mod;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;
using FrameWork;
using System.IO;
using System.Reflection;

namespace CharacterStudio.Frontend;

[PluginConfig("CharacterStudio.test", "WQHH", "0.5.0-test")]
public sealed class FrontendEntry : TaiwuRemakePlugin
{
    private Harmony? _harmony;
    private GameObject? _host;
    internal static string ModId = "CharacterStudio.test";
    internal static bool InfinitePoints;
    internal static bool ImmediateLegacyPassing;
    internal static KeyCode ImmediateLegacyPassingKey = KeyCode.T;
    internal static bool RevealPreviousIdentity;
    internal static KeyCode PanelKey = KeyCode.F9;
    internal static FrontendStudioSettings UiSettings = new();
    internal static bool LegacyMaster;

    public override void Initialize()
    {
        ModId = ModIdStr;
        LoadUiSettings();
        ReloadSettings();
        _harmony = new Harmony(GetGuid());
        _harmony.PatchAll(typeof(FrontendEntry).Assembly);
        _host = new GameObject("CharacterStudio.UI");
        Object.DontDestroyOnLoad(_host);
        _host.hideFlags = HideFlags.HideAndDontSave;
        _host.AddComponent<CharacterStudioPanel>();
        GEvent.Add(UiEvents.OnUIElementShow, LegacyIdentityInteraction.OnUiElementShow);
        ApplyPointLimits();
    }

    public override void OnModSettingUpdate()
    {
        ReloadSettings();
        ApplyPointLimits();
    }

    public override void Dispose()
    {
        _harmony?.UnpatchSelf();
        GEvent.Remove(UiEvents.OnUIElementShow, LegacyIdentityInteraction.OnUiElementShow);
        if (_host != null)
            Object.Destroy(_host);
    }

    private static void ReloadSettings()
    {
        InfinitePoints = UiSettings.EnableInfiniteCreationPoints;
        string panelKey = "F9";
        try { ModManager.GetSetting(ModId, "CharacterStudioPanelKey", ref panelKey); }
        catch { }
        if (!System.Enum.TryParse(panelKey, true, out PanelKey))
            PanelKey = KeyCode.F9;
        try { ModManager.GetSetting(ModId, "EnableLegacyFeatures", ref LegacyMaster); }
        catch { }
        ImmediateLegacyPassing = LegacyMaster && UiSettings.EnableImmediateLegacyPassing;
        string key = "T";
        try { ModManager.GetSetting(ModId, "ImmediateLegacyPassingKey", ref key); }
        catch { }
        if (!System.Enum.TryParse(key, true, out ImmediateLegacyPassingKey))
            ImmediateLegacyPassingKey = KeyCode.T;
        if (ImmediateLegacyPassing && PanelKey == ImmediateLegacyPassingKey)
            Debug.LogWarning($"[CharacterStudio] 人物工坊与立即传剑快捷键均为 {PanelKey}，请修改其中一个。");
        RevealPreviousIdentity = LegacyMaster && UiSettings.EnableRevealPreviousIdentity;
    }

    internal static void LoadUiSettings()
    {
        try
        {
            string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            if (Path.GetFileName(root).Equals("Plugins", System.StringComparison.OrdinalIgnoreCase))
                root = Directory.GetParent(root)?.FullName ?? root;
            string path = Path.Combine(root, "UserData", "studio_settings.json");
            if (File.Exists(path))
                UiSettings = JsonUtility.FromJson<FrontendStudioSettings>(File.ReadAllText(path))
                    ?? new FrontendStudioSettings();
        }
        catch { UiSettings = new FrontendStudioSettings(); }
        StudioLocalization.Load(UiSettings.Language);
    }

    internal static void ApplyUiSettings()
    {
        InfinitePoints = UiSettings.EnableInfiniteCreationPoints;
        ImmediateLegacyPassing = LegacyMaster && UiSettings.EnableImmediateLegacyPassing;
        RevealPreviousIdentity = LegacyMaster && UiSettings.EnableRevealPreviousIdentity;
        StudioLocalization.Load(UiSettings.Language);
        ApplyPointLimits();
    }

    internal static void ApplyPointLimits()
    {
        if (!InfinitePoints || GlobalConfig.Instance == null)
            return;
        GlobalConfig config = GlobalConfig.Instance;
        config.CustomProtagonistMainAttributeTotalPoint = short.MaxValue;
        config.CustomProtagonistMainAttributeMaxPoint = short.MaxValue;
        config.CustomProtagonistLifeSkillQualificationTotalPoint = short.MaxValue;
        config.CustomProtagonistLifeSkillQualificationMaxPoint = short.MaxValue;
        config.CustomProtagonistCombatSkillQualificationTotalPoint = short.MaxValue;
        config.CustomProtagonistCombatSkillQualificationMaxPoint = short.MaxValue;
        config.CustomProtagonistCharacterFeatureTotalPoint = short.MaxValue;
    }
}

[HarmonyPatch(typeof(GameApp), "Update")]
internal static class GameUpdatePatch
{
    private static void Postfix()
    {
        CharacterStudioPanel.PollHotkey(FrontendEntry.PanelKey);
        if (FrontendEntry.ImmediateLegacyPassing &&
            Input.GetKeyDown(FrontendEntry.ImmediateLegacyPassingKey))
            CharacterStudioPanel.RequestLegacyPassingConfirmation();
    }
}
