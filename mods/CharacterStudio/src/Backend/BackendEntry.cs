using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Mod;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace CharacterStudio.Backend;

[PluginConfig("CharacterStudio", "WQHH", "0.1.0")]
public sealed class BackendEntry : TaiwuRemakePlugin
{
    internal static BackendEntry? Instance { get; private set; }
    internal static CharacterStudioSettings Settings { get; private set; } = new();
    private Harmony? _harmony;

    public override void Initialize()
    {
        Instance = this;
        ReloadSettings();
        _harmony = new Harmony(GetGuid());
        _harmony.PatchAll(typeof(BackendEntry).Assembly);
        DomainManager.Mod.AddModMethod(ModIdStr, "CreateCharacters", CreateCharacters);
        AdaptableLog.Info("[CharacterStudio] 后端已加载。");
    }

    public override void OnModSettingUpdate() => ReloadSettings();

    public override void Dispose()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
        Instance = null;
    }

    private void ReloadSettings()
    {
        var value = new CharacterStudioSettings();
        Read("EnableMod", ref value.EnableMod);
        Read("CustomizeProtagonist", ref value.CustomizeProtagonist);
        Read("CustomizeCloseFriend", ref value.CustomizeCloseFriend);
        Read("CloseFriendCount", ref value.CloseFriendCount);
        Read("CustomizeCreatedVillagers", ref value.CustomizeCreatedVillagers);
        Read("CustomizeInitialVillagers", ref value.CustomizeInitialVillagers);
        Read("ExpandVillageCapacity", ref value.ExpandVillageCapacity);
        Read("VillageCapacity", ref value.VillageCapacity);
        Read("AttributeMode", ref value.AttributeMode);
        Read("MainAttributeValue", ref value.MainAttributeValue);
        Read("LifeQualificationValue", ref value.LifeQualificationValue);
        Read("CombatQualificationValue", ref value.CombatQualificationValue);
        Read("BaseHealth", ref value.BaseHealth);
        Read("Morality", ref value.Morality);
        Read("FeatureIds", ref value.FeatureIds);
        Read("RemoveFeatureIds", ref value.RemoveFeatureIds);
        Read("Bisexual", ref value.Bisexual);
        Read("RelationType", ref value.RelationType);
        Read("Favorability", ref value.Favorability);
        Read("DefaultGender", ref value.DefaultGender);
        Read("DefaultAge", ref value.DefaultAge);
        Read("DefaultAttraction", ref value.DefaultAttraction);
        Read("DefaultSurname", ref value.DefaultSurname);
        Read("DefaultGivenName", ref value.DefaultGivenName);
        Read("BodyTypeChoice", ref value.BodyTypeChoice);
        Read("ClothingTemplateId", ref value.ClothingTemplateId);
        Read("EnableDebugLog", ref value.EnableDebugLog);
        value.Normalize();
        Settings = value;
        AdaptableLog.Info($"[CharacterStudio] 设置已载入：主角={value.CustomizeProtagonist}，密友={value.CustomizeCloseFriend}，村民={value.CustomizeCreatedVillagers}。");
    }

    private void Read(string key, ref bool value)
    {
        try { DomainManager.Mod.GetSetting(ModIdStr, key, ref value); } catch { }
    }

    private void Read(string key, ref int value)
    {
        try { DomainManager.Mod.GetSetting(ModIdStr, key, ref value); } catch { }
    }

    private void Read(string key, ref string value)
    {
        try { DomainManager.Mod.GetSetting(ModIdStr, key, ref value); } catch { }
    }

    private static void CreateCharacters(DataContext context, SerializableModData? data)
    {
        if (!Settings.EnableMod)
            return;

        try
        {
            CreationRequest request = CreationRequest.From(data, Settings);
            CharacterCreationService.CreateRecruitedVillagers(context, request);
        }
        catch (Exception ex)
        {
            AdaptableLog.Error("[CharacterStudio] 创建请求失败：" + ex);
        }
    }
}
