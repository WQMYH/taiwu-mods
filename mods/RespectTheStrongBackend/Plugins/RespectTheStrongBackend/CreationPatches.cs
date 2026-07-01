using System;
using GameData.Domains.Character;
using Redzen.Random;

namespace RespectTheStrongBackend;

internal static unsafe class CreationPatches
{
    internal static void NormalDistributePrefix(ref int num, ref int span)
    {
        int original = span;
        span = Math.Max(0, num * ModSettings.CustomSpan / 100);
        ModLog.Debug("CreationSpan", $"mean={num}, span={original}->{span}");
    }

    internal static void MainAttributesPostfix(ref MainAttributes __result)
    {
        for (int i = 0; i < 6; i++)
            __result.Items[i] = Scale(__result.Items[i], ModSettings.GlobalMainAttributeScale);
        ModLog.Debug("ScaleMainAttributes", $"applied {ModSettings.GlobalMainAttributeScale}% to 6 values");
    }

    internal static void LifeQualificationsPostfix(ref LifeSkillShorts __result)
    {
        for (int i = 0; i < 16; i++)
            __result.Items[i] = Scale(__result.Items[i], ModSettings.GlobalLifeSkillScale);
        ModLog.Debug("ScaleLifeQualifications", $"applied {ModSettings.GlobalLifeSkillScale}% to 16 values");
    }

    internal static void CombatQualificationsPostfix(ref CombatSkillShorts __result)
    {
        // CombatSkillShorts has exactly 14 entries in the current backend.
        for (int i = 0; i < 14; i++)
            __result.Items[i] = Scale(__result.Items[i], ModSettings.GlobalCombatSkillScale);
        ModLog.Debug("ScaleCombatQualifications", $"applied {ModSettings.GlobalCombatSkillScale}% to 14 values");
    }

    private static short Scale(short value, int percent) =>
        (short)Math.Clamp((long)value * percent / 100, 0, short.MaxValue);
}
