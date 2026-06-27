using System;
using System.Collections.Generic;
using System.Reflection;
using GameData.Domains.Extra;
using GameData.Domains.Item;
using GameData.Domains.Item.Display;
using GameData.Serializer;
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
        private static bool _enableActionPointCheck = true;
        private static bool _debugLog = true;
        private static int _actionPointCost = 30;
        private static int _maxGradeLimit;
        private static int _maxConsecutiveDigs = 50;
        private static bool _continuousSessionActive;
        private static bool _expectRequestPending;
        private static int _sessionGeneration;
        private static int _animationGeneration;
        private static int _continuedDigCount;
        private static float _digEffectReadyAt;
        private static float _animationReadyAt;
        private static readonly HashSet<string> CapturedResultSignatures = new HashSet<string>();
        private static readonly HashSet<string> ProcessedAnimationSignatures = new HashSet<string>();
        private static readonly HashSet<string> FinalizedResultSignatures = new HashSet<string>();
        private static readonly List<ItemDisplayData> CollectedItemDisplayData = new List<ItemDisplayData>();
        private static Harmony? _harmony;

        public override void Initialize()
        {
            LoadSettings();
            if (!_enabled)
                return;

            _harmony = new Harmony(GetGuid() + ".frontend");
            InstallPatches();
            AdaptableLog.Info($"{Prefix} Frontend initialized. Debug={_debugLog}");
        }

        public override void OnModSettingUpdate()
        {
            LoadSettings();
            AdaptableLog.Info($"{Prefix} Frontend settings reloaded. Enabled={_enabled}");
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
            Patch(type, "AnimEntry", postfix: nameof(AnimEntryPostfix));
            Patch(type, "DigAnimFailed", prefix: nameof(DigAnimFailedPrefix));
            Patch(type, "AnimFinalCall", prefix: nameof(AnimFinalCallPrefix));
            Patch(type, "InvokeStop", postfix: nameof(InvokeStopPostfix));
            Patch(type, "OnDisable", prefix: nameof(OnDisablePrefix), postfix: nameof(OnDisablePostfix));
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
            _sessionGeneration++;
            _continuedDigCount = 0;
            _animationGeneration = 0;
            _expectRequestPending = false;
            CapturedResultSignatures.Clear();
            ProcessedAnimationSignatures.Clear();
            FinalizedResultSignatures.Clear();
            CollectedItemDisplayData.Clear();
            _continuousSessionActive = _enabled && treasure && series;
            Debug($"OnInit: treasure={treasure}, series={series}, frontendSession={_continuousSessionActive}, generation={_sessionGeneration}");
        }

        public static void AnimEntryPostfix(object __instance)
        {
            if (!_continuousSessionActive)
                return;

            _animationGeneration++;
            _digEffectReadyAt = UnityEngine.Time.time + 1.4f;
            _animationReadyAt = UnityEngine.Time.time + 2.95f;
            Debug($"AnimEntry started: animation={_animationGeneration}, signature={GetResultSignature(ReadMember(__instance, "_digResult"))}, effectReadyAt={_digEffectReadyAt:F3}, finalReadyAt={_animationReadyAt:F3}");
        }

        public static bool DigAnimFailedPrefix(object __instance)
        {
            object? result = ReadMember(__instance, "_digResult");
            string signature = GetResultSignature(result);
            string animationKey = $"{_animationGeneration}:{signature}";
            Debug($"DigAnimFailed entered: session={_continuousSessionActive}, series={ReadBool(__instance, "_isDigSeries")}, animation={_animationGeneration}, signature={signature}, result=({DescribeResult(result)})");

            if (!_continuousSessionActive || result == null || !ReadBool(result, "Success") || ReadBool(result, "AnyMaterial"))
                return true;

            if (UnityEngine.Time.time < _digEffectReadyAt)
            {
                Debug($"Ignored stale DigAnimFailed before current effect time. now={UnityEngine.Time.time:F3}, readyAt={_digEffectReadyAt:F3}, animation={_animationGeneration}");
                return false;
            }

            if (!ProcessedAnimationSignatures.Add(animationKey))
            {
                Debug($"Ignored duplicate DigAnimFailed callback for {animationKey}.");
                return false;
            }

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
            string signature = GetResultSignature(result);
            string animationKey = $"{_animationGeneration}:{signature}";
            CaptureCurrentResult(__instance, result, animationKey);
            Debug($"AnimFinalCall entered: session={_continuousSessionActive}, series={series}, pending={_expectRequestPending}, count={_continuedDigCount}, animation={_animationGeneration}, signature={signature}, result=({DescribeResult(result)})");

            if (!_continuousSessionActive)
                return true;

            if (!ReadBool(__instance, "_skipDigAnimation") && UnityEngine.Time.time < _animationReadyAt)
            {
                Debug($"Ignored stale AnimFinalCall before current animation was ready. now={UnityEngine.Time.time:F3}, readyAt={_animationReadyAt:F3}, signature={signature}");
                return false;
            }

            if (!FinalizedResultSignatures.Add(animationKey))
            {
                Debug($"Ignored duplicate AnimFinalCall for finalized animation {animationKey}.");
                return false;
            }

            if (_expectRequestPending)
            {
                Debug("Ignored duplicate AnimFinalCall while FindTreasureExpect is pending.");
                return false;
            }

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

            CaptureRewardThenBeginTreasureExpect(__instance, result!, reason, animationKey);
            return false;
        }

        public static void InvokeStopPostfix()
        {
            if (_continuousSessionActive)
                ResetSession("InvokeStop called by player");
        }

        public static void OnDisablePrefix(object __instance)
        {
            object? result = ReadMember(__instance, "_digResult");
            CaptureCurrentResult(__instance, result, $"{_animationGeneration}:{GetResultSignature(result)}");
            if (CollectedItemDisplayData.Count == 0)
                return;

            FieldInfo? field = AccessTools.Field(__instance.GetType(), "_digItemDataList");
            if (field == null)
            {
                AdaptableLog.Warning($"{Prefix} Could not inject collected rewards: _digItemDataList field not found.");
                return;
            }

            field.SetValue(__instance, new List<ItemDisplayData>(CollectedItemDisplayData));
            AdaptableLog.Info($"{Prefix} Injected {CollectedItemDisplayData.Count} collected reward entries into the original obtain window.");
        }

        public static void OnDisablePostfix()
        {
            ResetSession("treasure UI disabled");
            _sessionGeneration++;
            _expectRequestPending = false;
            CapturedResultSignatures.Clear();
            ProcessedAnimationSignatures.Clear();
            FinalizedResultSignatures.Clear();
            CollectedItemDisplayData.Clear();
        }

        private static void CaptureRewardThenBeginTreasureExpect(
            object instance, object result, string continueReason, string animationKey)
        {
            if (CapturedResultSignatures.Contains(animationKey))
            {
                BeginTreasureExpect(instance, result, continueReason);
                return;
            }

            if (!(result is TreasureFindResult treasureResult))
            {
                AdaptableLog.Warning($"{Prefix} Could not strongly type the treasure result; continuing without reward aggregation.");
                BeginTreasureExpect(instance, result, continueReason);
                return;
            }

            var itemKeys = new List<ItemKey>();
            if (treasureResult.AnyItem)
                itemKeys.Add(treasureResult.ItemKey);
            if (treasureResult.AnyExtraItem && treasureResult.ExtraItems != null)
                itemKeys.AddRange(treasureResult.ExtraItems);
            if (itemKeys.Count == 0)
            {
                BeginTreasureExpect(instance, result, continueReason);
                return;
            }

            int generation = _sessionGeneration;
            _expectRequestPending = true;
            Debug($"Waiting for reward display data before continuing. animationKey={animationKey}, itemKeys={itemKeys.Count}");
            ItemDomainMethod.AsyncCall.GetItemDisplayDataList(null, itemKeys, (offset, pool) =>
            {
                try
                {
                    if (generation != _sessionGeneration || !_continuousSessionActive)
                    {
                        Debug($"Ignored stale reward display callback. CallbackGeneration={generation}, CurrentGeneration={_sessionGeneration}");
                        return;
                    }

                    var displayData = new List<ItemDisplayData>();
                    Serializer.Deserialize(pool, offset, ref displayData);
                    foreach (ItemDisplayData item in displayData)
                    {
                        if (treasureResult.AnyItem && item.Key.Equals(treasureResult.ItemKey))
                            item.Amount = (int)treasureResult.ItemCount;
                    }

                    if (CapturedResultSignatures.Add(animationKey))
                    {
                        CollectedItemDisplayData.AddRange(displayData);
                        Debug($"Captured reward asynchronously for {animationKey}. Added={displayData.Count}, Total={CollectedItemDisplayData.Count}");
                    }

                    _expectRequestPending = false;
                    BeginTreasureExpect(instance, result, continueReason);
                }
                catch (Exception ex)
                {
                    AdaptableLog.Error($"{Prefix} Reward display capture failed: {ex.Message}\n{ex.StackTrace}");
                    _expectRequestPending = false;
                    BeginTreasureExpect(instance, result, continueReason);
                }
            });
        }

        private static void BeginTreasureExpect(object instance, object result, string continueReason)
        {
            object? locationObject = ReadMember(result, "Location");
            if (!(locationObject is GameData.Domains.Map.Location location) || !location.IsValid())
            {
                FinishWithOriginalFlow(instance, "result location is invalid");
                return;
            }

            int generation = _sessionGeneration;
            _expectRequestPending = true;
            Debug($"Requesting FindTreasureExpect before continuing. Location={location}, Generation={generation}");
            ExtraDomainMethod.AsyncCall.FindTreasureExpect(null, location, (offset, pool) =>
            {
                try
                {
                    if (generation != _sessionGeneration || !_continuousSessionActive)
                    {
                        Debug($"Ignored stale FindTreasureExpect callback. CallbackGeneration={generation}, CurrentGeneration={_sessionGeneration}");
                        return;
                    }

                    TreasureExpectResult expect = default;
                    Serializer.Deserialize(pool, offset, ref expect);
                    bool hasTreasure = expect.AnyNormalItem || expect.AnyMaterial ||
                                       (expect.MaxGrade > 0 && expect.Chance > 0);
                    Debug($"FindTreasureExpect result: location={location}, normal={expect.AnyNormalItem}, material={expect.AnyMaterial}, maxGrade={expect.MaxGrade}, chance={expect.Chance}, hasTreasure={hasTreasure}");

                    if (!hasTreasure)
                    {
                        FinishWithOriginalFlow(instance, "current location has no remaining treasure");
                        return;
                    }

                    MethodInfo? doDig = AccessTools.Method(instance.GetType(), "DoDig");
                    if (doDig == null)
                    {
                        FinishWithOriginalFlow(instance, "DoDig method not found");
                        return;
                    }

                    _expectRequestPending = false;
                    _continuedDigCount++;
                    AdaptableLog.Info($"{Prefix} Continue after treasure precheck. Completed={_continuedDigCount}, Reason={continueReason}");
                    doDig.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    AdaptableLog.Error($"{Prefix} FindTreasureExpect callback failed: {ex.Message}\n{ex.StackTrace}");
                    FinishWithOriginalFlow(instance, "treasure precheck failed");
                }
            });
        }

        private static void FinishWithOriginalFlow(object instance, string reason)
        {
            _expectRequestPending = false;
            ResetSession(reason);
            try
            {
                AccessTools.Field(instance.GetType(), "_isDigSeries")?.SetValue(instance, false);
                MethodInfo? animFinalCall = AccessTools.Method(instance.GetType(), "AnimFinalCall");
                if (animFinalCall == null)
                {
                    AdaptableLog.Warning($"{Prefix} Cannot finish original flow: AnimFinalCall method not found.");
                    return;
                }
                animFinalCall.Invoke(instance, null);
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"{Prefix} Failed to finish original treasure flow: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void CaptureCurrentResult(object instance, object? result, string animationKey)
        {
            if (result == null || !ReadBool(result, "Success") || string.IsNullOrEmpty(animationKey))
                return;
            if (!CapturedResultSignatures.Add(animationKey))
                return;

            object? value = ReadMember(instance, "_digItemDataList");
            if (!(value is List<ItemDisplayData> current) || current.Count == 0)
            {
                CapturedResultSignatures.Remove(animationKey);
                Debug($"Reward display data is not ready for {animationKey}; asynchronous capture will be used.");
                return;
            }

            CollectedItemDisplayData.AddRange(current);
            Debug($"Captured reward {animationKey}. Added={current.Count}, Total={CollectedItemDisplayData.Count}");
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

        private static string GetResultSignature(object? result)
        {
            if (result == null)
                return string.Empty;
            return $"{ReadMember(result, "ItemKey")}|{ReadMember(result, "ItemCount")}|{ReadMember(result, "MaterialTemplateId")}|{ReadMember(result, "ExtraItemType")}";
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
