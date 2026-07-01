using System;
using System.Collections.Generic;
using System.Linq;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Building;
using GameData.Domains.Character;
using GameData.Domains.Global;
using GameData.Domains.Map;
using GameData.Domains.Organization;
using GameData.Domains.World;
using HarmonyLib;

namespace CharacterStudio.Backend;

[HarmonyPatch(typeof(CharacterDomain), "CreateCloseFriend")]
internal static class CloseFriendCreatedPatch
{
    [ThreadStatic]
    private static bool _creatingBatch;
    [ThreadStatic]
    private static List<int>? _batchIds;
    [ThreadStatic]
    private static int _nameIndex;
    [ThreadStatic]
    private static int _nameOffset;

    private static bool Prefix(
        CharacterDomain __instance,
        DataContext context,
        ref short charTemplateId,
        short morality,
        Character protagonistChar,
        ref Character __result,
        out bool __state)
    {
        __state = false;
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (!_creatingBatch && settings.EnableCustomCloseFriends && settings.CloseFriendGender is 1 or 2)
        {
            sbyte state = DomainManager.Map.GetStateTemplateIdByAreaId(
                DomainManager.Taiwu.GetTaiwuVillageLocation().AreaId);
            charTemplateId = MapDomain.GetCharacterTemplateId(state, (sbyte)(settings.CloseFriendGender - 1));
        }
        if (_creatingBatch || !settings.EnableCustomCloseFriends || settings.CloseFriendCount <= 1)
            return true;

        __state = true;
        _creatingBatch = true;
        _batchIds = new List<int>();
        _nameIndex = 0;
        _nameOffset = context.Random.Next(1024);
        Character? last = null;
        try
        {
            for (int i = 0; i < settings.CloseFriendCount; i++)
                last = __instance.CreateCloseFriend(context, charTemplateId, morality, protagonistChar);
        }
        finally
        {
            _creatingBatch = false;
        }
        CharacterBatchService.ApplyRelations(context, _batchIds,
            settings.CloseFriendBatchRelationType, settings.CloseFriendBatchFavorability);
        _batchIds = null;
        __result = last!;
        return false;
    }

    private static void Postfix(DataContext context, Character __result, bool __state)
    {
        if (__state || __result == null || !BackendEntry.Settings.EnableCustomCloseFriends)
            return;
        CharacterStudioSettings settings = BackendEntry.Settings;
        CharacterProfile friendProfile = CharacterProfileRepository.Resolve(
            CreationSource.ManualCreate, settings.CloseFriendProfile);
        VillagerProfileService.Apply(__result, context, friendProfile, CreationSource.ManualCreate);
        CloseFriendSyncService.Apply(context, __result);
        int age = settings.CloseFriendAgeMin == settings.CloseFriendAgeMax
            ? settings.CloseFriendAgeMin
            : context.Random.Next(settings.CloseFriendAgeMin, settings.CloseFriendAgeMax + 1);
        __result.SetCurrAge((short)age, context);
        ApplyCustomName(context, __result, settings);
        CharacterBatchService.ApplyRelationToTaiwu(
            context, __result.GetId(), settings.CloseFriendRelationType, settings.CloseFriendFavorability);
        if (settings.EnableCloseFriendReincarnation)
            ReincarnationService.Inject(context, __result, settings.CloseFriendReincarnationSource,
                settings.CloseFriendReincarnationCount, settings.CloseFriendReincarnationProfile);
        _batchIds?.Add(__result.GetId());
    }

    private static void ApplyCustomName(DataContext context, Character character, CharacterStudioSettings settings)
    {
        string[] surnames = Split(settings.CloseFriendSurnames);
        string[] names = Split(settings.CloseFriendGivenNames);
        if (surnames.Length == 0 && names.Length == 0) return;
        int poolLength = Math.Max(1, Math.Max(surnames.Length, names.Length));
        int index = settings.CloseFriendNameMode switch
        {
            1 => (_nameOffset + _nameIndex++) % poolLength,
            2 => _nameIndex++,
            _ => context.Random.Next(poolLength)
        };
        string surname = surnames.Length == 0 ? "" : surnames[index % surnames.Length];
        string given = names.Length == 0 ? "" : names[index % names.Length];
        character.SetFullName(NameService.CreateName(context, character.GetGender(), surname, given), context);
    }

    private static string[] Split(string text) => (text ?? "").Split(
        new[] { ',', ';', '，', '；', '\n' }, StringSplitOptions.RemoveEmptyEntries);
}

[HarmonyPatch(typeof(CharacterDomain), "CreateProtagonist")]
internal static class ProtagonistReincarnationPatch
{
    private static void Postfix(DataContext context)
    {
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (!settings.EnableTaiwuReincarnation) return;
        Character? taiwu = DomainManager.Taiwu.GetTaiwu();
        if (taiwu != null)
            ReincarnationService.Inject(context, taiwu, settings.TaiwuReincarnationSource,
                settings.TaiwuReincarnationCount, settings.TaiwuReincarnationProfile);
    }
}

[HarmonyPatch(typeof(BuildingDomain), "CreateCharacterByRecruitCharacterData")]
internal static class RecruitedCharacterCreatedPatch
{
    private static void Postfix(DataContext context, int __result)
    {
        if (!BackendEntry.Settings.EnableCustomVillagers || __result <= 0)
            return;
        CreationSource source = CreationSourceContext.Source;
        if (source == CreationSource.ManualCreate && !BackendEntry.Settings.EnableManualCreate)
            return;
        if (source == CreationSource.RecruitCreated && !BackendEntry.Settings.ProcessRecruitCreated)
            return;
        VillagerProcessingState.ApplyCharacter(
            __result, context, source, CreationSourceContext.ProfileId);
    }
}

[HarmonyPatch(typeof(OrganizationDomain), "CreateCoreCharacter")]
internal static class InitialVillageCreatedPatch
{
    private static void Prefix(SettlementMembersCreationInfo info, out HashSet<int> __state)
    {
        __state = info.OrgTemplateId == 16
            ? VillagerProcessingState.GetCurrentVillagerIds().ToHashSet()
            : new HashSet<int>();
    }

    private static void Postfix(DataContext context, SettlementMembersCreationInfo info, HashSet<int> __state)
    {
        if (!BackendEntry.Settings.EnableCustomVillagers ||
            !BackendEntry.Settings.ProcessInitialVillage ||
            info.OrgTemplateId != 16)
            return;
        foreach (int charId in VillagerProcessingState.GetCurrentVillagerIds())
            if (!__state.Contains(charId))
                VillagerProcessingState.ApplyCharacter(
                    charId, context, CreationSource.InitialVillage);
    }
}

[HarmonyPatch(typeof(GlobalDomain), nameof(GlobalDomain.OnCurrWorldArchiveDataReady))]
internal static class CharacterStudioCompleteLoadingPatch
{
    private static void Postfix()
    {
        if (BackendEntry.Settings.EnableCustomVillagers)
            VillagerProcessingState.CaptureKnownVillagers();
    }
}

[HarmonyPatch(typeof(WorldDomain), "AdvanceMonth_DisplayedMonthlyNotifications")]
internal static class CharacterStudioMonthPatch
{
    private static void Postfix() => VillagerProcessingState.ProcessMonth();
}
