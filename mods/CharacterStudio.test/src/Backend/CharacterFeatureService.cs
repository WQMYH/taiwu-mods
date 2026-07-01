using System;
using System.Collections.Generic;
using Config;
using GameData.Common;
using GameData.Domains.Character;
using GameData.Utilities;
using CharacterEntity = GameData.Domains.Character.Character;

namespace CharacterStudio.Backend;

internal static class CharacterFeatureService
{
    internal static bool ApplyProfileFeatures(
        CharacterEntity character,
        DataContext context,
        CharacterFeatureRule rule)
    {
        try
        {
            if (rule.RemoveNegative)
                RemoveFeatures(character, context, CollectConfiguredFeatures(positive: false));
            RemoveFeatures(character, context, rule.RemoveIds);
            if (rule.AddAllPositive)
                AddFeatures(character, context, CollectConfiguredFeatures(positive: true));
            AddFeatures(character, context, rule.AddIds);
            return true;
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning($"[CharacterStudio] 村民特性应用失败：charId={character.GetId()}, {ex}");
            return false;
        }
    }

    internal static bool MergeTaiwuFeatures(
        CharacterEntity taiwu,
        CharacterEntity friend,
        DataContext context,
        ISet<short> excludedMutexGroups)
    {
        try
        {
            List<short> merged = new(friend.GetFeatureIds() ?? new List<short>());
            var existing = new HashSet<short>(merged);
            foreach (short featureId in taiwu.GetFeatureIds() ?? new List<short>())
            {
                CharacterFeatureItem? item = CharacterFeature.Instance.GetItem(featureId);
                if (item != null && excludedMutexGroups.Contains(item.MutexGroupId))
                    continue;
                if (existing.Add(featureId))
                    merged.Add(featureId);
            }
            friend.SetFeatureIds(merged, context);
            var actual = new HashSet<short>(friend.GetFeatureIds() ?? new List<short>());
            bool success = true;
            foreach (short featureId in merged)
                success &= actual.Contains(featureId);
            if (!success)
                AdaptableLog.Warning($"[CharacterStudio] 谷密特性同步校验失败：charId={friend.GetId()}");
            return success;
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning($"[CharacterStudio] 谷密特性同步异常：charId={friend.GetId()}, {ex}");
            return false;
        }
    }

    private static List<short> CollectConfiguredFeatures(bool positive)
    {
        var result = new List<short>();
        foreach (short key in CharacterFeature.Instance.GetAllKeys())
        {
            CharacterFeatureItem? item = CharacterFeature.Instance.GetItem(key);
            if (item == null || item.Hidden)
                continue;
            int type = (int)item.Type;
            bool matches = positive
                ? type == 1 || (type == 0 && item.Level > 0)
                : type == 2 || (type == 0 && item.Level < 0);
            if (matches)
                result.Add(item.TemplateId);
        }
        return result;
    }

    private static void AddFeatures(CharacterEntity character, DataContext context, IEnumerable<short> ids)
    {
        var existing = new HashSet<short>(character.GetFeatureIds() ?? new List<short>());
        foreach (short id in ids)
            if (existing.Add(id))
                character.AddFeature(context, id, true);
    }

    private static void RemoveFeatures(CharacterEntity character, DataContext context, IEnumerable<short> ids)
    {
        var existing = new HashSet<short>(character.GetFeatureIds() ?? new List<short>());
        foreach (short id in ids)
            if (existing.Remove(id))
                character.RemoveFeature(context, id);
    }
}
