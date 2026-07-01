using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Relation;
using GameData.Utilities;
using HarmonyLib;
using CharacterEntity = GameData.Domains.Character.Character;

namespace CharacterStudio.Backend;

internal static class CharacterBatchService
{
    internal static void ApplyRelations(DataContext context, IReadOnlyList<int> ids, int relation, int favorability)
    {
        relation = CharacterStudioSettings.SafeRelation(relation);
        if (relation == 0 || ids.Count < 2) return;
        for (int i = 0; i < ids.Count; i++)
        for (int j = i + 1; j < ids.Count; j++)
            TryPair(context, ids[i], ids[j], (ushort)relation, (short)Math.Clamp(favorability, -30000, 30000));
    }

    internal static void ApplyRelationToTaiwu(DataContext context, int charId, int relation, int favorability)
    {
        int taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        if (charId > 0 && taiwuId > 0 && charId != taiwuId)
            TryPair(context, charId, taiwuId, (ushort)CharacterStudioSettings.SafeRelation(relation),
                (short)Math.Clamp(favorability, -30000, 30000));
    }

    private static void TryPair(DataContext context, int a, int b, ushort relation, short favor)
    {
        if (relation == 0) return;
        try
        {
            DomainManager.Character.AddRelation(context, a, b, relation, int.MinValue);
            ushort opposite = RelationType.GetOppositeRelationType(relation);
            if (opposite != 0)
                DomainManager.Character.AddRelation(context, b, a, opposite, int.MinValue);
            else if (relation is RelationType.Adored or RelationType.Enemy)
                DomainManager.Character.AddRelation(context, b, a, relation, int.MinValue);
            DomainManager.Character.DirectlySetFavorabilities(context, a, b, favor, favor);
        }
        catch (Exception ex) { AdaptableLog.Warning($"[CharacterStudio] relation {a}<->{b} skipped: {ex.Message}"); }
    }
}

internal static class ReincarnationService
{
    private static readonly MethodInfo? GenerateNextObjectId =
        AccessTools.Method(typeof(CharacterDomain), "GenerateNextObjectId", new[] { typeof(DataContext) });
    private static readonly MethodInfo? AddDeadCharacter =
        AccessTools.Method(typeof(CharacterDomain), "AddElement_DeadCharacters",
            new[] { typeof(int), typeof(DeadCharacter), typeof(DataContext) });
    private static readonly MethodInfo? RemoveDeadCharacter =
        AccessTools.Method(typeof(CharacterDomain), "RemoveElement_DeadCharacters",
            new[] { typeof(int), typeof(DataContext) });
    private static readonly short[] SwordAncestorTemplates = { 201, 202, 203, 204, 205, 206, 207, 208, 209 };

    internal static bool Inject(DataContext context, CharacterEntity target, int source, int count, string profileId)
    {
        if (target == null || GenerateNextObjectId == null || AddDeadCharacter == null) return false;
        count = Math.Clamp(count, 1, PreexistenceCharIds.MaxCount);
        var registeredIds = new List<int>(count);
        try
        {
            List<DeadCharacter> dead = Build(context, target, source, count, profileId);
            if (dead.Count != count) return false;
            foreach (DeadCharacter item in dead)
            {
                int id = (int)GenerateNextObjectId.Invoke(DomainManager.Character, new object[] { context })!;
                AddDeadCharacter.Invoke(DomainManager.Character, new object[] { id, item, context });
                registeredIds.Add(id);
            }
            PreexistenceCharIds value = target.GetPreexistenceCharIds();
            value.Reset();
            foreach (int id in registeredIds) value.Add(context.Random, id);
            target.GetPreexistenceCharIds() = value;
            AdaptableLog.Info($"[CharacterStudio] reincarnation injected: char={target.GetId()}, count={registeredIds.Count}, source={source}");
            return true;
        }
        catch (Exception ex)
        {
            if (RemoveDeadCharacter != null)
                foreach (int id in registeredIds)
                    try { RemoveDeadCharacter.Invoke(DomainManager.Character, new object[] { id, context }); } catch { }
            AdaptableLog.Warning($"[CharacterStudio] reincarnation injection skipped: char={target.GetId()}, {ex}");
            return false;
        }
    }

    private static List<DeadCharacter> Build(
        DataContext context, CharacterEntity target, int source, int count, string profileId)
    {
        int date = DomainManager.World.GetCurrDate();
        var result = new List<DeadCharacter>(count);
        CharacterProfile profile = CharacterProfileRepository.Resolve(CreationSource.ManualCreate, profileId);
        List<short> randomTemplates = Config.Character.Instance
            .Where(x => x != null && x.TemplateId >= 0)
            .Select(x => x.TemplateId).ToList();
        for (int i = 0; i < count; i++)
        {
            DeadCharacter item;
            if (source == 0)
                item = DeadCharacterHelper.CreateDeadCharacter(target, date);
            else
            {
                short templateId = source == 1
                    ? SwordAncestorTemplates[i % SwordAncestorTemplates.Length]
                    : randomTemplates[context.Random.Next(randomTemplates.Count)];
                item = DeadCharacterHelper.CreateDeadNonIntelligentCharacter(context.Random, templateId, date);
                if (source == 2) ApplyProfile(item, profile, target.GetId() * 397 + i);
            }
            result.Add(item);
        }
        return result;
    }

    private static void ApplyProfile(DeadCharacter dead, CharacterProfile profile, int seed)
    {
        var random = new Random(seed);
        MainAttributes main = dead.BaseMainAttributes;
        for (int i = 0; i < 6; i++) main[i] = profile.MainAttributes.Resolve(main[i], random);
        dead.BaseMainAttributes = main;
        LifeSkillShorts life = dead.BaseLifeSkillQualifications;
        for (int i = 0; i < 16; i++) life[i] = profile.LifeQualifications.Resolve(life[i], random);
        dead.BaseLifeSkillQualifications = life;
        CombatSkillShorts combat = dead.BaseCombatSkillQualifications;
        for (int i = 0; i < 14; i++) combat[i] = profile.CombatQualifications.Resolve(combat[i], random);
        dead.BaseCombatSkillQualifications = combat;
        if (profile.ApplyMorality) dead.Morality = (short)profile.Morality;
        if (profile.Features.RemoveNegative)
            dead.FeatureIds.RemoveAll(id => CharacterFeature.Instance[id]?.Level < 0);
        if (profile.Features.AddAllPositive)
            foreach (CharacterFeatureItem item in CharacterFeature.Instance)
                if (item != null && item.Level > 0 && !dead.FeatureIds.Contains(item.TemplateId))
                    dead.FeatureIds.Add(item.TemplateId);
    }
}
