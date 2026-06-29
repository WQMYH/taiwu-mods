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
    internal static int CreateRecruitedVillagers(DataContext context, CreationRequest request)
    {
        if (!TryGetRecruitAnchor(out BuildingBlockKey key, out BuildingBlockData block))
        {
            AdaptableLog.Error("[CharacterStudio] 太吾村没有可用建筑，不能创建村民。");
            return 0;
        }

        EnsureCapacity(context, request.Count);
        int created = 0;
        for (int i = 0; i < request.Count; i++)
        {
            sbyte gender = request.Gender is 0 or 1
                ? (sbyte)request.Gender
                : (sbyte)context.Random.Next(2);
            RecruitCharacterData recruit = CharacterEntity.GenerateRecruitCharacterData(context.Random, 0, key, block);
            PrepareRecruitData(context, recruit, gender, request);
            int charId = DomainManager.Building.CreateCharacterByRecruitCharacterData(context, recruit);
            if (charId <= 0)
            {
                AdaptableLog.Error($"[CharacterStudio] 第 {i + 1} 名人物创建失败。");
                break;
            }

            created++;
        }

        AdaptableLog.Info($"[CharacterStudio] 已创建 {created}/{request.Count} 名村民。");
        return created;
    }

    private static void PrepareRecruitData(
        DataContext context,
        RecruitCharacterData recruit,
        sbyte gender,
        CreationRequest request)
    {
        Location village = DomainManager.Taiwu.GetTaiwuVillageLocation();
        sbyte stateId = DomainManager.Map.GetStateTemplateIdByAreaId(village.AreaId);
        short templateId = OrganizationDomain.GetCharacterTemplateId(
            MapState.Instance[stateId].SectID, stateId, gender);
        CharacterItem template = CharacterConfig.Instance[templateId];

        recruit.Gender = gender;
        recruit.Transgender = false;
        recruit.Age = (short)request.Age;
        recruit.BaseAttraction = (short)request.Attraction;
        recruit.TemplateId = templateId;
        recruit.AvatarData = AvatarManager.Instance.GetRandomAvatar(
            context.Random, gender, false, template.PresetBodyType, (short)request.Attraction);
        recruit.FullName = NameService.CreateName(context, gender, request.Surname, request.GivenName);
        recruit.FinalAttraction = recruit.AvatarData.GetCharm(recruit.BaseAttraction, 0);
        recruit.Recalculate();
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
