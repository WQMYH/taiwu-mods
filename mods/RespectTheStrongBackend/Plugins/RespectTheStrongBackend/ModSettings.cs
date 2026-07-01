using System;
using GameData.Domains;

namespace RespectTheStrongBackend;

internal static class ModSettings
{
    internal static bool FixGrowingSectGrade;
    internal static int GrowingSectGrade = 8;
    internal static int CustomSpan = 20;
    internal static int GlobalCombatSkillScale = 100;
    internal static int GlobalLifeSkillScale = 100;
    internal static int GlobalMainAttributeScale = 100;
    internal static int HeroProb;
    internal static int HeroCombatSkillBonus;
    internal static int HeroLifeSkillBonus;
    internal static int HeroMainAttributeBonus;
    internal static bool HeroPerfectFeature;
    internal static bool NpcNoBreakoutInjury;
    internal static bool PragmaticNpc;
    internal static bool NpcCopyCombatSkillBook;
    internal static bool NpcCopyLifeSkillBook;
    internal static bool NeiliAdjust;
    internal static bool NewPromotionRule;
    internal static bool ImproveSkillChoiceLogic;
    internal static bool ImproveOrganizationEquipment;
    internal static bool EnableDebugLog;

    internal static void Reload(string modId)
    {
        Read(modId, "FixGrowingSectGrade", ref FixGrowingSectGrade);
        Read(modId, "GrowingSectGrade", ref GrowingSectGrade);
        Read(modId, "CustomSpan", ref CustomSpan);
        Read(modId, "GlobalCombatSkillScale", ref GlobalCombatSkillScale);
        Read(modId, "GlobalLifeSkillScale", ref GlobalLifeSkillScale);
        Read(modId, "GlobalMainAttributeScale", ref GlobalMainAttributeScale);
        ReadNumber(modId, "HeroProb", ref HeroProb);
        ReadNumber(modId, "HeroCombatSkillBonus", ref HeroCombatSkillBonus);
        ReadNumber(modId, "HeroLifeSkillBonus", ref HeroLifeSkillBonus);
        ReadNumber(modId, "HeroMainAttributeBonus", ref HeroMainAttributeBonus);
        Read(modId, "HeroPerfectFeature", ref HeroPerfectFeature);
        Read(modId, "NpcNoBreakoutInjury", ref NpcNoBreakoutInjury);
        Read(modId, "PragmaticNpc", ref PragmaticNpc);
        Read(modId, "NpcCopyCombatSkillBook", ref NpcCopyCombatSkillBook);
        Read(modId, "NpcCopyLifeSkillBook", ref NpcCopyLifeSkillBook);
        Read(modId, "NeiliAdjust", ref NeiliAdjust);
        Read(modId, "NewPromotionRule", ref NewPromotionRule);
        Read(modId, "ImproveSkillChoiceLogic", ref ImproveSkillChoiceLogic);
        Read(modId, "ImproveOrganizationEquipment", ref ImproveOrganizationEquipment);
        Read(modId, "EnableDebugLog", ref EnableDebugLog);

        GrowingSectGrade = Math.Clamp(GrowingSectGrade, 0, 8);
        CustomSpan = Math.Clamp(CustomSpan, 0, 100);
        GlobalCombatSkillScale = Math.Clamp(GlobalCombatSkillScale, 0, 300);
        GlobalLifeSkillScale = Math.Clamp(GlobalLifeSkillScale, 0, 300);
        GlobalMainAttributeScale = Math.Clamp(GlobalMainAttributeScale, 0, 300);
        HeroProb = Math.Clamp(HeroProb, 0, 10000);
        HeroCombatSkillBonus = Math.Clamp(HeroCombatSkillBonus, 0, 100);
        HeroLifeSkillBonus = Math.Clamp(HeroLifeSkillBonus, 0, 100);
        HeroMainAttributeBonus = Math.Clamp(HeroMainAttributeBonus, 0, 100);
    }

    private static void Read(string modId, string key, ref bool value) =>
        DomainManager.Mod.GetSetting(modId, key, ref value);

    private static void Read(string modId, string key, ref int value) =>
        DomainManager.Mod.GetSetting(modId, key, ref value);

    private static void ReadNumber(string modId, string key, ref int value)
    {
        string text = value.ToString();
        DomainManager.Mod.GetSetting(modId, key, ref text);
        if (!int.TryParse(text, out value))
            value = 0;
    }
}
