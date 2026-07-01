using System;
using System.IO;
using System.Reflection;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Mod;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace CharacterStudio.Backend;

[PluginConfig("CharacterStudio.test", "WQHH", "0.5.0-test")]
public sealed class BackendEntry : TaiwuRemakePlugin
{
    internal static BackendEntry? Instance { get; private set; }
    internal static CharacterStudioSettings Settings { get; private set; } = new();
    private Harmony? _harmony;

    public override void Initialize()
    {
        Instance = this;
        CharacterStudioPaths.Initialize(Assembly.GetExecutingAssembly().Location);
        ReloadSettings();
        CharacterProfileRepository.Initialize();
        VillagerProcessingState.Initialize();
        _harmony = new Harmony(GetGuid());
        _harmony.PatchAll(typeof(BackendEntry).Assembly);
        DomainManager.Mod.AddModMethod(ModIdStr, "CreateCharacters", CreateCharacters);
        DomainManager.Mod.AddModMethod(ModIdStr, "RequestLegacyPassing",
            (Action<DataContext, SerializableModData>)RequestLegacyPassing);
        DomainManager.Mod.AddModMethod(ModIdStr, "RevealPreviousIdentity",
            (Action<DataContext, SerializableModData>)RevealPreviousIdentity);
        DomainManager.Mod.AddModMethod(ModIdStr, "SaveCharacterProfile",
            (Action<DataContext, SerializableModData>)SaveCharacterProfile);
        DomainManager.Mod.AddModMethod(ModIdStr, "DeleteCharacterProfile",
            (Action<DataContext, SerializableModData>)DeleteCharacterProfile);
        DomainManager.Mod.AddModMethod(ModIdStr, "ReloadCharacterProfiles",
            (Action<DataContext, SerializableModData>)ReloadCharacterProfiles);
        DomainManager.Mod.AddModMethod(ModIdStr, "SaveStudioSettings",
            (Action<DataContext, SerializableModData>)SaveStudioSettings);
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
        CharacterStudioSettings value = CharacterStudioPaths.IsInitialized
            ? StudioSettingsStore.Load()
            : new CharacterStudioSettings();
        Read("EnableCustomVillagers", ref value.EnableCustomVillagers);
        Read("EnableCustomCloseFriends", ref value.EnableCustomCloseFriends);
        Read("EnableLegacyFeatures", ref value.EnableLegacyFeatures);
        Read("CharacterStudioPanelKey", ref value.CharacterStudioPanelKey);
        Read("EnableDebugLog", ref value.EnableDebugLog);
        Read("LogValueSnapshots", ref value.LogValueSnapshots);
        Read("ImmediateLegacyPassingKey", ref value.ImmediateLegacyPassingKey);
        value.Normalize();
        ApplyUiSettings(value);
        if (CharacterStudioPaths.IsInitialized && value.ReloadProfilesOnSettingUpdate)
            CharacterProfileRepository.Reload();
        AdaptableLog.Info($"[CharacterStudio] 设置已载入：谷密同步={value.SyncCloseFriend}，初始村民={value.ProcessInitialVillage}，过月现有村民={value.ProcessExistingVillagers}。");
    }

    internal static void ApplyUiSettings(CharacterStudioSettings value)
    {
        value.Normalize();
        Settings = value;
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
        if (!Settings.EnableCustomVillagers || !Settings.EnableManualCreate)
            return;

        try
        {
            CreationRequest request = CreationRequest.From(data, Settings);
            CharacterCreationService.CreateRecruitedVillagers(context, request);
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning("[CharacterStudio] 创建请求失败：" + ex);
        }
    }

    private static void RequestLegacyPassing(DataContext context, SerializableModData data) =>
        LogOperationResult("即刻传剑", LegacyTransferService.RequestLegacyPassing(context, data));

    private static void RevealPreviousIdentity(DataContext context, SerializableModData data) =>
        LogOperationResult("袒露身份", LegacyTransferService.RevealPreviousIdentity(context, data));

    private static void SaveCharacterProfile(DataContext context, SerializableModData data) =>
        LogOperationResult("保存人物模板", CharacterProfileRepository.SaveUserProfile(data));

    private static void DeleteCharacterProfile(DataContext context, SerializableModData data) =>
        LogOperationResult("删除人物模板", CharacterProfileRepository.DeleteUserProfile(data));

    private static void ReloadCharacterProfiles(DataContext context, SerializableModData data)
    {
        CharacterProfileRepository.Reload();
        AdaptableLog.Info("[CharacterStudio] 人物模板已重新载入。");
    }

    private static void SaveStudioSettings(DataContext context, SerializableModData data) =>
        LogOperationResult("保存界面设置", StudioSettingsStore.Save(data));

    private static void LogOperationResult(string operation, SerializableModData result)
    {
        bool success = false;
        string message = "";
        result.Get("Success", out success);
        result.Get("Message", out message);
        if (success)
            AdaptableLog.Info($"[CharacterStudio] {operation}：{message}");
        else
            AdaptableLog.Warning($"[CharacterStudio] {operation}被拒绝：{message}");
    }
}
