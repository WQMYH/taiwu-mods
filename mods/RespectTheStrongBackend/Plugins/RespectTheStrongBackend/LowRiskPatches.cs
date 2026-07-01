using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using CharacterEntity = GameData.Domains.Character.Character;

namespace RespectTheStrongBackend;

internal static class LowRiskPatches
{
    internal static void GiftLevelPostfix(ref sbyte __result)
    {
        if (ModSettings.PragmaticNpc)
        {
            __result = 9;
            ModLog.Debug("GiftLevel", "gift level overridden to 9");
        }
    }

    internal static void LearnLifeSkillPostfix(
        DataContext context, short lifeSkillTemplateId, CharacterEntity __instance)
    {
        if (!ModSettings.NpcCopyLifeSkillBook || __instance.GetId() == DomainManager.Taiwu.GetTaiwuCharId())
            return;
        short bookId = LifeSkill.Instance[lifeSkillTemplateId].SkillBookId;
        __instance.CreateInventoryItem(context, 10, bookId, 1);
        ModLog.Info("LearnLifeSkill",
            $"created skill book; character={__instance.GetId()}, skill={lifeSkillTemplateId}, book={bookId}");
    }

    internal static void LearnCombatSkillPostfix(
        DataContext context, short combatSkillTemplateId, CharacterEntity __instance)
    {
        if (!ModSettings.NpcCopyCombatSkillBook || __instance.GetId() == DomainManager.Taiwu.GetTaiwuCharId())
            return;
        short bookId = Config.CombatSkill.Instance[combatSkillTemplateId].BookId;
        __instance.CreateInventoryItem(context, 10, bookId, 1);
        ModLog.Info("LearnCombatSkill",
            $"created skill book; character={__instance.GetId()}, skill={combatSkillTemplateId}, book={bookId}");
    }
}
