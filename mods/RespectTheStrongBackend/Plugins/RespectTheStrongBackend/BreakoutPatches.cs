using System;
using System.Reflection;
using GameData.Domains;
using GameData.Domains.Character;
using HarmonyLib;
using CharacterEntity = GameData.Domains.Character.Character;

namespace RespectTheStrongBackend;

internal static class BreakoutPatches
{
    [ThreadStatic]
    private static int _npcBreakoutDepth;

    internal static void BreakoutPrefix(object[] __args)
    {
        if (!ModSettings.NpcNoBreakoutInjury || __args == null || __args.Length == 0)
            return;
        object context = __args[0];
        if (context == null)
            return;
        FieldInfo field = AccessTools.Field(context.GetType(), "Character");
        CharacterEntity character = field?.GetValue(context) as CharacterEntity;
        if (character != null && character.GetId() != DomainManager.Taiwu.GetTaiwuCharId())
        {
            _npcBreakoutDepth++;
            ModLog.Debug("BreakoutNoInjury",
                $"entered NPC breakout scope; character={character.GetId()}, depth={_npcBreakoutDepth}");
        }
    }

    internal static bool InjuryPrefix()
    {
        bool runOriginal = _npcBreakoutDepth <= 0;
        if (!runOriginal)
            ModLog.Info("BreakoutNoInjury", "prevented one NPC breakout injury calculation");
        return runOriginal;
    }

    internal static Exception BreakoutFinalizer(Exception __exception)
    {
        if (_npcBreakoutDepth > 0)
            _npcBreakoutDepth--;
        if (__exception != null)
            ModLog.Error("BreakoutNoInjury", "exception in NPC breakout scope", __exception);
        else
            ModLog.Debug("BreakoutNoInjury", $"left breakout scope; depth={_npcBreakoutDepth}");
        return __exception;
    }

    internal static void ClearGuard() => _npcBreakoutDepth = 0;
}
