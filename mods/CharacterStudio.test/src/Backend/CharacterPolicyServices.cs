using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Utilities;
using HarmonyLib;

namespace CharacterStudio.Backend;

internal static class CloseFriendSyncService
{
    internal static bool Apply(DataContext context, Character friend)
    {
        if (!BackendEntry.Settings.SyncCloseFriend)
            return true;
        Character? taiwu = DomainManager.Taiwu.GetTaiwu();
        if (taiwu == null || friend == null)
            return false;

        CharacterValueSnapshot target = CharacterValueAdapter.Capture(taiwu);
        bool valuesOk = CharacterValueAdapter.ApplyVerified(friend, context, target, "CloseFriendSync");
        ISet<short> excluded = ParseShortSet(BackendEntry.Settings.CloseFriendExcludedMutexGroups);
        bool featuresOk = CharacterFeatureService.MergeTaiwuFeatures(taiwu, friend, context, excluded);
        ApplyCloseFriendRelation(context, friend.GetId(), taiwu.GetId());
        return valuesOk && featuresOk;
    }

    private static void ApplyCloseFriendRelation(DataContext context, int friendId, int taiwuId)
    {
        CharacterStudioSettings settings = BackendEntry.Settings;
        if (settings.CloseFriendRelationType <= 0)
            return;
        DomainManager.Character.AddRelation(
            context, friendId, taiwuId, (ushort)settings.CloseFriendRelationType, int.MinValue);
        DomainManager.Character.DirectlySetFavorabilities(
            context,
            friendId,
            taiwuId,
            (short)settings.CloseFriendFavorability,
            (short)settings.CloseFriendFavorability);
    }

    private static ISet<short> ParseShortSet(string text)
    {
        var result = new HashSet<short>();
        foreach (string token in (text ?? "").Split(
                     new[] { ',', ';', '，', '；', ' ' },
                     StringSplitOptions.RemoveEmptyEntries))
            if (short.TryParse(token, out short value))
                result.Add(value);
        return result;
    }
}

internal static class VillagerProfileService
{
    internal static bool Apply(
        Character character,
        DataContext context,
        CharacterProfile profile,
        CreationSource source)
    {
        if (character == null)
            return false;
        CharacterValueSnapshot before = CharacterValueAdapter.Capture(character);
        int profileSeed = profile.Hash.Length >= 8
            ? unchecked((int)Convert.ToUInt32(profile.Hash[..8], 16))
            : 0;
        var random = new Random(unchecked(character.GetId() * 397 ^ profileSeed));
        var target = new CharacterValueSnapshot(
            Resolve(before.Main, profile.MainAttributes, random),
            Resolve(before.Life, profile.LifeQualifications, random),
            Resolve(before.Combat, profile.CombatQualifications, random));

        bool valuesOk = CharacterValueAdapter.ApplyVerified(
            character, context, target, source.ToString());
        if (!valuesOk)
            return false;

        bool featuresOk = CharacterFeatureService.ApplyProfileFeatures(
            character, context, profile.Features);
        if (!featuresOk)
            return false;

        if (profile.BaseHealth > 0)
        {
            character.SetBaseMaxHealth((short)profile.BaseHealth, context);
            character.ChangeHealth(context, profile.BaseHealth - character.GetHealth());
        }
        if (profile.ApplyMorality)
            character.SetBaseMorality((short)profile.Morality, context);
        if (profile.ClothingTemplateId > 0)
            character.ForceReplaceClothing(context, (short)profile.ClothingTemplateId);
        if (profile.BodyType >= 0)
        {
            var avatar = character.GetAvatar();
            avatar.ChangeBodyType((sbyte)profile.BodyType);
            character.SetAvatar(context, avatar);
        }
        if (profile.Bisexual)
        {
            try { Traverse.Create(character).Field("_bisexual").SetValue(true); }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[CharacterStudio] 双性恋字段写入失败：charId={character.GetId()}, {ex.Message}");
            }
        }
        ApplyRelation(context, character.GetId(), profile.RelationToTaiwu);
        AdaptableLog.Info(
            $"[CharacterStudio] 模板应用成功：charId={character.GetId()}, source={source}, profile={profile.Id}, hash={profile.Hash[..12]}");
        return true;
    }

    private static short[] Resolve(short[] current, CharacterValueRule rule, Random random)
    {
        var result = new short[current.Length];
        for (int i = 0; i < current.Length; i++)
            result[i] = rule.Resolve(current[i], random);
        return result;
    }

    private static void ApplyRelation(DataContext context, int charId, CharacterRelationRule rule)
    {
        if (rule.RelationType <= 0)
            return;
        Character? taiwu = DomainManager.Taiwu.GetTaiwu();
        if (taiwu == null || taiwu.GetId() == charId)
            return;
        DomainManager.Character.AddRelation(
            context, charId, taiwu.GetId(), (ushort)rule.RelationType, int.MinValue);
        DomainManager.Character.DirectlySetFavorabilities(
            context,
            charId,
            taiwu.GetId(),
            (short)rule.Favorability,
            (short)rule.Favorability);
    }
}
