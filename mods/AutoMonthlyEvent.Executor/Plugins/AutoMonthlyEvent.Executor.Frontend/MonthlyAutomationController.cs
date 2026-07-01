using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FrameWork;
using FrameWork.UISystem.UIElements;
using Game.Views.EventWindow;
using GameData.Domains.Character.Display;
using GameData.Domains.LifeRecord.GeneralRecord;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using GameData.Domains.World;
using GameData.Domains.World.MonthlyEvent;
using GameData.Serializer;
using GameData.Utilities;
using HarmonyLib;
using UnityEngine;

namespace AutoMonthlyEvent.Executor.Frontend
{
    internal static class MonthlyAutomationController
    {
        private static readonly FieldInfo? RenderInfosField = AccessTools.Field(typeof(UI_MonthlyEvent), "_monthlyEventRenderInfoList");
        private static readonly FieldInfo? IgnoreClickField = AccessTools.Field(typeof(UI_MonthlyEvent), "_ignoreClick");
        private static readonly PropertyInfo? WindowDataProperty = AccessTools.Property(typeof(ViewEventWindow), "Data");
        private static readonly PropertyInfo? WindowCanSelectProperty = AccessTools.Property(typeof(ViewEventWindow), "CanSelect");
        private static readonly MethodInfo? SelectOptionMethod = AccessTools.Method(typeof(ViewEventWindow), "SelectOptionByOptionKey");

        private static bool _running;
        private static bool _handling;
        private static bool _requestingCollection;
        private static bool _relationPending;
        private static bool _manualTakeover;
        private static int _currentOffset;
        private static SupportedMonthlyEvent? _current;
        private static UI_MonthlyEvent? _monthlyUi;
        private static string _selectedToken = string.Empty;
        private static string _resolvedRequestOption = string.Empty;
        private static string _submittedNameToken = string.Empty;
        private static CanvasGroup? _hiddenWindow;

        public static void Install(Harmony harmony)
        {
            Patch(harmony, typeof(UI_MonthlyEvent), "RefreshMonthlyEventScroll", nameof(OnMonthlyRefresh));
            Patch(harmony, typeof(UI_MonthlyEvent), "OnFinishHandlingMonthlyEvent", nameof(OnMonthlyFinished));
            Patch(harmony, typeof(ViewEventWindow), "Update", nameof(OnEventWindowUpdate));
            ActionLogger.Debug("monthly-install", string.Empty, "monthly", "指定月度事件接管补丁已安装");
        }

        public static void Reset()
        {
            _running = _handling = _requestingCollection = _relationPending = _manualTakeover = false;
            _current = null;
            _monthlyUi = null;
            _selectedToken = _resolvedRequestOption = string.Empty;
            _submittedNameToken = string.Empty;
        }

