using System;
using System.Linq;
using GameData.Common;
using GameData.Domains.Character;
using GameData.Utilities;

namespace CharacterStudio.Backend;

internal sealed record CharacterValueSnapshot(short[] Main, short[] Life, short[] Combat)
{
    internal bool EqualsValues(CharacterValueSnapshot other) =>
        Main.SequenceEqual(other.Main) &&
        Life.SequenceEqual(other.Life) &&
        Combat.SequenceEqual(other.Combat);

    public override string ToString() =>
        $"main=[{string.Join(",", Main)}] life=[{string.Join(",", Life)}] combat=[{string.Join(",", Combat)}]";
}

internal static class CharacterValueAdapter
{
    internal static CharacterValueSnapshot Capture(Character character)
    {
        MainAttributes main = character.GetBaseMainAttributes();
        LifeSkillShorts life = character.GetBaseLifeSkillQualifications();
        CombatSkillShorts combat = character.GetBaseCombatSkillQualifications();
        return new CharacterValueSnapshot(
            Copy(main, 6),
            Copy(life, 16),
            Copy(combat, 14));
    }

    internal static bool ApplyVerified(
        Character character,
        DataContext context,
        CharacterValueSnapshot target,
        string source)
    {
        CharacterValueSnapshot before = Capture(character);
        LogSnapshot(character.GetId(), source, "before", before);
        LogSnapshot(character.GetId(), source, "target", target);

        try
        {
            ApplySetters(character, context, target);
            CharacterValueSnapshot afterSetter = Capture(character);
            LogSnapshot(character.GetId(), source, "after-setter", afterSetter);
            if (afterSetter.EqualsValues(target))
            {
                Debug($"数值 setter 校验成功：charId={character.GetId()}, source={source}");
                return true;
            }

            ApplyByDelta(character, context, target, afterSetter);
            CharacterValueSnapshot afterFallback = Capture(character);
            LogSnapshot(character.GetId(), source, "after-delta", afterFallback);
            if (afterFallback.EqualsValues(target))
            {
                Debug($"数值差值回退成功：charId={character.GetId()}, source={source}");
                return true;
            }
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning($"[CharacterStudio] 数值写入异常：charId={character.GetId()}, source={source}, {ex}");
        }

        try
        {
            ApplySetters(character, context, before);
            CharacterValueSnapshot afterRestoreSetter = Capture(character);
            if (!afterRestoreSetter.EqualsValues(before))
                ApplyByDelta(character, context, before, afterRestoreSetter);
            CharacterValueSnapshot restored = Capture(character);
            LogSnapshot(character.GetId(), source, "rollback", restored);
            AdaptableLog.Warning(
                $"[CharacterStudio] 数值写入校验失败，已回滚：charId={character.GetId()}, source={source}, restored={restored.EqualsValues(before)}");
        }
        catch (Exception rollbackEx)
        {
            AdaptableLog.Warning(
                $"[CharacterStudio] 数值写入及回滚均失败：charId={character.GetId()}, source={source}, {rollbackEx}");
        }
        return false;
    }

    private static void ApplySetters(Character character, DataContext context, CharacterValueSnapshot target)
    {
        MainAttributes main = character.GetBaseMainAttributes();
        for (int i = 0; i < 6; i++)
            main[i] = target.Main[i];
        character.SetBaseMainAttributes(main, context);

        LifeSkillShorts life = character.GetBaseLifeSkillQualifications();
        for (int i = 0; i < 16; i++)
            life[i] = target.Life[i];
        character.SetBaseLifeSkillQualifications(ref life, context);

        CombatSkillShorts combat = character.GetBaseCombatSkillQualifications();
        for (int i = 0; i < 14; i++)
            combat[i] = target.Combat[i];
        character.SetBaseCombatSkillQualifications(ref combat, context);
    }

    private static void ApplyByDelta(
        Character character,
        DataContext context,
        CharacterValueSnapshot target,
        CharacterValueSnapshot current)
    {
        for (sbyte i = 0; i < 6; i++)
        {
            int delta = target.Main[i] - current.Main[i];
            if (delta != 0)
                character.ChangeBaseMainAttribute(context, i, (short)delta);
        }
        for (sbyte i = 0; i < 16; i++)
        {
            int delta = target.Life[i] - current.Life[i];
            if (delta != 0)
                character.ChangeBaseLifeSkillQualification(context, i, delta);
        }
        for (sbyte i = 0; i < 14; i++)
        {
            int delta = target.Combat[i] - current.Combat[i];
            if (delta != 0)
                character.ChangeBaseCombatSkillQualification(context, i, delta);
        }
    }

    private static short[] Copy(MainAttributes value, int length)
    {
        var result = new short[length];
        for (int i = 0; i < length; i++)
            result[i] = value[i];
        return result;
    }

    private static short[] Copy(LifeSkillShorts value, int length)
    {
        var result = new short[length];
        for (int i = 0; i < length; i++)
            result[i] = value[i];
        return result;
    }

    private static short[] Copy(CombatSkillShorts value, int length)
    {
        var result = new short[length];
        for (int i = 0; i < length; i++)
            result[i] = value[i];
        return result;
    }

    private static void LogSnapshot(int charId, string source, string stage, CharacterValueSnapshot snapshot)
    {
        if (BackendEntry.Settings.EnableDebugLog && BackendEntry.Settings.LogValueSnapshots)
            AdaptableLog.Info($"[CharacterStudio] snapshot charId={charId} source={source} stage={stage} {snapshot}");
    }

    private static void Debug(string message)
    {
        if (BackendEntry.Settings.EnableDebugLog)
            AdaptableLog.Info("[CharacterStudio] " + message);
    }
}
