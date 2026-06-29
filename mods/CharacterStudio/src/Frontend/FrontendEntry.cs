using GameData.Domains.Mod;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;

namespace CharacterStudio.Frontend;

[PluginConfig("CharacterStudio", "WQHH", "0.1.0")]
public sealed class FrontendEntry : TaiwuRemakePlugin
{
    private Harmony? _harmony;
    private GameObject? _host;
    internal static string ModId = "CharacterStudio";
    internal static bool InfinitePoints;

    public override void Initialize()
    {
        ModId = ModIdStr;
        ReloadSettings();
        _harmony = new Harmony(GetGuid());
        _harmony.PatchAll(typeof(FrontendEntry).Assembly);
        _host = new GameObject("CharacterStudio.UI");
        Object.DontDestroyOnLoad(_host);
        _host.hideFlags = HideFlags.HideAndDontSave;
        _host.AddComponent<CharacterStudioPanel>();
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
        if (_host != null)
            Object.Destroy(_host);
    }

    private static void ReloadSettings()
    {
        try { ModManager.GetSetting(ModId, "InfiniteCreationPoints", ref InfinitePoints); }
        catch { }
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
    private static void Postfix() => CharacterStudioPanel.PollHotkey();
}
