using System;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Extra;
using GameData.Domains.Item;
using HarmonyLib;
using NLog;
using TaiwuModdingLib.Core.Plugin;

namespace ContinuousDigging.Backend
{
    [PluginConfig("ContinuousDiggingBackend", "MOD Developer", "1.1.0")]
    public sealed class PluginMainBackend : TaiwuRemakePlugin
    {
        private const string Prefix = "[ContinuousDigging.Backend]";
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static bool _enabled = true;
        private static bool _enableBackendPatch;
        private static bool _debugLog = true;
        private static int _maxGradeLimit;
        private static int _maxConsecutiveDigs = 50;
        [ThreadStatic] private static bool _insideBatch;
        private Harmony _harmony;

        public override void Initialize()
        {
            LoadSettings();
            _harmony = new Harmony(GetGuid() + ".backend");
            MethodInfoGuard.Patch(_harmony);
            Logger.Info("{0} Initialized. Enabled={1}, BackendPatch={2}, MaxDigs={3}",
                Prefix, _enabled, _enableBackendPatch, _maxConsecutiveDigs);
        }

        public override void OnModSettingUpdate()
        {
            LoadSettings();
            Logger.Info("{0} Settings reloaded. Enabled={1}, BackendPatch={2}", Prefix, _enabled, _enableBackendPatch);
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
        }

        private void LoadSettings()
        {
            ReadSetting("EnabledContinuousDigging", ref _enabled);
            ReadSetting("EnableBackendPatch", ref _enableBackendPatch);
            ReadSetting("MaxGradeLimit", ref _maxGradeLimit);
            ReadSetting("MaxConsecutiveDigs", ref _maxConsecutiveDigs);
            ReadSetting("EnableDebugLog", ref _debugLog);
            if (_maxConsecutiveDigs <= 0)
                _maxConsecutiveDigs = 50;
        }

        private void ReadSetting(string key, ref bool value)
        {
            try { DomainManager.Mod.GetSetting(ModIdStr, key, ref value); }
            catch (Exception ex) { Logger.Warn(ex, "{0} Failed to read setting {1}", Prefix, key); }
        }

        private void ReadSetting(string key, ref int value)
        {
            string text = value.ToString();
            try
            {
                DomainManager.Mod.GetSetting(ModIdStr, key, ref text);
                if (int.TryParse(text, out int parsed))
                    value = parsed;
            }
            catch (Exception ex) { Logger.Warn(ex, "{0} Failed to read setting {1}", Prefix, key); }
        }

        private static class MethodInfoGuard
        {
            internal static void Patch(Harmony harmony)
            {
                var target = AccessTools.Method(typeof(ExtraDomain), nameof(ExtraDomain.FindTreasure),
                    new[] { typeof(DataContext), typeof(int) });
                if (target == null)
                    throw new MissingMethodException(typeof(ExtraDomain).FullName, nameof(ExtraDomain.FindTreasure));
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(PluginMainBackend), nameof(FindTreasurePrefix)));
                Logger.Info("{0} Patched ExtraDomain.FindTreasure.", Prefix);
            }
        }

        public static bool FindTreasurePrefix(
            ExtraDomain __instance,
            DataContext context,
            int charId,
            ref TreasureFindResult __result)
        {
            if (!_enabled)
                return true;

            if (!_insideBatch && !HasRemainingTreasure(__instance, charId))
            {
                __result = TreasureFindResult.Invalid;
                Logger.Info("{0} Stop before settlement: current location has no remaining treasure; no days were consumed.", Prefix);
                return false;
            }

            if (!_enableBackendPatch || _insideBatch)
                return true;

            _insideBatch = true;
            try
            {
                TreasureFindResult lastResult = TreasureFindResult.Invalid;
                TreasureFindResult lastSuccess = TreasureFindResult.Invalid;
                int attempts = 0;
                int successes = 0;

                while (attempts < _maxConsecutiveDigs && DomainManager.World.GetLeftDaysInCurrMonth() >= 3)
                {
                    attempts++;
                    TreasureFindResult current = __instance.FindTreasure(context, charId);
                    lastResult = current;
                    if (_debugLog)
                    {
                        Logger.Info("{0} Attempt={1}, Invalid={2}, Success={3}, Item={4}, Extra={5}, Material={6}, Resource={7}",
                            Prefix, attempts, current.RequestInvalid, current.Success, current.AnyItem,
                            current.AnyExtraItem, current.AnyMaterial, current.AnyResource);
                    }

                    if (current.RequestInvalid)
                        break;

                    if (current.Success)
                    {
                        successes++;
                        lastSuccess = current;
                    }

                    if (current.AnyMaterial)
                    {
                        Logger.Info("{0} Stop batch for material event. Attempts={1}", Prefix, attempts);
                        break;
                    }

                    int grade = GetRewardGrade(current);
                    if (_maxGradeLimit > 0 && grade > 0 && grade <= _maxGradeLimit)
                    {
                        Logger.Info("{0} Stop batch at reward grade {1}.", Prefix, grade);
                        break;
                    }

                    if (!HasRemainingTreasure(__instance, current))
                    {
                        Logger.Info("{0} Stop batch: current location has no remaining treasure.", Prefix);
                        break;
                    }
                }

                __result = !lastSuccess.RequestInvalid ? lastSuccess : lastResult;
                Logger.Info("{0} Batch completed. Attempts={1}, Successes={2}, LeftDays={3}, ReturnedSuccess={4}",
                    Prefix, attempts, successes, DomainManager.World.GetLeftDaysInCurrMonth(), __result.Success);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "{0} Batch failed; falling back to one original FindTreasure call.", Prefix);
                return true;
            }
            finally
            {
                _insideBatch = false;
            }
        }

        private static bool HasRemainingTreasure(ExtraDomain domain, TreasureFindResult result)
        {
            if (!result.Location.IsValid())
                return false;
            TreasureExpectResult expect = domain.FindTreasureExpect(result.Location);
            return HasTreasure(expect);
        }

        private static bool HasRemainingTreasure(ExtraDomain domain, int charId)
        {
            if (!DomainManager.Character.TryGetElement_Objects(charId, out var character))
                return false;
            var location = character.GetLocation();
            if (!location.IsValid())
                return false;
            return HasTreasure(domain.FindTreasureExpect(location));
        }

        private static bool HasTreasure(TreasureExpectResult expect)
        {
            // Extra treasures are reflected through MaxGrade/Chance, but do not set AnyNormalItem.
            return expect.AnyNormalItem || expect.AnyMaterial || (expect.MaxGrade > 0 && expect.Chance > 0);
        }

        private static int GetRewardGrade(TreasureFindResult result)
        {
            if (!result.AnyItem)
                return 0;
            ItemKey itemKey = result.ItemKey;
            return ItemTemplateHelper.GetGrade(itemKey.ItemType, itemKey.TemplateId);
        }
    }
}
