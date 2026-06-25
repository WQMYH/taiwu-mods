using System;
using System.Collections.Generic;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Character.ParallelModifications;
using HarmonyLib;
using NLog;
using TaiwuModdingLib.Core.Plugin;

namespace PregnantStateGuard.Backend
{
    [PluginConfig("PregnantStateGuard", "WQMYH", "0.1.0")]
    public sealed class BackendEntry : TaiwuRemakePlugin
    {
        private const string LogPrefix = "[PregnantStateGuard]";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private Harmony _harmony;

        public override void Initialize()
        {
            _harmony = new Harmony(GetGuid() + ".backend");
            InstallHarmonyPatches();
            Logger.Info("{0} Backend initialized.", LogPrefix);
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            Logger.Info("{0} Backend disposed.", LogPrefix);
        }

        private void InstallHarmonyPatches()
        {
            PatchGetElementPregnantStates();
            PatchParallelUpdatePregnantState();
            PatchOfflineUpdatePregnantState();
        }

        private void PatchGetElementPregnantStates()
        {
            var target = AccessTools.Method(typeof(CharacterDomain), "GetElement_PregnantStates",
                new[] { typeof(int) });
            if (target == null)
            {
                Logger.Warn("{0} Target not found: CharacterDomain.GetElement_PregnantStates(int).", LogPrefix);
                return;
            }

            _harmony.Patch(target,
                finalizer: new HarmonyMethod(typeof(BackendEntry), nameof(GetElementPregnantStatesFinalizer)));
            Logger.Info("{0} Patch installed: CharacterDomain.GetElement_PregnantStates.", LogPrefix);
        }

        private void PatchParallelUpdatePregnantState()
        {
            var target = AccessTools.Method(typeof(CharacterDomain), "ParallelUpdatePregnantState",
                new[] { typeof(DataContext), typeof(Character), typeof(PregnantStateModification) });
            if (target == null)
            {
                Logger.Warn("{0} Target not found: CharacterDomain.ParallelUpdatePregnantState(DataContext, Character, PregnantStateModification).", LogPrefix);
                return;
            }

            _harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(BackendEntry), nameof(ParallelUpdatePregnantStatePrefix)),
                finalizer: new HarmonyMethod(typeof(BackendEntry), nameof(ParallelUpdatePregnantStateFinalizer)));
            Logger.Info("{0} Patch installed: CharacterDomain.ParallelUpdatePregnantState.", LogPrefix);
        }

        private void PatchOfflineUpdatePregnantState()
        {
            var target = AccessTools.Method(typeof(Character), "OfflineUpdatePregnantState",
                new[] { typeof(DataContext), typeof(PeriAdvanceMonthUpdateStatusModification) });
            if (target == null)
            {
                Logger.Warn("{0} Target not found: Character.OfflineUpdatePregnantState(DataContext, PeriAdvanceMonthUpdateStatusModification).", LogPrefix);
                return;
            }

            _harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(BackendEntry), nameof(OfflineUpdatePregnantStatePrefix)),
                finalizer: new HarmonyMethod(typeof(BackendEntry), nameof(OfflineUpdatePregnantStateFinalizer)));
            Logger.Info("{0} Patch installed: Character.OfflineUpdatePregnantState.", LogPrefix);
        }

        private static Exception GetElementPregnantStatesFinalizer(Exception __exception,
            int elementId, ref PregnantState __result)
        {
            if (__exception == null)
                return null;

            if (!IsMissingPregnantStateKey(__exception, elementId))
                return __exception;

            __result = null;
            Logger.Warn(__exception,
                "{0} Suppressed missing PregnantStates key in GetElement_PregnantStates. elementId={1}",
                LogPrefix, elementId);
            return null;
        }

        private static bool ParallelUpdatePregnantStatePrefix(Character mother)
        {
            int motherId = SafeGetCharacterId(mother);
            if (motherId < 0 || HasPregnantState(motherId))
                return true;

            Logger.Warn("{0} Skipped ParallelUpdatePregnantState because PregnantStates key is missing. motherId={1}",
                LogPrefix, motherId);
            return false;
        }

        private static Exception ParallelUpdatePregnantStateFinalizer(Exception __exception, Character mother)
        {
            if (__exception == null)
                return null;

            int motherId = SafeGetCharacterId(mother);
            if (!IsMissingPregnantStateKey(__exception, motherId))
                return __exception;

            Logger.Warn(__exception,
                "{0} Suppressed missing PregnantStates key in ParallelUpdatePregnantState. motherId={1}",
                LogPrefix, motherId);
            return null;
        }

        private static bool OfflineUpdatePregnantStatePrefix(Character __instance,
            PeriAdvanceMonthUpdateStatusModification mod, ref bool __result)
        {
            if (mod?.PregnantStateModification == null)
                return true;

            int characterId = SafeGetCharacterId(__instance);
            if (characterId < 0 || HasPregnantState(characterId))
                return true;

            __result = false;
            Logger.Warn("{0} Skipped OfflineUpdatePregnantState because PregnantStates key is missing. characterId={1}",
                LogPrefix, characterId);
            return false;
        }

        private static Exception OfflineUpdatePregnantStateFinalizer(Exception __exception,
            Character __instance, ref bool __result)
        {
            if (__exception == null)
                return null;

            int characterId = SafeGetCharacterId(__instance);
            if (!IsMissingPregnantStateKey(__exception, characterId))
                return __exception;

            __result = false;
            Logger.Warn(__exception,
                "{0} Suppressed missing PregnantStates key in OfflineUpdatePregnantState. characterId={1}",
                LogPrefix, characterId);
            return null;
        }

        private static bool HasPregnantState(int characterId)
        {
            try
            {
                return DomainManager.Character.TryGetPregnantState(characterId, out PregnantState _);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "{0} Failed to query PregnantState. characterId={1}", LogPrefix, characterId);
                return true;
            }
        }

        private static bool IsMissingPregnantStateKey(Exception exception, int expectedCharId)
        {
            if (exception is not KeyNotFoundException)
                return false;

            if (expectedCharId < 0)
                return true;

            string message = exception.Message ?? string.Empty;
            return message.Contains($"'{expectedCharId}'") ||
                   message.Contains($"\"{expectedCharId}\"") ||
                   message.Contains(expectedCharId.ToString());
        }

        private static int SafeGetCharacterId(Character character)
        {
            try
            {
                return character?.GetId() ?? -1;
            }
            catch
            {
                return -1;
            }
        }
    }
}