        private static void Patch(Harmony harmony, Type type, string methodName, string postfixName)
        {
            MethodInfo? target = AccessTools.Method(type, methodName);
            MethodInfo? postfix = AccessTools.Method(typeof(MonthlyAutomationController), postfixName);
            if (target == null || postfix == null)
            {
                ActionLogger.Debug("monthly-patch-disabled", string.Empty, "monthly", $"{type.FullName}.{methodName} 不存在");
                return;
            }
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static void OnMonthlyRefresh(UI_MonthlyEvent __instance)
        {
            try
            {
                _monthlyUi = __instance;
                MonthlySettingsPanel.EnsureButton(__instance);
                if (_running)
                {
                    SetIgnoreClick(true);
                    if (!_handling && !_requestingCollection)
                        StartNextFromUi();
                    return;
                }
                StartNextFromUi();
            }
            catch (Exception ex)
            {
                FailToManual("月度列表刷新处理失败", ex);
            }
        }

        private static void StartNextFromUi()
        {
            List<MonthlyEventRenderInfo>? infos = RenderInfosField?.GetValue(_monthlyUi) as List<MonthlyEventRenderInfo>;
            if (infos == null)
                return;
            MonthlyEventRenderInfo candidate = infos
                .Where(x => SupportedMonthlyEvents.TryGet(x.RecordType, out SupportedMonthlyEvent item)
                    && SupportedMonthlyEvents.IsEnabled(item, MonthlyAutomationSettings.Current))
                .OrderByDescending(x => x.Offset)
                .FirstOrDefault();
            if (candidate == null)
            {
                CompleteQueue();
                return;
            }
            if (!SupportedMonthlyEvents.TryGet(candidate.RecordType, out SupportedMonthlyEvent item))
                return;

            _running = true;
            _handling = true;
            _manualTakeover = false;
            _relationPending = false;
            _selectedToken = _resolvedRequestOption = string.Empty;
            _current = item;
            _currentOffset = candidate.Offset;
            SetIgnoreClick(true);
            HideMonthlyUi();
            ActionLogger.Debug("monthly-open", item.EventGuid, item.Name, $"recordType={item.RecordType}; offset={_currentOffset}");
            WorldDomainMethod.Call.HandleMonthlyEvent(_currentOffset);
        }

        private static void OnEventWindowUpdate(ViewEventWindow __instance)
        {
            if (!_running || !_handling || _manualTakeover || _current == null)
                return;
            if (!(WindowCanSelectProperty?.GetValue(__instance) is bool canSelect) || !canSelect)
                return;
            TaiwuEventDisplayData? data = WindowDataProperty?.GetValue(__instance) as TaiwuEventDisplayData;
            if (data?.EventOptionInfos == null || data.EventOptionInfos.Count == 0)
                return;
            HideAutomaticWindow(__instance);

            if (!string.Equals(data.EventGuid, _current.EventGuid, StringComparison.OrdinalIgnoreCase))
            {
                // 取名、结果页等后续页面继续交给现有专用处理器；纯确认页可自动选择唯一非退出选项。
                if (TryGetOnlyAvailableOption(data, out EventOptionInfo continuation))
                    Select(__instance, data, continuation, "后续确认页");
                return;
            }

            if (_current.Handler == MonthlyHandlerKind.RelationRequest)
            {
                HandleRequest(__instance, data);
                return;
            }

            EventOptionInfo option;
            string reason;
            switch (_current.Handler)
            {
                case MonthlyHandlerKind.BirthAndNaming:
                    if (!TryGetOwnSurnameOrFirst(data, out option))
                    {
                        FailToManual("没有可用的生育选项", null);
                        return;
                    }
                    reason = "优先随太吾姓，否则选择首个可用选项";
                    break;
                case MonthlyHandlerKind.Adoption:
                    if (!TryGetAdoptionOption(data, out option, out reason))
                    {
                        FailToManual("无法识别收养选项", null);
                        return;
                    }
                    break;
                case MonthlyHandlerKind.DefaultChoice:
                    int choice = GetConfiguredChoice(_current.RecordType);
                    if (!TryGetChoice(data, choice, out option))
                    {
                        FailToManual($"配置的第 {choice} 个选项不可用", null);
                        return;
                    }
                    reason = $"配置默认选项 {choice}";
                    break;
                default:
                    if (!TryGetFirstAvailable(data, out option))
                    {
                        FailToManual("没有可用的结果继续选项", null);
                        return;
                    }
                    reason = "自动略过结果";
                    break;
            }
            Select(__instance, data, option, reason);
        }

        public static bool TryHandleNameInput(EventModel eventModel, TaiwuEventDisplayData data)
        {
            if (!_running || !_handling || _current?.Handler != MonthlyHandlerKind.BirthAndNaming)
                return false;
            MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
            string name = (s.GenerationCharacter ?? string.Empty) + (s.GivenNameCharacter ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name) || data.ExtraData?.InputRequestData == null)
                return false;
            string token = (data.EventGuid ?? string.Empty) + "|" + name;
            if (_submittedNameToken == token)
                return true;
            try
            {
                eventModel.SetInputResult(name);
                _submittedNameToken = token;
                ActionLogger.Debug("monthly-name-input", data.EventGuid ?? string.Empty, "生育与取名", "已填写名字：" + name);
                return false; // 仍由窗口更新补丁选择确认按钮。
            }
            catch (Exception ex)
            {
                FailToManual("自动填写名字失败", ex);
                return true;
            }
        }

