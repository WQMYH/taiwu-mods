using System;
using System.Collections.Generic;
using System.IO;
using FrameWork.UISystem.UIElements;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using GameData.Utilities;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal static class FrontendAutoSelector
    {
        private const int MaxPendingAgeMs = 5000;
        private static ExecutorConfig? _config;
        private static PendingSelection? _pending;
        private static readonly Dictionary<string, string> CustomSelections = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> LooseSelections = new Dictionary<string, string>();
        private static readonly HashSet<string> LooseConflicts = new HashSet<string>();
        private static string _customMemoryFilePath = string.Empty;
        private static bool _isAutoSelecting;
        private static bool _customSkipSuspended;
        private static HotkeyBinding _suspendHotkey = HotkeyBinding.Empty;
        private static CToggle? _markCustomSkipToggle;

        public static void Configure(ExecutorConfig config)
        {
            _config = config;
            _pending = null;
            _isAutoSelecting = false;
            _customSkipSuspended = false;
            _suspendHotkey = HotkeyBinding.Parse(config.CustomDialogSkipSuspendHotkey);
            _customMemoryFilePath = Path.Combine(config.ModDirectoryPath, "UserData", "custom_dialog_skip.tsv");
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
                var updateOptionScrollMethod = AccessTools.Method(eventWindowType, "UpdateOptionScroll");
                var selectMethod = AccessTools.Method(eventWindowType, "SelectOption");
                if (updateMethod == null)
                {
                    AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Event window Update not found on " + eventWindowType.FullName + ".");
                    continue;
                }

                harmony.Patch(updateMethod,
                    postfix: new HarmonyMethod(typeof(FrontendAutoSelector), nameof(EventWindow_Update_Postfix)));

                if (updateOptionScrollMethod != null)
                {
                    try
                    {
                        harmony.Patch(updateOptionScrollMethod,
                            postfix: new HarmonyMethod(typeof(FrontendAutoSelector), nameof(EventWindow_UpdateOptionScroll_Postfix)));
                    }
                    catch (Exception ex)
                    {
                        AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Failed to patch event window UpdateOptionScroll on "
                            + eventWindowType.FullName + ": " + ex.Message);
                    }
                }

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
            ExecutorConfig? config = _config;
            if (config == null || !config.EnableCustomDialogSkip)
                return false;

            if (_customSkipSuspended)
            {
                ActionLogger.Debug("custom-dialog-suspended", data.EventGuid ?? string.Empty, "customDialogSkip", "玩家自定义对话跳过已被快捷键临时暂停");
                return false;
            }

            if (!CustomSelections.TryGetValue(signature, out string remembered) || string.IsNullOrWhiteSpace(remembered))
            {
                string looseSignature = EventClassifier.BuildLooseSignature(data);
                if (LooseConflicts.Contains(looseSignature))
                {
                    ActionLogger.Debug("custom-dialog-loose-conflict", data.EventGuid ?? string.Empty, "customDialogSkip", $"宽松签名存在冲突规则，交给玩家处理；loose={looseSignature}");
                    return false;
                }

                if (!LooseSelections.TryGetValue(looseSignature, out remembered) || string.IsNullOrWhiteSpace(remembered))
                    return false;

                ActionLogger.Debug("custom-dialog-loose-hit", data.EventGuid ?? string.Empty, "customDialogSkip", $"命中宽松签名；loose={looseSignature}; option={remembered}");
            }
            else
            {
                ActionLogger.Debug("custom-dialog-strict-hit", data.EventGuid ?? string.Empty, "customDialogSkip", $"命中严格签名；option={remembered}");
            }

            if (!HasAvailableOption(data, remembered))
            {
                ActionLogger.Debug("custom-dialog-option-blocked", data.EventGuid ?? string.Empty, "customDialogSkip", $"目标选项不存在或不可用，阻止跳过；option={remembered}");
                return false;
            }

            optionKey = remembered;
            return true;
        }

        public static void EventWindow_Update_Postfix(object __instance)
        {
            ExecutorConfig? config = _config;
            if (config == null)
                return;

            HandleSuspendHotkey(config);

            PendingSelection? pending = _pending;
            if (pending == null)
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

        public static void EventWindow_UpdateOptionScroll_Postfix(object __instance)
        {
            ExecutorConfig? config = _config;
            if (config == null || !config.EnableCustomDialogSkip)
            {
                RemoveMarkCustomSkipToggle();
                return;
            }

            try
            {
                EventModel eventModel = SingletonObject.getInstance<EventModel>();
                TaiwuEventDisplayData? data = eventModel?.DisplayingEventData;
                if (data == null || data.EventOptionInfos == null)
                    return;

                RectTransform? optionContainer = Traverse.Create(__instance).Field("optionContainer").GetValue<RectTransform>();
                if (optionContainer == null)
                    return;

                string signature = EventClassifier.BuildSignature(data);
                RemoveMarkCustomSkipToggle();
                _markCustomSkipToggle = CreateToggle(optionContainer, "AutoMonthlyEventCustomSkipToggle", "记住本次选择", "打开后手动选择一次选项；以后遇到同一事件签名时自动复用。");
                _markCustomSkipToggle.isOn = CustomSelections.ContainsKey(signature);
                LayoutRebuilder.ForceRebuildLayoutImmediate(optionContainer);
            }
            catch (Exception ex)
            {
                ActionLogger.Debug("custom-dialog-toggle-exception", string.Empty, "customDialogSkip", ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static void EventWindow_SelectOption_Prefix(object __instance, EventOptionInfo optionInfo)
        {
            ExecutorConfig? config = _config;
            if (config == null || _isAutoSelecting || !config.EnableCustomDialogSkip)
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
                if (string.IsNullOrWhiteSpace(optionKey) || _markCustomSkipToggle == null)
                    return;

                if (_markCustomSkipToggle.isOn)
                {
                    CustomSelections[signature] = optionKey;
                    RebuildLooseIndex();
                    PersistMemory();
                    ActionLogger.Debug("custom-dialog-save", data.EventGuid ?? string.Empty, "customDialogSkip", $"signature={signature}; option={optionKey}");
                    return;
                }

                if (CustomSelections.Remove(signature))
                {
                    RebuildLooseIndex();
                    PersistMemory();
                    ActionLogger.Debug("custom-dialog-remove", data.EventGuid ?? string.Empty, "customDialogSkip", $"signature={signature}");
                }
            }
            catch (Exception ex)
            {
                ActionLogger.Debug("custom-dialog-save-exception", string.Empty, "customDialogSkip", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void HandleSuspendHotkey(ExecutorConfig config)
        {
            if (!config.EnableCustomDialogSkip || !_suspendHotkey.IsValid)
                return;

            if (!_suspendHotkey.IsPressed())
                return;

            _customSkipSuspended = !_customSkipSuspended;
            ActionLogger.Debug("custom-dialog-suspend-toggle", string.Empty, "customDialogSkip", _customSkipSuspended ? "玩家自定义对话跳过：已全局暂停" : "玩家自定义对话跳过：已恢复");
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
            CustomSelections.Clear();
            LooseSelections.Clear();
            LooseConflicts.Clear();
            try
            {
                if (!File.Exists(_customMemoryFilePath))
                    return;

                foreach (string line in File.ReadAllLines(_customMemoryFilePath))
                {
                    int split = line.IndexOf('\t');
                    if (split <= 0 || split >= line.Length - 1)
                        continue;
                    CustomSelections[line.Substring(0, split)] = line.Substring(split + 1);
                }
                RebuildLooseIndex();
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Failed to load custom dialog skip memory: " + ex.Message);
            }
        }

        private static void RebuildLooseIndex()
        {
            LooseSelections.Clear();
            LooseConflicts.Clear();
            foreach (KeyValuePair<string, string> pair in CustomSelections)
            {
                string loose = BuildLooseSignatureFromStoredStrictSignature(pair.Key);
                if (string.IsNullOrEmpty(loose))
                    continue;

                if (LooseSelections.TryGetValue(loose, out string existing) && existing != pair.Value)
                    LooseConflicts.Add(loose);
                else
                    LooseSelections[loose] = pair.Value;
            }

            foreach (string conflict in LooseConflicts)
                LooseSelections.Remove(conflict);
        }

        private static string BuildLooseSignatureFromStoredStrictSignature(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
                return string.Empty;

            string[] parts = signature.Split('|');
            if (parts.Length < 5)
                return string.Empty;

            var looseParts = new List<string> { parts[0] };
            for (int i = 1; i < parts.Length - 3; i++)
            {
                string optionPart = parts[i];
                int hashIndex = optionPart.IndexOf('#');
                looseParts.Add(hashIndex >= 0 ? optionPart.Substring(0, hashIndex) : optionPart);
            }
            return string.Join("|", looseParts);
        }

        private static void PersistMemory()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_customMemoryFilePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var lines = new List<string>();
                foreach (KeyValuePair<string, string> pair in CustomSelections)
                    lines.Add(pair.Key + "\t" + pair.Value);
                File.WriteAllLines(_customMemoryFilePath, lines);
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning("[AutoMonthlyEvent.Executor] Failed to save custom dialog skip memory: " + ex.Message);
            }
        }

        private static CToggle CreateToggle(Transform parent, string name, string label, string description)
        {
            TextMeshProUGUI sourceText = parent.GetComponentInChildren<TextMeshProUGUI>(true);
            GameObject row = new GameObject(name + "Row");
            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.SetParent(parent, false);
            rowRect.sizeDelta = new Vector2(620f, 48f);
            row.transform.SetAsFirstSibling();

            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.minHeight = 48f;
            layout.preferredHeight = 48f;
            layout.minWidth = 180f;
            layout.preferredWidth = 320f;

            HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
            group.childControlHeight = false;
            group.childControlWidth = false;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = false;
            group.childAlignment = TextAnchor.MiddleLeft;
            group.spacing = 12f;
            group.padding = new RectOffset(26, 0, 6, 0);

            TextMeshProUGUI labelText = CreateLabel(row.transform, label, sourceText);
            CToggle toggle = CreateSwitch(row.transform, name);
            AddTooltip(labelText.gameObject, description);
            return toggle;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string label, TextMeshProUGUI sourceText)
        {
            GameObject labelGo = new GameObject("Label");
            RectTransform rect = labelGo.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(136f, 34f);
            TextMeshProUGUI text = labelGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Left;
            text.color = new Color(0.93f, 0.87f, 0.72f, 1f);
            if (sourceText != null)
            {
                text.font = sourceText.font;
                text.fontSharedMaterial = sourceText.fontSharedMaterial;
                text.color = sourceText.color;
            }
            text.raycastTarget = true;
            LayoutElement layout = labelGo.AddComponent<LayoutElement>();
            layout.preferredWidth = 136f;
            layout.preferredHeight = 34f;
            return text;
        }

        private static CToggle CreateSwitch(Transform parent, string name)
        {
            GameObject toggleGo = new GameObject(name);
            RectTransform rect = toggleGo.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(48f, 26f);
            CImage background = toggleGo.AddComponent<CImage>();
            background.color = new Color(0.17f, 0.14f, 0.1f, 0.95f);
            background.raycastTarget = true;
            toggleGo.AddComponent<UIInteractionBehaviour>();
            CToggle toggle = toggleGo.AddComponent<CToggle>();
            toggle.targetGraphic = background;

            GameObject checkGo = new GameObject("Checkmark");
            RectTransform checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.SetParent(toggleGo.transform, false);
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.anchoredPosition = new Vector2(36f, 0f);
            checkRect.sizeDelta = new Vector2(14f, 14f);
            CImage checkImage = checkGo.AddComponent<CImage>();
            checkImage.color = new Color(0.85f, 0.65f, 0.28f, 1f);
            checkImage.raycastTarget = false;
            toggle.graphic = checkImage;
            return toggle;
        }

        private static void AddTooltip(GameObject gameObject, string tips)
        {
            try
            {
                TooltipInvoker tooltip = gameObject.GetComponent<TooltipInvoker>() ?? gameObject.AddComponent<TooltipInvoker>();
                tooltip.enabled = true;
                tooltip.Type = TipType.SingleDesc;
                tooltip.IsLanguageKey = false;
                tooltip.PresetParam = new[] { tips };
                Traverse.Create(tooltip).Method("Refresh", false, -1).GetValue();
            }
            catch
            {
                // Tooltip APIs have changed between versions; the toggle still works without a tooltip.
            }
        }

        private static void RemoveMarkCustomSkipToggle()
        {
            if (_markCustomSkipToggle != null && _markCustomSkipToggle.gameObject != null)
            {
                UnityEngine.Object row = _markCustomSkipToggle.transform.parent.gameObject;
                UnityEngine.Object.Destroy(_markCustomSkipToggle.gameObject);
                UnityEngine.Object.Destroy(row);
            }
            _markCustomSkipToggle = null;
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

        private sealed class HotkeyBinding
        {
            public static readonly HotkeyBinding Empty = new HotkeyBinding(false, false, false, KeyCode.None);

            private readonly bool _ctrl;
            private readonly bool _alt;
            private readonly bool _shift;
            private readonly KeyCode _key;

            private HotkeyBinding(bool ctrl, bool alt, bool shift, KeyCode key)
            {
                _ctrl = ctrl;
                _alt = alt;
                _shift = shift;
                _key = key;
            }

            public bool IsValid => _key != KeyCode.None;

            public static HotkeyBinding Parse(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return Empty;

                bool ctrl = false;
                bool alt = false;
                bool shift = false;
                KeyCode key = KeyCode.None;
                string[] parts = value.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawPart in parts)
                {
                    string part = rawPart.Trim();
                    if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)
                        || part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    {
                        ctrl = true;
                        continue;
                    }
                    if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    {
                        alt = true;
                        continue;
                    }
                    if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    {
                        shift = true;
                        continue;
                    }

                    if (Enum.TryParse(part, true, out KeyCode parsed))
                        key = parsed;
                    else if (part.Length == 1 && char.IsLetter(part[0])
                        && Enum.TryParse(part.ToUpperInvariant(), out parsed))
                        key = parsed;
                }

                return key == KeyCode.None ? Empty : new HotkeyBinding(ctrl, alt, shift, key);
            }

            public bool IsPressed()
            {
                if (!Input.GetKeyDown(_key))
                    return false;

                if (_ctrl && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                    return false;
                if (_alt && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
                    return false;
                if (_shift && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                    return false;
                return true;
            }
        }
    }
}
