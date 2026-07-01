using System;
using System.Collections.Generic;
using GameData.Domains.Character;
using GameData.Domains.Organization;
using CharacterEntity = GameData.Domains.Character.Character;

namespace RespectTheStrongBackend;

internal static unsafe class PromotionPatches
{
    internal static void CalcInfluencePowerPostfix(
        CharacterEntity character, short baseInfluencePower, ref short __result)
    {
        if (!ModSettings.NewPromotionRule || character == null)
            return;

        sbyte orgId = character.GetOrganizationInfo().OrgTemplateId;
        if (orgId < 1 || orgId > 15 || __result <= 0)
            return;

        CombatSkillShorts combat = character.GetCombatSkillQualifications();
        MainAttributes main = character.GetMaxMainAttributes();

        long combatSum = 0;
        for (int i = 0; i < 14; i++)
            combatSum += combat.Items[i];
        long mainSum = 0;
        for (int i = 0; i < 6; i++)
            mainSum += main.Items[i];

        // Use current character data rather than old hard-coded member-table indices.
        double combatAverage = combatSum / 14.0;
        double mainAverage = mainSum / 6.0;
        double strength = combatAverage * 0.7 + mainAverage * 0.3;
        double factor = Math.Clamp(0.75 + strength / 400.0, 0.75, 1.60);
        int adjusted = (int)Math.Round(__result * factor);
        short original = __result;
        __result = (short)Math.Clamp(adjusted, 0, short.MaxValue);
        ModLog.Debug("Promotion",
            $"character={character.GetId()}, org={orgId}, strength={strength:F2}, factor={factor:F3}, influence={original}->{__result}");
    }
}
