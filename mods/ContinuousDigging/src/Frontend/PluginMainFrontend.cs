using System;
using System.Reflection;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;

namespace ContinuousDigging.Frontend
{
    [PluginConfig("ContinuousDiggingFrontend", "MOD Developer", "1.1.0")]
    public sealed class PluginMainFrontend : TaiwuRemakePlugin
    {
        private const string Prefix = "[ContinuousDigging]";

        private static bool _enabled = true;
        private static bool _enableBackendPatch;
        private static bool _enableActionPointCheck = true;
        private static bool _debugLog = true;
        private static int _actionPointCost = 30;
        private static int _maxGradeLimit;
        private static int _maxConsecutiveDigs = 50;
        private static bool _continuousSessionActive;
        private static int _continuedDigCount;
        private static Harmony? _harmony;

        public override void Initialize()
        {
            LoadSettings();
            if (!_enabled)
                return;

            _harmony = new Harmony(GetGuid() + ".frontend");
            InstallPatches();
            AdaptableLog.Info($"{Prefix} Frontend initialized. BackendPatch={_enableBackendPatch}, Debug={_debugLog}");
        }

        public override void OnModSettingUpdate()
        {
            LoadSettings();
            AdaptableLog.Info($"{Prefix} Frontend settings reloaded. Enabled={_enabled}, BackendPatch={_enableBackendPatch}");
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            ResetSession("plugin disposed");
        }

        private void LoadSettings()
        {
            ModManager.GetSetting(ModIdStr, "EnabledContinuousDigging", ref _enabled);
            ModManager.GetSetting(ModIdStr, "EnableBackendPatch", ref _enableBackendPatch);
            ModManager.GetSetting(ModIdStr, "MaxGradeLimit", ref _maxGradeLimit);
            ModManager.GetSetting(ModIdStr, "ActionPointCostPerDig", ref _actionPointCost);
            ModManager.GetSetting(ModIdStr, "EnableActionPointCheck", ref _enableActionPointCheck);
            ModManager.GetSetting(ModIdStr, "MaxConsecutiveDigs", ref _maxConsecutiveDigs);
            ModManager.GetSetting(ModIdStr, "EnableDebugLog", ref _debugLog);
            if (_actionPointCost <= 0)
                _actionPointCost = 30;
            if (_maxConsecutiveDigs <= 0)
                _maxConsecutiveDigs = 50;
        }

        private static void InstallPatches()
        {
            Type? type = AccessTools.TypeByName("Game.Views.Bottom.ViewCollectResource");
            if (type == null)
                throw new TypeLoadException("Game.Views.Bottom.ViewCollectResource");

            Patch(type, "OnInit", postfix: nameof(OnInitPostfix));
            Patch(type, "DigAnimFailed", prefix: nameof(DigAnimFailedPrefix));
            Patch(type, "AnimFinalCall", prefix: nameof(AnimFinalCallPrefix));
            Patch(type, "InvokeStop", postfix: nameof(InvokeStopPostfix));
            Patch(type, "OnDisable", postfix: nameof(OnDisablePostfix));
            AdaptableLog.Info($"{Prefix} Patched current ViewCollectResource flow.");
        }

        private static void Patch(Type type, string targetName, string? prefix = null, string? postfix = null)
        {
            MethodInfo target = AccessTools.Method(type, targetName)
                ?? throw new MissingMethodException(type.FullName, targetName);
            HarmonyMethod? prefixPatch = prefix == null ? null : new HarmonyMethod(typeof(PluginMainFrontend), prefix);
            HarmonyMethod? postfixPatch = postfix == null ? null : new HarmonyMethod(typeof(PluginMainFrontend), postfix);
            _harmony!.Patch(target, prefixPatch, postfixPatch);
        }

        public static void OnInitPostfix(object __instance)
        {
            bool treasure = IsTreasureView(__instance);
            bool series = ReadBool(__instance, "_isDigSeries");
            _continuedDigCount = 0;
            _continuousSessionActive = _enabled && !_enableBackendPatch && treasure && series;
            Debug($"OnInit: treasure={treasure}, series={series}, frontendSession={_continuousSessionActive}, backendPatch={_enableBackendPatch}");
        }

        public static bool DigAnimFailedPrefix(object __instance)
        {
            object? result = ReadMember(__instance, "_digResult");
            Debug($"DigAnimFailed entered: session={_continuousSessionActive}, series={ReadBool(__instance, "_isDigSeries")}, result=({DescribeResult(result)})");

            if (!_continuousSessionActive || result == null || !ReadBool(result, "Success") || ReadBool(result, "AnyMaterial"))
                return true;

            MethodInfo? playEffect = AccessTools.Method(__instance.GetType(), "PlayDigSuccessEffect");
            if (playEffect == null)
            {
                AdaptableLog.Warning($"{Prefix} PlayDigSuccessEffect not found; using original success-stop flow.");
                return true;
            }

            playEffect.Invoke(__instance, null);
            Debug("Suppressed the original success InvokeStop and preserved its success effect.");
            return false;
        }

