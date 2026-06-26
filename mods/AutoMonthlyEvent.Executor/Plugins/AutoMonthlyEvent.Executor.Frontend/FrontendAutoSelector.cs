using System;
using System.Collections.Generic;
using System.IO;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using GameData.Utilities;
using HarmonyLib;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal static class FrontendAutoSelector
    {
        private const int MaxPendingAgeMs = 5000;
        private static ExecutorConfig? _config;
        private static PendingSelection? _pending;
        private static readonly Dictionary<string, string> RememberedSelections = new Dictionary<string, string>();
        private static string _memoryFilePath = string.Empty;
        private static bool _isAutoSelecting;

        public static void Configure(ExecutorConfig config)
        {
            _config = config;
            _pending = null;
            _isAutoSelecting = false;
            _memoryFilePath = Path.Combine(config.ModDirectoryPath, config.LogDirectory, "frontend_selection_memory.tsv");
            LoadMemory();
        }

        public static void InstallPatches(Harmony harmony)
        {
            var patchedTypes = new HashSet<Type>();
            string[] typeNames =
            {
                "UI_EventWindow",
                "Game.Views.EventWindow.ViewEventWindow"
            };

            foreach (string typeName in typeNames)
            {
                Type? eventWindowType = AccessTools.TypeByName(typeName);
                if (eventWindowType == null || !patchedTypes.Add(eventWindowType))
                    continue;

                var updateMethod = AccessTools.Method(eventWindowType, "Update");
                var selectMethod = AccessTools.Method(eventWindowType, "SelectOption");
                if (updateMethod == null)
                {
                    AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Event window Update not found on " + eventWindowType.FullName + ".");
                    continue;
                }

                harmony.Patch(updateMethod,
                    postfix: new HarmonyMethod(typeof(FrontendAutoSelector), nameof(EventWindow_Update_Postfix)));

                if (selectMethod != null)
                {
                    harmony.Patch(selectMethod,
                        prefix: new HarmonyMethod(typeof(FrontendAutoSelector), nameof(EventWindow_SelectOption_Prefix)));
                }

                AdaptableLog.Info("[AutoMonthlyEvent.Executor] Frontend auto selector patched " + eventWindowType.FullName + ".");
            }

            if (patchedTypes.Count == 0)
                AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Event window type not found; frontend auto selector is disabled.");
        }

        public static void Enqueue(string signature, string optionKey, EventDecision decision)
        {
            _pending = new PendingSelection(signature, optionKey, decision, DateTime.UtcNow);
            ActionLogger.Debug("frontend-enqueue", decision.EventGuid, decision.CandidateType, $"等待 EventWindow.Update 选择；option={optionKey}; signature={signature}");
        }

        public static bool TryGetRememberedOption(string signature, TaiwuEventDisplayData data, out string optionKey)
        {
            optionKey = string.Empty;
            if (!RememberedSelections.TryGetValue(signature, out string remembered) || string.IsNullOrWhiteSpace(remembered))
                return false;

            if (!HasAvailableOption(data, remembered))
                return false;

            optionKey = remembered;
            return true;
        }

        public static void EventWindow_Update_Postfix(object __instance)
        {
            ExecutorConfig? config = _config;
            PendingSelection? pending = _pending;
            if (config == null || pending == null)
                return;

            if ((DateTime.UtcNow - pending.EnqueuedAt).TotalMilliseconds > MaxPendingAgeMs)
            {
                ActionLogger.Debug("frontend-pending-expired", pending.Decision.EventGuid, pending.Decision.CandidateType, $"等待超时；option={pending.OptionKey}");
                _pending = null;
                return;
            }

            EventModel eventModel = SingletonObject.getInstance<EventModel>();
            TaiwuEventDisplayData? data = eventModel?.DisplayingEventData;
            if (data == null)
                return;

            string currentSignature = EventClassifier.BuildSignature(data);
            if (!string.Equals(currentSignature, pending.Signature, StringComparison.Ordinal))
                return;

            if (!HasAvailableOption(data, pending.OptionKey))
            {
                ActionLogger.Debug("frontend-option-unavailable", pending.Decision.EventGuid, pending.Decision.CandidateType, $"目标选项不可用；option={pending.OptionKey}");
                _pending = null;
                return;
            }

            if (!CanAutoSelectNow(__instance))
                return;

            if (config.DryRun)
            {
                ActionLogger.Debug("frontend-dryrun", pending.Decision.EventGuid, pending.Decision.CandidateType, $"DryRun=true; option={pending.OptionKey}");
                _pending = null;
                return;
            }

            try
            {
                _isAutoSelecting = true;
                ActionLogger.Debug("frontend-select-before", pending.Decision.EventGuid, pending.Decision.CandidateType, $"调用 EventWindow.SelectOptionByOptionKey；option={pending.OptionKey}");
                Traverse.Create(__instance).Method("SelectOptionByOptionKey", pending.OptionKey).GetValue();
                ActionLogger.Debug("frontend-select-after", pending.Decision.EventGuid, pending.Decision.CandidateType, $"SelectOptionByOptionKey 已返回；option={pending.OptionKey}");
            }
            catch (Exception ex)
            {
                ActionLogger.Debug("frontend-select-exception", pending.Decision.EventGuid, pending.Decision.CandidateType, ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                _isAutoSelecting = false;
                _pending = null;
            }
        }

        public static void EventWindow_SelectOption_Prefix(object __instance, EventOptionInfo optionInfo)
        {
            ExecutorConfig? config = _config;
            if (config == null || _isAutoSelecting || !config.EnableFrontendRememberSelection)
                return;

            try
            {
                EventModel eventModel = SingletonObject.getInstance<EventModel>();
                TaiwuEventDisplayData? data = eventModel?.DisplayingEventData;
                if (data == null || optionInfo.OptionState != 0)
                    return;

                if (IsOperateAreaActive(__instance))
                    return;

                string signature = EventClassifier.BuildSignature(data);
                string optionKey = optionInfo.OptionKey ?? string.Empty;
                if (string.IsNullOrWhiteSpace(optionKey))
                    return;

                RememberedSelections[signature] = optionKey;
                SaveMemory(signature, optionKey);
                ActionLogger.Debug("frontend-memory-save", data.EventGuid ?? string.Empty, "manualSelection", $"signature={signature}; option={optionKey}");
            }
            catch (Exception ex)
            {
                ActionLogger.Debug("frontend-memory-exception", string.Empty, "manualSelection", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool CanAutoSelectNow(object eventWindow)
        {
            return !IsOperateAreaActive(eventWindow)
                && GetPrivateBool(eventWindow, "WaitSelect", true)
                && !GetPrivateBool(eventWindow, "_animating", false)
                && !GetPrivateBool(eventWindow, "_layoutDirty", false)
                && !GetPrivateBool(eventWindow, "_isDisplayingLog", false)
                && GetPrivateBool(eventWindow, "_animationMaskComplete", true);
        }

        private static bool HasAvailableOption(TaiwuEventDisplayData data, string optionKey)
        {
            if (data.EventOptionInfos == null)
                return false;

            foreach (EventOptionInfo option in data.EventOptionInfos)
            {
                if (option.OptionKey == optionKey && option.OptionState == 0)
                    return true;
            }
            return false;
        }

        private static bool GetPrivateBool(object eventWindow, string memberName, bool fallback)
        {
            try
            {
                Traverse traverse = Traverse.Create(eventWindow);
                object value = memberName.StartsWith("_", StringComparison.Ordinal)
                    ? traverse.Field(memberName).GetValue()
                    : traverse.Property(memberName).GetValue();
                return value is bool flag ? flag : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static bool IsOperateAreaActive(object eventWindow)
        {
            try
            {
                object operateArea = Traverse.Create(eventWindow).Field("operateArea").GetValue();
                if (operateArea == null)
                    return false;

                object gameObject = AccessTools.Property(operateArea.GetType(), "gameObject")?.GetValue(operateArea, null)!;
                object activeSelf = AccessTools.Property(gameObject.GetType(), "activeSelf")?.GetValue(gameObject, null)!;
                return activeSelf is bool flag && flag;
            }
            catch
            {
                return false;
            }
        }

        private static void LoadMemory()
        {
            RememberedSelections.Clear();
            try
            {
                if (!File.Exists(_memoryFilePath))
                    return;

                foreach (string line in File.ReadAllLines(_memoryFilePath))
                {
                    int split = line.IndexOf('\t');
                    if (split <= 0 || split >= line.Length - 1)
                        continue;
                    RememberedSelections[line.Substring(0, split)] = line.Substring(split + 1);
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Failed to load frontend selection memory: " + ex.Message);
            }
        }

        private static void SaveMemory(string signature, string optionKey)
        {
            try
            {
                string? directory = Path.GetDirectoryName(_memoryFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.AppendAllText(_memoryFilePath, signature + "\t" + optionKey + Environment.NewLine);
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Failed to save frontend selection memory: " + ex.Message);
            }
        }

        private sealed class PendingSelection
        {
            public PendingSelection(string signature, string optionKey, EventDecision decision, DateTime enqueuedAt)
            {
                Signature = signature;
                OptionKey = optionKey;
                Decision = decision;
                EnqueuedAt = enqueuedAt;
            }

            public string Signature { get; }
            public string OptionKey { get; }
            public EventDecision Decision { get; }
            public DateTime EnqueuedAt { get; }
        }
    }
}