        private static void HandleRequest(ViewEventWindow window, TaiwuEventDisplayData data)
        {
            if (!string.IsNullOrEmpty(_resolvedRequestOption))
            {
                EventOptionInfo selected = data.EventOptionInfos.FirstOrDefault(x => x.OptionKey == _resolvedRequestOption && x.OptionState == 0);
                if (!string.IsNullOrEmpty(selected.OptionKey))
                    Select(window, data, selected, "关系筛选结果");
                else
                    FailToManual("关系筛选目标选项已不可用", null);
                return;
            }
            if (_relationPending)
                return;
            if (!EventClassifier.TryFindRequestOptions(data, out EventOptionInfo agree, out EventOptionInfo reject, out string reason))
            {
                FailToManual("无法识别请求同意/婉拒选项：" + reason, null);
                return;
            }
            CharacterDisplayData? requester = RelationConditionResolver.FindRequester(data);
            if (requester == null)
            {
                _resolvedRequestOption = reject.OptionKey ?? string.Empty;
                return;
            }
            _relationPending = true;
            MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
            RelationConditionResolver.ResolveMonthly(requester, s.RelationMode, s.FavorabilityThreshold, result =>
            {
                _relationPending = false;
                _resolvedRequestOption = result.Resolved && result.ShouldGive
                    ? agree.OptionKey ?? string.Empty
                    : reject.OptionKey ?? string.Empty;
                ActionLogger.Debug("monthly-request-filter", _current?.EventGuid ?? string.Empty, "request", result.Reason);
            });
        }

        private static void OnMonthlyFinished(UI_MonthlyEvent __instance)
        {
            if (!_running || !_handling)
                return;
            _monthlyUi = __instance;
            _handling = false;
            _hiddenWindow = null;
            ActionLogger.Debug("monthly-completed", _current?.EventGuid ?? string.Empty, _current?.Name ?? "monthly", "原版事件链已完成并请求刷新集合");
            RequestFreshCollection();
        }

        private static void RequestFreshCollection()
        {
            if (_requestingCollection)
                return;
            _requestingCollection = true;
            WorldDomainMethod.AsyncCall.GetMonthlyEventCollection(null, OnCollectionReceived);
        }

        private static void OnCollectionReceived(int offset, RawDataPool pool)
        {
            _requestingCollection = false;
            MonthlyEventCollection? collection = null;
            Serializer.Deserialize(pool, offset, ref collection);
            var infos = new List<MonthlyEventRenderInfo>();
            var args = new ArgumentCollection();
            collection?.GetRenderInfos(infos, args);
            if (infos.Count == 0)
            {
                CompleteQueue();
                return;
            }
            try
            {
                UIManager.Instance.HideUI(UIElement.MonthNotify);
                UIElement.MonthlyEvent.SetOnInitArgs(EasyPool.Get<ArgumentBox>()
                    .Set("NeedSave", true)
                    .SetObject("RenderInfoList", infos)
                    .SetObject("Arguments", args));
                UIManager.Instance.MaskUI(UIElement.MonthlyEvent);
            }
            catch (Exception ex)
            {
                FailToManual("无法刷新月度事件列表", ex);
            }
        }

        private static void CompleteQueue()
        {
            _running = _handling = _requestingCollection = false;
            _current = null;
            SetIgnoreClick(false);
            ActionLogger.Debug("monthly-queue-completed", string.Empty, "monthly", "指定事件均已处理，显示剩余原版月度事件");
        }

        private static void FailToManual(string reason, Exception? ex)
        {
            _manualTakeover = true;
            RestoreAutomaticWindow();
            SetIgnoreClick(false);
            ActionLogger.Debug("monthly-manual-takeover", _current?.EventGuid ?? string.Empty, _current?.Name ?? "monthly",
                reason + (ex == null ? string.Empty : "；" + ex));
        }

        private static void Select(ViewEventWindow window, TaiwuEventDisplayData data, EventOptionInfo option, string reason)
        {
            string token = (data.EventGuid ?? string.Empty) + "|" + (option.OptionKey ?? string.Empty);
            if (_selectedToken == token)
                return;
            _selectedToken = token;
            ActionLogger.Debug("monthly-select", data.EventGuid ?? string.Empty, _current?.Name ?? "monthly",
                $"{reason}；{option.OptionKey}/{option.OptionContent}");
            SelectOptionMethod?.Invoke(window, new object[] { option.OptionKey });
        }

