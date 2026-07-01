using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Relation;
using GameData.Domains.Mod;
using GameData.Domains.Taiwu;
using GameData.Domains.TaiwuEvent.EventHelper;
using GameData.Serializer;
using GameData.Utilities;
using HarmonyLib;
using Character = GameData.Domains.Character.Character;

namespace CharacterStudio.Backend;

internal static class LegacyTransferService
{
    private static readonly MethodInfo? GenerateNextObjectId =
        AccessTools.Method(typeof(CharacterDomain), "GenerateNextObjectId", new[] { typeof(DataContext) });
    private static readonly MethodInfo? AddDeadCharacter =
        AccessTools.Method(typeof(CharacterDomain), "AddElement_DeadCharacters",
            new[] { typeof(int), typeof(DeadCharacter), typeof(DataContext) });

    internal static SerializableModData RequestLegacyPassing(DataContext context, SerializableModData _)
    {
        var result = Result(false, "即刻传剑请求被拒绝。");
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (!settings.EnableLegacyFeatures || !settings.EnableImmediateLegacyPassing)
            return Result(false, "即刻传剑功能未启用。");
        if (DomainManager.Taiwu.GetLegacyPassingState() != 0)
            return Result(false, "当前已在传剑流程中。");
        if (DomainManager.Combat.IsInCombat())
            return Result(false, "战斗中不能传剑。");
        if (DomainManager.World.GetAdvancingMonthState() != 0)
            return Result(false, "过月过程中不能传剑。");

        Character taiwu;
        try { taiwu = DomainManager.Taiwu.GetTaiwu(); }
        catch { return Result(false, "当前存档中没有有效太吾。"); }
        if (taiwu == null)
            return Result(false, "当前存档中没有有效太吾。");

        if (!DomainManager.Taiwu.IsLegacyPassingUnlocked())
        {
            if (!settings.ForceXiangshuInfectionBeforePassing)
                return Result(false, "尚未满足原版传剑条件。");
            try
            {
                if (!taiwu.IsCompletelyInfected())
                {
                    taiwu.ChangeXiangshuInfection(context, 200);
                    taiwu.UpdateXiangshuInfectionState(context);
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning("[CharacterStudio] 强制感染失败：" + ex);
                return Result(false, "无法修改相枢感染状态。");
            }
        }

        try
        {
            EventHelper.TriggerLegacyPassingEvent(false, "");
            AdaptableLog.Info("[CharacterStudio] 已请求原版传剑流程。");
            return Result(true, "已进入原版传剑流程。");
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning("[CharacterStudio] 即刻传剑失败：" + ex);
            return Result(false, "触发传剑失败，请查看后端日志。");
        }
    }

    internal static SerializableModData RevealPreviousIdentity(DataContext context, SerializableModData data)
    {
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (!settings.EnableLegacyFeatures || !settings.EnableRevealPreviousIdentity)
            return Result(false, "袒露转世身份功能未启用。");
        int npcId = -1;
        data?.Get("NpcId", out npcId);
        int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        if (npcId < 0 || npcId == taiwuId || !DomainManager.Character.TryGetElement_Objects(npcId, out _))
            return Result(false, "目标人物无效。");

        List<int> previous = DomainManager.Taiwu.GetPreviousTaiwuIds();
        if (previous == null || previous.Count == 0)
            return Result(false, "当前太吾没有可袒露的前任身份。");

        int oldTaiwuId = -1;
        RelatedCharacter npcToOld = default;
        RelatedCharacter oldToNpc = default;
        for (int i = previous.Count - 1; i >= 0; i--)
        {
            int candidate = previous[i];
            if (DomainManager.Character.TryGetRelation(npcId, candidate, out npcToOld))
            {
                oldTaiwuId = candidate;
                DomainManager.Character.TryGetRelation(candidate, npcId, out oldToNpc);
                break;
            }
        }
        if (oldTaiwuId < 0)
            return Result(false, "此人不认识任何前任太吾。");

        try
        {
            if (!DomainManager.Character.TryGetRelation(taiwuId, npcId, out _))
                DomainManager.Character.TryCreateRelation(context, taiwuId, npcId);
            if (settings.RevealTransferRelationTypes)
            {
                SetRelationBits(context, npcId, taiwuId, npcToOld.RelationType);
                SetRelationBits(context, taiwuId, npcId, oldToNpc.RelationType);
            }
            if (settings.RevealTransferFavorability)
                DomainManager.Character.DirectlySetFavorabilities(
                    context, taiwuId, npcId, oldToNpc.Favorability, npcToOld.Favorability);
            if (settings.RevealRemoveOldRelation)
            {
                DomainManager.Character.RemoveRelation(context, oldTaiwuId, npcId);
                DomainManager.Character.RemoveRelation(context, npcId, oldTaiwuId);
            }
            AdaptableLog.Info($"[CharacterStudio] 已迁移前任太吾 {oldTaiwuId} 与人物 {npcId} 的关系。");
            return Result(true, "对方接受了你袒露的前世身份。");
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning("[CharacterStudio] 袒露身份失败：" + ex);
            return Result(false, "关系迁移失败，请查看后端日志。");
        }
    }

    internal static void ApplyAfterTransfer(DataContext context, Character newTaiwu, Character oldTaiwu)
    {
        CharacterStudioSettings s = BackendEntry.Settings;
        if (!s.EnableLegacyFeatures)
            return;
        Try("外观", () =>
        {
            if (s.TransferInheritAvatar)
                newTaiwu.SetAvatar(context, new GameData.Domains.Character.AvatarSystem.AvatarData(oldTaiwu.GetAvatar()));
        });
        Try("姓名", () =>
        {
            if (s.TransferInheritName)
                newTaiwu.SetFullName(oldTaiwu.GetFullName(), context);
        });
        Try("立场", () =>
        {
            if (s.TransferInheritMorality)
                newTaiwu.SetBaseMorality(oldTaiwu.GetBaseMorality(), context);
        });
        Try("六维", () => { if (s.TransferMergeMainAttributes) MergeMainAttributes(context, newTaiwu, oldTaiwu); });
        Try("资质", () => { if (s.TransferMergeQualifications) MergeQualifications(context, newTaiwu, oldTaiwu); });
        Try("特性", () => { if (s.TransferFeatureMode > 0) MergeFeatures(context, newTaiwu, oldTaiwu, s); });
        Try("前世", () => { if (s.EnableTransferPreexistence) AddPreexistence(context, newTaiwu, oldTaiwu, s); });
    }

    private static unsafe void MergeMainAttributes(DataContext context, Character target, Character source)
    {
        MainAttributes value = target.GetBaseMainAttributes();
        MainAttributes old = source.GetBaseMainAttributes();
        for (int i = 0; i < 6; i++)
            value[i] = Math.Max(value[i], old[i]);
        target.SetBaseMainAttributes(value, context);
    }

    private static unsafe void MergeQualifications(DataContext context, Character target, Character source)
    {
        LifeSkillShorts life = target.GetBaseLifeSkillQualifications();
        LifeSkillShorts oldLife = source.GetBaseLifeSkillQualifications();
        for (int i = 0; i < 16; i++)
            life[i] = Math.Max(life[i], oldLife[i]);
        target.SetBaseLifeSkillQualifications(ref life, context);

        CombatSkillShorts combat = target.GetBaseCombatSkillQualifications();
        CombatSkillShorts oldCombat = source.GetBaseCombatSkillQualifications();
        for (int i = 0; i < 14; i++)
            combat[i] = Math.Max(combat[i], oldCombat[i]);
        target.SetBaseCombatSkillQualifications(ref combat, context);
    }

    private static void MergeFeatures(
        DataContext context, Character target, Character source, CharacterStudioSettings settings)
    {
        HashSet<short> excluded = ParseShortSet(settings.TransferExcludedFeatureGroups);
        Dictionary<short, CharacterFeatureItem> byGroup = new();
        foreach (short id in target.GetFeatureIds())
        {
            CharacterFeatureItem item = CharacterFeature.Instance[id];
            if (item != null && item.MutexGroupId > 0)
                byGroup[item.MutexGroupId] = item;
        }
        foreach (short id in source.GetFeatureIds())
        {
            CharacterFeatureItem item = CharacterFeature.Instance[id];
            if (item == null || excluded.Contains(item.MutexGroupId))
                continue;
            if (settings.TransferFeatureMode >= 2 && item.GeneticProb <= 0)
                continue;
            if (settings.TransferFeatureMode == 4 && item.Level < 0)
                continue;
            if (settings.TransferFeatureMode >= 3 &&
                byGroup.TryGetValue(item.MutexGroupId, out CharacterFeatureItem? current) &&
                current != null &&
                current.Level >= item.Level)
                continue;
            target.AddFeature(context, id, true);
            if (item.MutexGroupId > 0)
                byGroup[item.MutexGroupId] = item;
        }
    }

    private static unsafe void AddPreexistence(
        DataContext context, Character newTaiwu, Character oldTaiwu, CharacterStudioSettings settings)
    {
        if (GenerateNextObjectId == null || AddDeadCharacter == null)
        {
            AdaptableLog.Warning("[CharacterStudio] 当前版本缺少精确的死亡人物注册接口，前世功能已跳过。");
            return;
        }
        PreexistenceCharIds ids = newTaiwu.GetPreexistenceCharIds();
        bool reachesCapacity = ids.Count == PreexistenceCharIds.MaxCount - 1;
        if (ids.Count >= PreexistenceCharIds.MaxCount)
        {
            if (settings.PreexistenceOverflowMode == 1)
                return;
            for (int i = 1; i < ids.Count; i++)
                ids.CharIds[i - 1] = ids.CharIds[i];
            ids.Count--;
        }

        int objectId = (int)GenerateNextObjectId.Invoke(DomainManager.Character, new object[] { context })!;
        DeadCharacter dead = DeadCharacterHelper.CreateDeadCharacter(oldTaiwu, DomainManager.World.GetCurrDate());
        AddDeadCharacter.Invoke(DomainManager.Character, new object[] { objectId, dead, context });
        ids.Add(context.Random, objectId);
        newTaiwu.GetPreexistenceCharIds() = ids;
        if (reachesCapacity && settings.GrantReincarnationFeatureAtCapacity)
        {
            List<short> candidates = CharacterFeature.Instance
                .Where(item => item != null && Character.IsPositiveReincarnationBonusFeature(item.TemplateId))
                .Select(item => item.TemplateId)
                .ToList();
            if (candidates.Count > 0)
                newTaiwu.AddFeature(context, candidates[context.Random.Next(candidates.Count)], true);
        }
        AdaptableLog.Info($"[CharacterStudio] 已将前任太吾记录为前世，DeadCharacterId={objectId}。");
    }

    private static void SetRelationBits(DataContext context, int from, int to, ushort desired)
    {
        if (!DomainManager.Character.TryGetRelation(from, to, out RelatedCharacter current))
            return;
        for (int bit = 0; bit < 16; bit++)
        {
            ushort mask = (ushort)(1 << bit);
            bool has = (current.RelationType & mask) != 0;
            bool want = (desired & mask) != 0;
            if (has && !want) DomainManager.Character.ChangeRelationType(context, from, to, mask, 0);
            else if (!has && want) DomainManager.Character.ChangeRelationType(context, from, to, 0, mask);
        }
    }

    private static HashSet<short> ParseShortSet(string text)
    {
        var result = new HashSet<short>();
        foreach (string part in (text ?? "").Split(','))
            if (short.TryParse(part.Trim(), out short value))
                result.Add(value);
        return result;
    }

    private static SerializableModData Result(bool success, string message)
    {
        var result = new SerializableModData();
        result.Set("Success", success);
        result.Set("Message", message);
        return result;
    }

    private static void Try(string name, Action action)
    {
        try { action(); }
        catch (Exception ex) { AdaptableLog.Warning($"[CharacterStudio] 传剑{name}处理失败：" + ex); }
    }
}

[HarmonyPatch(typeof(TaiwuDomain), nameof(TaiwuDomain.TransferTaiwuData),
    new[] { typeof(DataContext), typeof(Character), typeof(Character), typeof(bool) })]
internal static class TransferTaiwuDataPatch
{
    private static void Postfix(DataContext context, Character newTaiwuChar, Character oldTaiwuChar) =>
        LegacyTransferService.ApplyAfterTransfer(context, newTaiwuChar, oldTaiwuChar);
}
