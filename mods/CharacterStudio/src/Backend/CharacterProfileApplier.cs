using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Utilities;
using HarmonyLib;

namespace CharacterStudio.Backend;

internal enum ProfileTarget { Protagonist, CloseFriend, Villager }

internal static class CharacterProfileApplier
{
    internal static void Apply(Character character, DataContext context, ProfileTarget target)
    {
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (!settings.EnableMod)
            return;

        MainAttributes attributes = character.GetBaseMainAttributes();
        for (int i = 0; i < 6; i++)
            attributes[i] = ApplyMode(attributes[i], settings.MainAttributeValue, settings.AttributeMode);
        character.SetBaseMainAttributes(attributes, context);

        LifeSkillShorts life = character.GetBaseLifeSkillQualifications();
        for (int i = 0; i < 16; i++)
            life[i] = ApplyMode(life[i], settings.LifeQualificationValue, settings.AttributeMode);
        character.SetBaseLifeSkillQualifications(ref life, context);

        CombatSkillShorts combat = character.GetBaseCombatSkillQualifications();
        for (int i = 0; i < 14; i++)
            combat[i] = ApplyMode(combat[i], settings.CombatQualificationValue, settings.AttributeMode);
        character.SetBaseCombatSkillQualifications(ref combat, context);

        if (settings.BaseHealth > 0)
        {
            character.SetBaseMaxHealth((short)settings.BaseHealth, context);
            character.ChangeHealth(context, settings.BaseHealth - character.GetHealth());
        }
        if (settings.Morality >= 0)
            character.SetBaseMorality((short)settings.Morality, context);
        if (settings.ClothingTemplateId > 0)
            character.ForceReplaceClothing(context, (short)settings.ClothingTemplateId);
        if (settings.BodyType >= 0)
        {
            var avatar = character.GetAvatar();
            avatar.ChangeBodyType((sbyte)settings.BodyType);
            character.SetAvatar(context, avatar);
        }
        if (!string.IsNullOrWhiteSpace(settings.DefaultSurname) ||
            !string.IsNullOrWhiteSpace(settings.DefaultGivenName))
        {
            FullName name = NameService.CreateName(
                context,
                character.GetGender(),
                settings.DefaultSurname,
                settings.DefaultGivenName);
            Traverse.Create(character).Field("_fullName").SetValue(name);
        }

        foreach (short id in ParseIds(settings.RemoveFeatureIds))
            character.RemoveFeature(context, id);
        foreach (short id in ParseIds(settings.FeatureIds))
            character.AddFeature(context, id, true);

        if (settings.Bisexual)
        {
            try { Traverse.Create(character).Field("_bisexual").SetValue(true); }
            catch (Exception ex) { Debug($"双性恋字段设置失败：{ex.Message}"); }
        }
        Debug($"已应用人物档案：target={target}, charId={character.GetId()}");
    }

    internal static void ApplyRelationToTaiwu(DataContext context, int characterId)
    {
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (settings.RelationType <= 0)
            return;
        Character? taiwu = DomainManager.Taiwu.GetTaiwu();
        if (taiwu == null || taiwu.GetId() == characterId)
            return;
        int taiwuId = taiwu.GetId();
        DomainManager.Character.AddRelation(
            context, characterId, taiwuId, (ushort)settings.RelationType, int.MinValue);
        DomainManager.Character.DirectlySetFavorabilities(
            context, characterId, taiwuId, (short)settings.Favorability, (short)settings.Favorability);
    }

    private static short ApplyMode(short current, int configured, int mode) =>
        mode switch
        {
            1 => (short)Math.Max(current, configured),
            2 => (short)configured,
            _ => current
        };

    private static IEnumerable<short> ParseIds(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;
        foreach (string token in text.Split(new[] { ',', ';', '，', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            if (short.TryParse(token.Trim(), out short id) && id >= 0)
                yield return id;
    }

    private static void Debug(string message)
    {
        if (BackendEntry.Settings.EnableDebugLog)
            AdaptableLog.Info("[CharacterStudio] " + message);
    }
}