        private static bool TryGetChoice(TaiwuEventDisplayData data, int oneBased, out EventOptionInfo option)
        {
            var available = data.EventOptionInfos.Where((x, i) => x.OptionState == 0 && i != data.EscOptionIndex).ToList();
            if (available.Count == 0)
                available = data.EventOptionInfos.Where(x => x.OptionState == 0).ToList();
            option = oneBased > 0 && oneBased <= available.Count ? available[oneBased - 1] : default;
            return !string.IsNullOrEmpty(option.OptionKey);
        }

        private static bool TryGetFirstAvailable(TaiwuEventDisplayData data, out EventOptionInfo option) => TryGetChoice(data, 1, out option);
        private static bool TryGetOnlyAvailableOption(TaiwuEventDisplayData data, out EventOptionInfo option)
        {
            var available = data.EventOptionInfos.Where(x => x.OptionState == 0).ToList();
            option = available.Count == 1 ? available[0] : default;
            return available.Count == 1;
        }

        private static bool TryGetOwnSurnameOrFirst(TaiwuEventDisplayData data, out EventOptionInfo option)
        {
            option = data.EventOptionInfos.FirstOrDefault(x => x.OptionState == 0
                && (x.OptionKey == "Option_-1455740954"
                    || ((x.OptionContent ?? string.Empty).Contains("太吾") && (x.OptionContent ?? string.Empty).Contains("姓"))));
            return !string.IsNullOrEmpty(option.OptionKey) || TryGetFirstAvailable(data, out option);
        }

        private static bool TryGetAdoptionOption(TaiwuEventDisplayData data, out EventOptionInfo option, out string reason)
        {
            MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
            bool adopt = s.AdoptionMode == 1;
            if (s.AdoptionMode == 2)
            {
                CharacterDisplayData? child = data.TargetCharacter ?? data.MainCharacter;
                adopt = child != null && child.CurrAge <= s.AdoptionMaxAge && (child.BehaviorType == 0 || child.BehaviorType == 1 || child.BehaviorType == 2);
            }
            string key = adopt ? "Option_1751715976" : "Option_-899805435";
            option = data.EventOptionInfos.FirstOrDefault(x => x.OptionState == 0 && x.OptionKey == key);
            reason = adopt ? "收养策略命中" : "拒绝收养策略命中";
            return !string.IsNullOrEmpty(option.OptionKey);
        }

        private static int GetConfiguredChoice(short recordType)
        {
            MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
            if (recordType == 13) return s.PrenatalChoice;
            if (recordType == 109) return s.BenevolenceChoice;
            if (recordType == 110) return s.RecruitChoice;
            if (recordType == 280) return s.GuidanceChoice;
            if (recordType == 281) return s.PregnancyChoice;
            return 1;
        }

        private static void SetIgnoreClick(bool value)
        {
            try { IgnoreClickField?.SetValue(_monthlyUi, value); } catch { }
        }

        private static void HideMonthlyUi()
        {
            try
            {
                UIManager.Instance.HideUI(UIElement.MonthlyEvent);
                UIManager.Instance.HideUI(UIElement.MonthNotify);
            }
            catch { }
        }

        private static void HideAutomaticWindow(ViewEventWindow window)
        {
            if (_hiddenWindow != null)
                return;
            try
            {
                CanvasGroup group = window.gameObject.GetComponent<CanvasGroup>() ?? window.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                _hiddenWindow = group;
            }
            catch (Exception ex)
            {
                ActionLogger.Debug("monthly-window-hide-failed", _current?.EventGuid ?? string.Empty, _current?.Name ?? "monthly", ex.Message);
            }
        }

        private static void RestoreAutomaticWindow()
        {
            try
            {
                if (_hiddenWindow != null)
                {
                    _hiddenWindow.alpha = 1f;
                    _hiddenWindow.interactable = true;
                    _hiddenWindow.blocksRaycasts = true;
                }
            }
            catch { }
            _hiddenWindow = null;
        }
    }
}
