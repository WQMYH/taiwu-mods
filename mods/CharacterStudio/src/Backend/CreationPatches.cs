using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.Creation;
using GameData.Domains.Building;
using GameData.Domains.Organization;
using HarmonyLib;

namespace CharacterStudio.Backend;

[HarmonyPatch(typeof(CharacterDomain), "CreateProtagonist")]
internal static class ProtagonistCreatedPatch
{
    private static void Postfix(DataContext context, ProtagonistCreationInfo info)
    {
        if (!BackendEntry.Settings.EnableMod || !BackendEntry.Settings.CustomizeProtagonist)
            return;
        Character? taiwu = DomainManager.Taiwu.GetTaiwu();
        if (taiwu != null)
            CharacterProfileApplier.Apply(taiwu, context, ProfileTarget.Protagonist);
    }
}

[HarmonyPatch(typeof(CharacterDomain), "CreateCloseFriend")]
internal static class CloseFriendCreatedPatch
{
    [ThreadStatic]
    private static bool _creatingBatch;

    private static bool Prefix(
        CharacterDomain __instance,
        DataContext context,
        short charTemplateId,
        short morality,
        Character protagonistChar,
        ref Character __result,
        out bool __state)
    {
        __state = false;
        int count = BackendEntry.Settings.CloseFriendCount;
        if (_creatingBatch || !BackendEntry.Settings.EnableMod || count <= 1)
            return true;

        __state = true;
        _creatingBatch = true;
        Character? last = null;
        try
        {
            for (int i = 0; i < count; i++)
                last = __instance.CreateCloseFriend(context, charTemplateId, morality, protagonistChar);
        }
        finally
        {
            _creatingBatch = false;
        }
        __result = last!;
        return false;
    }

    private static void Postfix(DataContext context, Character __result, bool __state)
    {
        if (__state)
            return;
        if (!BackendEntry.Settings.EnableMod ||
            !BackendEntry.Settings.CustomizeCloseFriend ||
            __result == null)
            return;
        CharacterProfileApplier.Apply(__result, context, ProfileTarget.CloseFriend);
        CharacterProfileApplier.ApplyRelationToTaiwu(context, __result.GetId());
    }
}

[HarmonyPatch(typeof(BuildingDomain), "CreateCharacterByRecruitCharacterData")]
internal static class RecruitedCharacterCreatedPatch
{
    private static void Postfix(DataContext context, int __result)
    {
        if (!BackendEntry.Settings.EnableMod ||
            !BackendEntry.Settings.CustomizeCreatedVillagers ||
            __result <= 0)
            return;
        Character? character = DomainManager.Character.GetElement_Objects(__result);
        if (character != null)
            CharacterProfileApplier.Apply(character, context, ProfileTarget.Villager);
        CharacterProfileApplier.ApplyRelationToTaiwu(context, __result);
    }
}

[HarmonyPatch(typeof(OrganizationDomain), "CreateCoreCharacter")]
internal static class InitialVillageScopePatch
{
    [ThreadStatic]
    internal static bool Active;

    private static void Prefix(SettlementMembersCreationInfo info)
    {
        Active = BackendEntry.Settings.EnableMod &&
                 BackendEntry.Settings.CustomizeInitialVillagers &&
                 info.OrgTemplateId == 16;
    }

    private static Exception? Finalizer(Exception? __exception)
    {
        Active = false;
        return __exception;
    }
}

[HarmonyPatch(typeof(CharacterDomain), "CreateIntelligentCharacter")]
internal static class InitialVillagerCreatedPatch
{
    private static void Postfix(DataContext context, Character __result)
    {
        if (InitialVillageScopePatch.Active && __result != null)
            CharacterProfileApplier.Apply(__result, context, ProfileTarget.Villager);
    }
}
