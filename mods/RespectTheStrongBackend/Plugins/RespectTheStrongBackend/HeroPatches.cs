using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using GameData.Common;
using GameData.Domains.Character;
using GameData.Domains.Character.Creation;
using GameData.Domains.Character.ParallelModifications;
using CharacterEntity = GameData.Domains.Character.Character;

namespace RespectTheStrongBackend;

internal static class HeroPatches
{
    internal readonly struct CreationState
    {
        internal CreationState(bool eligible, bool hero)
        {
            Eligible = eligible;
            Hero = hero;
        }
        internal bool Eligible { get; }
        internal bool Hero { get; }
    }

    internal static void CreatePrefix(
        DataContext context, ref IntelligentCharacterCreationInfo info, out CreationState __state)
    {
        if (ModSettings.FixGrowingSectGrade)
        {
            sbyte original = info.GrowingSectGrade;
            info.GrowingSectGrade = (sbyte)ModSettings.GrowingSectGrade;
            ModLog.Debug("GrowingSectGrade", $"overridden {original}->{info.GrowingSectGrade}");
        }

        bool eligible = info.MotherCharId < 0 && info.FatherCharId < 0;
        bool hero = eligible && ModSettings.HeroProb > 0 &&
                    context.Random.Next(10000) < ModSettings.HeroProb;
        __state = new CreationState(eligible, hero);
        ModLog.Debug("HeroCreation",
            $"evaluated: eligible={eligible}, selected={hero}, probability={ModSettings.HeroProb}/10000");
    }

    internal static void CreatePostfix(
        DataContext context, CreateIntelligentCharacterModification __result, CreationState __state)
    {
        if (!__state.Eligible || !__state.Hero || __result?.Self == null)
            return;

        CharacterEntity character = __result.Self;
        AddMainAttributes(character, context);
        AddLifeQualifications(character, context);
        AddCombatQualifications(character, context);
        if (ModSettings.HeroPerfectFeature)
            UpgradeBasicFeatures(character, context);
        ModLog.Info("HeroCreation",
            $"hero bonuses applied; character={character.GetId()}, main={ModSettings.HeroMainAttributeBonus}, " +
            $"life={ModSettings.HeroLifeSkillBonus}, combat={ModSettings.HeroCombatSkillBonus}, " +
            $"perfectFeature={ModSettings.HeroPerfectFeature}");
    }

    private static void AddMainAttributes(CharacterEntity character, DataContext context)
    {
        short bonus = (short)ModSettings.HeroMainAttributeBonus;
        character.ChangeBaseMainAttributes(context,
            new MainAttributes(new[] { bonus, bonus, bonus, bonus, bonus, bonus }));
    }

    private static void AddLifeQualifications(CharacterEntity character, DataContext context)
    {
        short[] values = Enumerable.Repeat((short)ModSettings.HeroLifeSkillBonus, 16).ToArray();
        var delta = new LifeSkillShorts(values);
        character.ChangeBaseLifeSkillQualifications(context, ref delta);
    }

    private static void AddCombatQualifications(CharacterEntity character, DataContext context)
    {
        short[] values = Enumerable.Repeat((short)ModSettings.HeroCombatSkillBonus, 14).ToArray();
        var delta = new CombatSkillShorts(values);
        character.ChangeBaseCombatSkillQualifications(context, ref delta);
    }

    private static void UpgradeBasicFeatures(CharacterEntity character, DataContext context)
    {
        List<short> current = character.GetFeatureIds() ?? new List<short>();
        var bestByGroup = new Dictionary<short, CharacterFeatureItem>();
        foreach (short key in CharacterFeature.Instance.GetAllKeys())
        {
            CharacterFeatureItem item = CharacterFeature.Instance.GetItem(key);
            if (item == null || !item.Basic || item.Hidden || item.MutexGroupId <= 0 || item.Level <= 0)
                continue;
            if (!bestByGroup.TryGetValue(item.MutexGroupId, out CharacterFeatureItem best) ||
                item.Level > best.Level)
                bestByGroup[item.MutexGroupId] = item;
        }

        var replacements = new Dictionary<short, short>();
        foreach (short id in current)
        {
            CharacterFeatureItem item = CharacterFeature.Instance.GetItem(id);
            if (item != null && item.Basic && bestByGroup.TryGetValue(item.MutexGroupId, out CharacterFeatureItem best) &&
                best.Level > item.Level)
                replacements[id] = best.TemplateId;
        }

        foreach ((short oldId, short newId) in replacements)
        {
            character.RemoveFeature(context, oldId);
            character.AddFeature(context, newId, true);
        }
        ModLog.Debug("HeroPerfectFeature",
            $"character={character.GetId()}, upgradedFeatures={replacements.Count}");
    }
}