        public static bool AnimFinalCallPrefix(object __instance)
        {
            object? result = ReadMember(__instance, "_digResult");
            bool series = ReadBool(__instance, "_isDigSeries");
            Debug($"AnimFinalCall entered: session={_continuousSessionActive}, series={series}, count={_continuedDigCount}, result=({DescribeResult(result)})");

            if (!_continuousSessionActive)
                return true;

            if (!series)
            {
                ResetSession("series flag was cleared, normally by player stop");
                return true;
            }

            if (!ShouldContinue(result, out string reason))
            {
                ResetSession(reason);
                return true;
            }

            MethodInfo? doDig = AccessTools.Method(__instance.GetType(), "DoDig");
            if (doDig == null)
            {
                ResetSession("DoDig method not found");
                return true;
            }

            _continuedDigCount++;
            AdaptableLog.Info($"{Prefix} Continue after successful treasure. Completed={_continuedDigCount}, Reason={reason}");
            doDig.Invoke(__instance, null);
            return false;
        }

        public static void InvokeStopPostfix()
        {
            if (_continuousSessionActive)
                ResetSession("InvokeStop called by player");
        }

        public static void OnDisablePostfix()
        {
            ResetSession("treasure UI disabled");
        }

        private static bool ShouldContinue(object? result, out string reason)
        {
            if (result == null)
            {
                reason = "result is null";
                return false;
            }
            if (ReadBool(result, "RequestInvalid"))
            {
                reason = "request invalid or no remaining action days";
                return false;
            }
            if (ReadBool(result, "AnyMaterial"))
            {
                reason = "special material event";
                return false;
            }
            if (!ReadBool(result, "Success"))
            {
                reason = "unsuccessful roll; original series flow handles retry";
                return false;
            }
            if (_maxConsecutiveDigs > 0 && _continuedDigCount >= _maxConsecutiveDigs - 1)
            {
                reason = $"maximum dig count {_maxConsecutiveDigs} reached";
                return false;
            }
            if (_enableActionPointCheck &&
                !SingletonObject.getInstance<TimeManager>().IsActionPointEnough(_actionPointCost))
            {
                reason = $"action point below {_actionPointCost}";
                return false;
            }

            int grade = GetBestRewardGrade(result);
            if (_maxGradeLimit > 0 && grade > 0 && grade <= _maxGradeLimit)
            {
                reason = $"reward grade {grade} reached limit {_maxGradeLimit}";
                return false;
            }

            bool hasReward = ReadBool(result, "AnyItem") || ReadBool(result, "AnyExtraItem");
            reason = hasReward ? $"eligible reward, grade={grade}" : "no item reward";
            return hasReward;
        }

        private static int GetBestRewardGrade(object result)
        {
            try
            {
                if (!ReadBool(result, "AnyItem"))
                    return 0;
                object? itemKey = ReadMember(result, "ItemKey");
                sbyte itemType = Convert.ToSByte(ReadMember(itemKey, "ItemType"));
                short templateId = Convert.ToInt16(ReadMember(itemKey, "TemplateId"));
                return templateId < 0 ? 0 : ItemTemplateHelper.GetGrade(itemType, templateId);
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"{Prefix} Failed to read reward grade: {ex.Message}");
                return 0;
            }
        }

        private static bool IsTreasureView(object instance)
        {
            object? collectType = ReadMember(instance, "CurrentCollectType");
            return collectType != null && Convert.ToInt32(collectType) == 4;
        }

        private static bool ReadBool(object? instance, string name)
        {
            return ReadMember(instance, name) is bool value && value;
        }

        private static object? ReadMember(object? instance, string name)
        {
            if (instance == null)
                return null;
            Type type = instance.GetType();
            PropertyInfo? property = AccessTools.Property(type, name);
            if (property != null)
                return property.GetValue(instance);
            return AccessTools.Field(type, name)?.GetValue(instance);
        }

        private static string DescribeResult(object? result)
        {
            if (result == null)
                return "null";
            return $"type={result.GetType().FullName}, invalid={ReadBool(result, "RequestInvalid")}, success={ReadBool(result, "Success")}, item={ReadBool(result, "AnyItem")}, extra={ReadBool(result, "AnyExtraItem")}, material={ReadBool(result, "AnyMaterial")}, resource={ReadBool(result, "AnyResource")}, itemKey={ReadMember(result, "ItemKey")}, itemCount={ReadMember(result, "ItemCount")}";
        }

        private static void ResetSession(string reason)
        {
            if (_continuousSessionActive)
                Debug($"Session stopped: {reason}");
            _continuousSessionActive = false;
            _continuedDigCount = 0;
        }

        private static void Debug(string message)
        {
            if (_debugLog)
                AdaptableLog.Info($"{Prefix}[Debug] {message}");
        }
    }
}
