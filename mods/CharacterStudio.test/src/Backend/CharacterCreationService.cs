using System;
using System.Collections.Generic;
using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Building;
using GameData.Domains.Character;
using GameData.Domains.Character.AvatarSystem;
using GameData.Domains.Map;
using GameData.Domains.Organization;
using GameData.Utilities;
using CharacterEntity = GameData.Domains.Character.Character;
using CharacterConfig = Config.Character;

namespace CharacterStudio.Backend;

internal static class CharacterCreationService
{
    internal static List<int> CreateRecruitedVillagers(DataContext context, CreationRequest request)
    {
        if (!TryGetRecruitAnchor(out BuildingBlockKey key, out BuildingBlockData block))
        {
            AdaptableLog.Warning("[CharacterStudio] 太吾村没有可用建筑，不能创建村民。");
            return new List<int>();
        }

        EnsureCapacity(context, request.Count);
        CharacterProfile profile = CharacterProfileRepository.Resolve(
            CreationSource.ManualCreate, request.ProfileId);
        var createdIds = new List<int>();
        for (int i = 0; i < request.Count; i++)
        {
            sbyte gender = ResolveGender(context, profile, request.GenderOverride);
            RecruitCharacterData recruit = CharacterEntity.GenerateRecruitCharacterData(context.Random, 0, key, block);
            PrepareRecruitData(context, recruit, gender, request, profile);
            int charId;
            using (CreationSourceContext.Push(CreationSource.ManualCreate, profile.Id))
                charId = DomainManager.Building.CreateCharacterByRecruitCharacterData(context, recruit);
            if (charId <= 0)
            {
                AdaptableLog.Warning($"[CharacterStudio] 第 {i + 1} 名人物创建失败。");
                break;
            }

            createdIds.Add(charId);
        }

        CharacterStudioSettings settings = BackendEntry.Settings;
        CharacterBatchService.ApplyRelations(
            context, createdIds, settings.VillagerBatchRelationType, settings.VillagerBatchFavorability);
        if (settings.EnableVillagerReincarnation)
            foreach (int id in createdIds)
                if (DomainManager.Character.TryGetElement_Objects(id, out CharacterEntity? character) && character != null)
                    ReincarnationService.Inject(context, character, settings.VillagerReincarnationSource,
                        settings.VillagerReincarnationCount, settings.VillagerReincarnationProfile);
        AdaptableLog.Info($"[CharacterStudio] 已创建 {createdIds.Count}/{request.Count} 名村民。");
        return createdIds;
    }

    private static void PrepareRecruitData(
        DataContext context,
        RecruitCharacterData recruit,
        sbyte gender,
        CreationRequest request,
        CharacterProfile profile)
    {
        Location village = DomainManager.Taiwu.GetTaiwuVillageLocation();
        sbyte stateId = DomainManager.Map.GetStateTemplateIdByAreaId(village.AreaId);
        short templateId = profile.OriginalTemplateMode == OriginalTemplateMode.Explicit &&
                           profile.ExplicitTemplateId >= 0
            ? profile.ExplicitTemplateId
            : OrganizationDomain.GetCharacterTemplateId(
                MapState.Instance[stateId].SectID, stateId, gender);
        CharacterItem template = CharacterConfig.Instance[templateId];
        int age = request.AgeOverride >= 0
            ? request.AgeOverride
            : context.Random.Next(profile.AgeMin, profile.AgeMax + 1);
        int attraction = request.AttractionOverride >= 0
            ? request.AttractionOverride
            : context.Random.Next(profile.AttractionMin, profile.AttractionMax + 1);

        recruit.Gender = gender;
        recruit.Transgender = false;
        recruit.Age = (short)age;
        recruit.BaseAttraction = (short)attraction;
        recruit.TemplateId = templateId;
        recruit.AvatarData = AvatarManager.Instance.GetRandomAvatar(
            context.Random,
            gender,
            false,
            profile.BodyType >= 0 ? (sbyte)profile.BodyType : template.PresetBodyType,
            (short)attraction);
        recruit.FullName = NameService.CreateName(context, gender, request.Surname, request.GivenName);
        recruit.FinalAttraction = recruit.AvatarData.GetCharm(recruit.BaseAttraction, 0);
        recruit.Recalculate();
    }

    private static sbyte ResolveGender(
        DataContext context,
        CharacterProfile profile,
        int overrideValue)
    {
        if (overrideValue is 0 or 1)
            return (sbyte)overrideValue;
        return profile.Gender switch
        {
            CharacterGenderMode.Female => 0,
            CharacterGenderMode.Male => 1,
            _ => (sbyte)context.Random.Next(2)
        };
    }

    private static bool TryGetRecruitAnchor(
        out BuildingBlockKey key,
        out BuildingBlockData block)
    {
        key = default;
        block = null!;
        List<Location> areas = DomainManager.Building.GetTaiwuBuildingAreas();
        if (areas == null)
            return false;
        foreach (Location area in areas)
        {
            foreach (BuildingBlockData candidate in DomainManager.Building.GetBuildingBlocksAtLocation(area, _ => true))
            {
                key = new BuildingBlockKey(area.AreaId, candidate.RootBlockIndex, candidate.BlockIndex);
                block = candidate;
                return true;
            }
        }
        return false;
    }

    private static void EnsureCapacity(DataContext context, int additional)
    {
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (!settings.ExpandVillageCapacity)
            return;
        int current = DomainManager.Taiwu.GetBuildingSpaceLimit();
        int population = DomainManager.Taiwu.GetTotalVillagerCount();
        int target = Math.Max(settings.VillageCapacity, population + additional);
        if (target > current)
            DomainManager.Taiwu.AddBuildingSpaceExtra(context, target - current);
    }
}
