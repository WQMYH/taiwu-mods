using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Config;
using GameData.Domains.LifeRecord.GeneralRecord;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using GameData.Domains.World.MonthlyEvent;
using GameData.GameDataBridge;
using GameData.Serializer;
using GameData.Utilities;
using HarmonyLib;
using UnityEngine;

namespace RuntimeInfoGrabber.Frontend
{
    internal static class DiscoveryDumper
    {
        private const string ConfigFileName = "Config.lua";
        private const string MonthlyCatalogFileName = "monthly_events_catalog.json";
        private const string MonthlyEventGuidIndexFileName = "monthly_event_guid_index.json";
        private const string RuntimeMonthlyEventsFileName = "runtime_monthly_events.jsonl";
        private const string RuntimeEventOptionsFileName = "runtime_event_options.jsonl";
        private const string RuntimeMonthlyEventOptionsFileName = "runtime_monthly_event_options.jsonl";
        private const string RuntimeMonthlyEventWindowsFileName = "runtime_monthly_event_windows.jsonl";
        private const string RuntimeMonthlyEventFlowFileName = "runtime_monthly_event_flow.jsonl";
        private const string RuntimeMonthlyEventCandidatesFileName = "runtime_monthly_event_candidates.jsonl";

        private static bool _discoveryMode = true;
        private static bool _dumpToJson = true;
        private static bool _logVerbose;
        private static string _dumpDirectoryName = "Dump_out";
        private static string _lastEventOptionsSignature = string.Empty;
        private static string _lastMonthlyEventOptionsSignature = string.Empty;
        private static string _lastMonthlyEventWindowSignature = string.Empty;
        private static string _lastMonthlyEventFlowSignature = string.Empty;
        private static string _lastMonthlyEventCandidateSignature = string.Empty;
        private static int _flowSequence;
        private static DateTime _lastMonthlyCollectionObservedAt = DateTime.MinValue;
        private static readonly HashSet<string> _monthlyEventGuids = new HashSet<string>();
        private static readonly Dictionary<string, List<MonthlyEventItem>> _monthlyEventsByGuid = new Dictionary<string, List<MonthlyEventItem>>();
        private static readonly Dictionary<string, List<string>> _recentMonthlyEventContextsByGuid = new Dictionary<string, List<string>>();

        private static string GameRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string ConfigFilePath => Path.Combine(GameRootPath, "Mod", "RuntimeInfoGrabber", ConfigFileName);
        private static string DumpDirectoryPath => Path.Combine(GameRootPath, _dumpDirectoryName);

        public static bool DiscoveryEnabled => _discoveryMode && _dumpToJson;

        public static void InstallPatches(Harmony harmony)
        {
            Type? monthNotifyType = FindTypeBySimpleName("UI_MonthNotify");
            if (monthNotifyType == null)
            {
                AdaptableLog.Warning("[RuntimeInfoGrabber] UI_MonthNotify type not found; monthly event runtime dump is disabled.");
            }
            else
            {
                var onNotifyGameData = AccessTools.Method(monthNotifyType, "OnNotifyGameData");
                if (onNotifyGameData == null)
                {
                    AdaptableLog.Warning("[RuntimeInfoGrabber] UI_MonthNotify.OnNotifyGameData not found; monthly event runtime dump is disabled.");
                }
                else
                {
                    harmony.Patch(onNotifyGameData,
                        postfix: new HarmonyMethod(typeof(UI_MonthNotify_OnNotifyGameData_Patch), nameof(UI_MonthNotify_OnNotifyGameData_Patch.Postfix)));
                }
            }

            var eventModelNotify = AccessTools.Method(typeof(EventModel), "OnNotifyGameData");
            if (eventModelNotify == null)
            {
                AdaptableLog.Warning("[RuntimeInfoGrabber] EventModel.OnNotifyGameData not found; event option runtime dump is disabled.");
            }
            else
            {
                harmony.Patch(eventModelNotify,
                    postfix: new HarmonyMethod(typeof(EventModel_OnNotifyGameData_Patch), nameof(EventModel_OnNotifyGameData_Patch.Postfix)));
            }
        }

        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    AdaptableLog.Warning($"[RuntimeInfoGrabber] Config not found at {ConfigFilePath}; using discovery defaults.");
                    return;
                }

                string content = File.ReadAllText(ConfigFilePath);
                _discoveryMode = ReadBool(content, "DiscoveryMode", true);
                _dumpToJson = ReadBool(content, "DumpToJson", true);
                _logVerbose = ReadBool(content, "LogVerbose", false);
                _dumpDirectoryName = ReadString(content, "DumpDirectory", "Dump_out");
                if (string.IsNullOrWhiteSpace(_dumpDirectoryName))
                    _dumpDirectoryName = "Dump_out";

                AdaptableLog.Info($"[RuntimeInfoGrabber] Discovery config loaded. DiscoveryMode={_discoveryMode}, DumpToJson={_dumpToJson}, DumpDirectory={_dumpDirectoryName}");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to load discovery config: {ex}");
            }
        }

        public static void EnsureDumpDirectory()
        {
            if (!DiscoveryEnabled)
                return;

            Directory.CreateDirectory(DumpDirectoryPath);
        }

        public static void ExportStaticCatalogs()
        {
            if (!DiscoveryEnabled)
                return;

            try
            {
                EnsureDumpDirectory();
                RefreshMonthlyEventGuidIndex();
                File.WriteAllText(Path.Combine(DumpDirectoryPath, MonthlyCatalogFileName), BuildMonthlyEventsCatalogJson(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(DumpDirectoryPath, MonthlyEventGuidIndexFileName), BuildMonthlyEventGuidIndexJson(), Encoding.UTF8);
                AdaptableLog.Info($"[RuntimeInfoGrabber] Static catalogs dumped to {DumpDirectoryPath}");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to export static catalogs: {ex}");
            }
        }

        public static void DumpMonthlyEventCollection(List<NotificationWrapper> notifications)
        {
            if (!DiscoveryEnabled || notifications == null)
                return;

            try
            {
                foreach (NotificationWrapper wrapper in notifications)
                {
                    var notification = wrapper.Notification;
                    if (notification.Type != 1 || notification.DomainId != 1 || notification.MethodId != 5)
                        continue;

                    MonthlyEventCollection? collection = null;
                    Serializer.Deserialize(wrapper.DataPool, notification.ValueOffset, ref collection);
                    if (collection == null)
                        continue;

                    DumpMonthlyEventCollection(collection);
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to dump runtime monthly events: {ex}");
            }
        }

        public static void DumpEventOptions(EventModel eventModel)
        {
            if (!DiscoveryEnabled || eventModel?.DisplayingEventData == null)
                return;

            try
            {
                TaiwuEventDisplayData data = eventModel.DisplayingEventData;
                if (string.IsNullOrEmpty(data.EventGuid))
                    return;

                EnsureMonthlyEventGuidIndex();
                string signature = BuildEventOptionsSignature(data);
                bool isMonthlyGuid = _monthlyEventGuids.Contains(data.EventGuid);
                bool nearMonthly = IsNearMonthlyProcess();
                string candidateType = GetMonthlyEventCandidateType(data);
                bool isCandidate = !string.IsNullOrEmpty(candidateType);
                string candidateReason = isCandidate ? BuildMonthlyEventCandidateReason(data, candidateType) : string.Empty;

                if (signature != _lastEventOptionsSignature)
                {
                    _lastEventOptionsSignature = signature;
                    string line = BuildEventOptionsJsonLine(data, isMonthlyGuid, nearMonthly, isCandidate, candidateType, candidateReason);
                    AppendLine(RuntimeEventOptionsFileName, line);

                    if (_logVerbose)
                        AdaptableLog.Info($"[RuntimeInfoGrabber] Event options dumped. EventGuid={data.EventGuid}, Options={data.EventOptionInfos?.Count ?? 0}");
                }

                if (isMonthlyGuid && signature != _lastMonthlyEventOptionsSignature)
                {
                    _lastMonthlyEventOptionsSignature = signature;
                    AppendLine(RuntimeMonthlyEventOptionsFileName, BuildMonthlyEventOptionsJsonLine(data));

                    AdaptableLog.Info($"[RuntimeInfoGrabber] Monthly event options dumped. EventGuid={data.EventGuid}, Options={data.EventOptionInfos?.Count ?? 0}");
                }

                if (isMonthlyGuid && signature != _lastMonthlyEventWindowSignature)
                {
                    _lastMonthlyEventWindowSignature = signature;
                    AppendLine(RuntimeMonthlyEventWindowsFileName, BuildMonthlyEventWindowJsonLine(data, isMonthlyGuid, nearMonthly));
                }

                if (isCandidate && signature != _lastMonthlyEventCandidateSignature)
                {
                    _lastMonthlyEventCandidateSignature = signature;
                    AppendLine(RuntimeMonthlyEventCandidatesFileName, BuildMonthlyEventCandidateJsonLine(data, candidateType, candidateReason, nearMonthly));
                }

                if ((isMonthlyGuid || nearMonthly || isCandidate) && signature != _lastMonthlyEventFlowSignature)
                {
                    _lastMonthlyEventFlowSignature = signature;
                    AppendLine(RuntimeMonthlyEventFlowFileName, BuildMonthlyEventFlowJsonLine(data, isMonthlyGuid, nearMonthly, isCandidate, candidateType, candidateReason));
                }
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[RuntimeInfoGrabber] Failed to dump runtime event options: {ex}");
            }
        }

        private static void DumpMonthlyEventCollection(MonthlyEventCollection collection)
        {
            _lastMonthlyCollectionObservedAt = DateTime.Now;
            var renderInfos = new List<MonthlyEventRenderInfo>();
            var arguments = new ArgumentCollection();
            collection.GetRenderInfos(renderInfos, arguments);

            foreach (MonthlyEventRenderInfo info in renderInfos)
            {
                MonthlyEventItem item = MonthlyEvent.Instance.GetItem(info.RecordType);
                string line = BuildRuntimeMonthlyEventJsonLine(info, item);
                AppendLine(RuntimeMonthlyEventsFileName, line);
                RememberMonthlyEventContext(info.EventGuid, line);
                AppendLine(RuntimeMonthlyEventFlowFileName, BuildMonthlyEventFlowJsonLine(info, item));
                AdaptableLog.Info($"[RuntimeInfoGrabber] MonthlyEvent offset={info.Offset}, recordType={info.RecordType}, type={item?.Type.ToString() ?? "Unknown"}, eventGuid={info.EventGuid}");
            }
        }

        private static void RefreshMonthlyEventGuidIndex()
        {
            _monthlyEventGuids.Clear();
            _monthlyEventsByGuid.Clear();

            foreach (MonthlyEventItem item in MonthlyEvent.Instance)
            {
                if (item == null || string.IsNullOrEmpty(item.Event))
                    continue;

                _monthlyEventGuids.Add(item.Event);
                if (!_monthlyEventsByGuid.TryGetValue(item.Event, out List<MonthlyEventItem> items))
                {
                    items = new List<MonthlyEventItem>();
                    _monthlyEventsByGuid.Add(item.Event, items);
                }

                items.Add(item);
            }
        }

        private static Type? FindTypeBySimpleName(string typeName)
        {
            Type? type = AccessTools.TypeByName(typeName);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type candidate in assembly.GetTypes())
                    {
                        if (candidate.Name == typeName || candidate.FullName == typeName)
                            return candidate;
                    }
                }
                catch
                {
                    // Some Unity/runtime assemblies can fail type enumeration; skip them.
                }
            }

            return null;
        }

        private static void EnsureMonthlyEventGuidIndex()
        {
            if (_monthlyEventGuids.Count == 0)
                RefreshMonthlyEventGuidIndex();
        }

        private static string BuildMonthlyEventsCatalogJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"generatedAt\": " + JsonString(Timestamp()) + ",");
            sb.AppendLine("  \"items\": [");

            bool first = true;
            foreach (MonthlyEventItem item in MonthlyEvent.Instance)
            {
                if (item == null)
                    continue;

                if (!first)
                    sb.AppendLine(",");
                first = false;

                sb.Append("    {");
                AppendJsonProperty(sb, "templateId", item.TemplateId, false);
                AppendJsonProperty(sb, "name", item.Name, true);
                AppendJsonProperty(sb, "type", item.Type.ToString(), true);
                AppendJsonProperty(sb, "eventGuid", item.Event, true);
                AppendJsonProperty(sb, "desc", item.Desc, true);
                AppendJsonArrayProperty(sb, "parameters", item.Parameters, true);
                AppendJsonProperty(sb, "score", item.Score, true);
                AppendJsonProperty(sb, "node", item.Node, true);
                sb.Append(" }");
            }

            sb.AppendLine();
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildMonthlyEventGuidIndexJson()
        {
            EnsureMonthlyEventGuidIndex();

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"generatedAt\": " + JsonString(Timestamp()) + ",");
            sb.AppendLine("  \"source\": \"Config.MonthlyEvent.Event\",");
            sb.AppendLine("  \"items\": [");

            bool first = true;
            foreach (KeyValuePair<string, List<MonthlyEventItem>> pair in _monthlyEventsByGuid)
            {
                if (!first)
                    sb.AppendLine(",");
                first = false;

                sb.Append("    {");
                AppendJsonProperty(sb, "eventGuid", pair.Key, false);
                sb.Append(",\"monthlyEvents\":[");
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    MonthlyEventItem item = pair.Value[i];
                    if (i > 0)
                        sb.Append(",");
                    AppendMonthlyEventSummaryObject(sb, item);
                }

                sb.Append("] }");
            }

            sb.AppendLine();
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildRuntimeMonthlyEventJsonLine(MonthlyEventRenderInfo info, MonthlyEventItem? item)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "offset", info.Offset, true);
            AppendJsonProperty(sb, "recordType", info.RecordType, true);
            AppendJsonProperty(sb, "type", item?.Type.ToString(), true);
            AppendJsonProperty(sb, "eventGuid", info.EventGuid, true);
            AppendJsonProperty(sb, "name", item?.Name, true);
            AppendJsonProperty(sb, "desc", item?.Desc, true);
            AppendJsonArrayProperty(sb, "parameters", item?.Parameters, true);
            AppendRenderArgumentsProperty(sb, "arguments", info.Arguments, true);
            AppendJsonProperty(sb, "text", info.Text, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildEventOptionsJsonLine(
            TaiwuEventDisplayData data,
            bool isMonthlyEventGuid,
            bool nearMonthlyProcess,
            bool isMonthlyEventCandidate,
            string candidateType,
            string candidateReason)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "eventGuid", data.EventGuid, true);
            AppendJsonProperty(sb, "isMonthlyEventGuid", isMonthlyEventGuid, true);
            AppendJsonProperty(sb, "nearMonthlyProcess", nearMonthlyProcess, true);
            AppendJsonProperty(sb, "isMonthlyEventCandidate", isMonthlyEventCandidate, true);
            AppendJsonProperty(sb, "candidateType", candidateType, true);
            AppendJsonProperty(sb, "candidateReason", candidateReason, true);
            AppendJsonProperty(sb, "classification", ClassifyEventWindow(data, isMonthlyEventGuid, nearMonthlyProcess, isMonthlyEventCandidate, candidateType), true);
            AppendJsonProperty(sb, "stage", InferEventWindowStage(data), true);
            AppendJsonProperty(sb, "optionCount", data.EventOptionInfos?.Count ?? 0, true);
            AppendJsonProperty(sb, "hasOptions", (data.EventOptionInfos?.Count ?? 0) > 0, true);
            AppendJsonProperty(sb, "eventContent", data.EventContent, true);
            AppendJsonProperty(sb, "mainCharacterId", data.MainCharacter?.CharacterId ?? -1, true);
            AppendJsonProperty(sb, "targetCharacterId", data.TargetCharacter?.CharacterId ?? -1, true);
            var options = data.EventOptionInfos;
            sb.Append(",\"options\":[");

            for (int i = 0; i < (options?.Count ?? 0); i++)
            {
                EventOptionInfo option = options![i];
                if (i > 0)
                    sb.Append(",");
                sb.Append("{");
                AppendJsonProperty(sb, "optionKey", option.OptionKey, false);
                AppendJsonProperty(sb, "optionContent", option.OptionContent, true);
                AppendJsonProperty(sb, "optionType", option.OptionType, true);
                AppendJsonProperty(sb, "optionState", option.OptionState, true);
                AppendJsonProperty(sb, "behavior", option.Behavior, true);
                sb.Append("}");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static string BuildMonthlyEventCandidateJsonLine(TaiwuEventDisplayData data, string candidateType, string candidateReason, bool nearMonthlyProcess)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "sequenceId", NextFlowSequence(), true);
            AppendJsonProperty(sb, "source", "EventModel", true);
            AppendJsonProperty(sb, "eventGuid", data.EventGuid, true);
            AppendJsonProperty(sb, "isMonthlyEventGuid", false, true);
            AppendJsonProperty(sb, "nearMonthlyProcess", nearMonthlyProcess, true);
            AppendJsonProperty(sb, "isMonthlyEventCandidate", true, true);
            AppendJsonProperty(sb, "candidateType", candidateType, true);
            AppendJsonProperty(sb, "candidateReason", candidateReason, true);
            AppendJsonProperty(sb, "classification", ClassifyEventWindow(data, false, nearMonthlyProcess, true, candidateType), true);
            AppendJsonProperty(sb, "stage", InferEventWindowStage(data), true);
            AppendJsonProperty(sb, "optionCount", data.EventOptionInfos?.Count ?? 0, true);
            AppendJsonProperty(sb, "hasOptions", (data.EventOptionInfos?.Count ?? 0) > 0, true);
            AppendJsonProperty(sb, "eventContent", data.EventContent, true);
            AppendJsonProperty(sb, "mainCharacterId", data.MainCharacter?.CharacterId ?? -1, true);
            AppendJsonProperty(sb, "targetCharacterId", data.TargetCharacter?.CharacterId ?? -1, true);
            AppendEventOptionsArrayProperty(sb, data, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildMonthlyEventWindowJsonLine(TaiwuEventDisplayData data, bool isMonthlyEventGuid, bool nearMonthlyProcess)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "sequenceId", NextFlowSequence(), true);
            AppendJsonProperty(sb, "source", "EventModel", true);
            AppendJsonProperty(sb, "eventGuid", data.EventGuid, true);
            AppendJsonProperty(sb, "isMonthlyEventGuid", isMonthlyEventGuid, true);
            AppendJsonProperty(sb, "nearMonthlyProcess", nearMonthlyProcess, true);
            AppendJsonProperty(sb, "classification", ClassifyEventWindow(data, isMonthlyEventGuid, nearMonthlyProcess, false, string.Empty), true);
            AppendJsonProperty(sb, "stage", InferEventWindowStage(data), true);
            AppendJsonProperty(sb, "optionCount", data.EventOptionInfos?.Count ?? 0, true);
            AppendJsonProperty(sb, "hasOptions", (data.EventOptionInfos?.Count ?? 0) > 0, true);
            AppendJsonProperty(sb, "eventContent", data.EventContent, true);
            AppendJsonProperty(sb, "mainCharacterId", data.MainCharacter?.CharacterId ?? -1, true);
            AppendJsonProperty(sb, "targetCharacterId", data.TargetCharacter?.CharacterId ?? -1, true);
            AppendEventOptionsArrayProperty(sb, data, true);
            AppendMonthlyEventSummariesProperty(sb, "matchedMonthlyEvents", data.EventGuid, true);
            AppendRecentMonthlyEventContextsProperty(sb, "recentRuntimeMonthlyEvents", data.EventGuid, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildMonthlyEventFlowJsonLine(
            TaiwuEventDisplayData data,
            bool isMonthlyEventGuid,
            bool nearMonthlyProcess,
            bool isMonthlyEventCandidate,
            string candidateType,
            string candidateReason)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "sequenceId", NextFlowSequence(), true);
            AppendJsonProperty(sb, "source", "EventModel", true);
            AppendJsonProperty(sb, "eventGuid", data.EventGuid, true);
            AppendJsonProperty(sb, "isMonthlyEventGuid", isMonthlyEventGuid, true);
            AppendJsonProperty(sb, "nearMonthlyProcess", nearMonthlyProcess, true);
            AppendJsonProperty(sb, "isMonthlyEventCandidate", isMonthlyEventCandidate, true);
            AppendJsonProperty(sb, "candidateType", candidateType, true);
            AppendJsonProperty(sb, "candidateReason", candidateReason, true);
            AppendJsonProperty(sb, "classification", ClassifyEventWindow(data, isMonthlyEventGuid, nearMonthlyProcess, isMonthlyEventCandidate, candidateType), true);
            AppendJsonProperty(sb, "stage", InferEventWindowStage(data), true);
            AppendJsonProperty(sb, "optionCount", data.EventOptionInfos?.Count ?? 0, true);
            AppendJsonProperty(sb, "hasOptions", (data.EventOptionInfos?.Count ?? 0) > 0, true);
            AppendJsonProperty(sb, "eventContent", data.EventContent, true);
            AppendEventOptionsArrayProperty(sb, data, true);
            AppendMonthlyEventSummariesProperty(sb, "matchedMonthlyEvents", data.EventGuid, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildMonthlyEventFlowJsonLine(MonthlyEventRenderInfo info, MonthlyEventItem? item)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "sequenceId", NextFlowSequence(), true);
            AppendJsonProperty(sb, "source", "MonthlyEventCollection", true);
            AppendJsonProperty(sb, "eventGuid", info.EventGuid, true);
            AppendJsonProperty(sb, "isMonthlyEventGuid", !string.IsNullOrEmpty(info.EventGuid), true);
            AppendJsonProperty(sb, "nearMonthlyProcess", true, true);
            AppendJsonProperty(sb, "classification", "monthlyListItem", true);
            AppendJsonProperty(sb, "stage", "monthlyList", true);
            AppendJsonProperty(sb, "offset", info.Offset, true);
            AppendJsonProperty(sb, "recordType", info.RecordType, true);
            AppendJsonProperty(sb, "templateId", item?.TemplateId ?? -1, true);
            AppendJsonProperty(sb, "name", item?.Name, true);
            AppendJsonProperty(sb, "type", item?.Type.ToString(), true);
            AppendJsonProperty(sb, "desc", item?.Desc, true);
            AppendJsonProperty(sb, "text", info.Text, true);
            AppendJsonArrayProperty(sb, "parameters", item?.Parameters, true);
            AppendRenderArgumentsProperty(sb, "arguments", info.Arguments, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildMonthlyEventOptionsJsonLine(TaiwuEventDisplayData data)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "eventGuid", data.EventGuid, true);
            AppendJsonProperty(sb, "eventContent", data.EventContent, true);
            AppendJsonProperty(sb, "mainCharacterId", data.MainCharacter?.CharacterId ?? -1, true);
            AppendJsonProperty(sb, "targetCharacterId", data.TargetCharacter?.CharacterId ?? -1, true);
            AppendEventOptionsArrayProperty(sb, data, true);
            AppendMonthlyEventSummariesProperty(sb, "matchedMonthlyEvents", data.EventGuid, true);
            AppendRecentMonthlyEventContextsProperty(sb, "recentRuntimeMonthlyEvents", data.EventGuid, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildEventOptionsSignature(TaiwuEventDisplayData data)
        {
            var sb = new StringBuilder();
            sb.Append(data.EventGuid);
            sb.Append("|");
            var options = data.EventOptionInfos;
            for (int i = 0; i < (options?.Count ?? 0); i++)
            {
                sb.Append(options![i].OptionKey);
                sb.Append(":");
                sb.Append(options[i].OptionState);
                sb.Append(";");
            }
            return sb.ToString();
        }

        private static void RememberMonthlyEventContext(string eventGuid, string jsonLine)
        {
            if (string.IsNullOrEmpty(eventGuid) || string.IsNullOrEmpty(jsonLine))
                return;

            if (!_recentMonthlyEventContextsByGuid.TryGetValue(eventGuid, out List<string> contexts))
            {
                contexts = new List<string>();
                _recentMonthlyEventContextsByGuid.Add(eventGuid, contexts);
            }

            contexts.Add(jsonLine);
            while (contexts.Count > 20)
                contexts.RemoveAt(0);
        }

        private static bool IsNearMonthlyProcess()
        {
            return _lastMonthlyCollectionObservedAt != DateTime.MinValue
                && (DateTime.Now - _lastMonthlyCollectionObservedAt).TotalSeconds <= 180;
        }

        private static int NextFlowSequence()
        {
            _flowSequence++;
            return _flowSequence;
        }

        private static string GetMonthlyEventCandidateType(TaiwuEventDisplayData data)
        {
            string text = BuildSearchText(data);

            if (ContainsAny(text, "解囊相助", "希望太吾", "希望能解�?, "身陷困窘", "赠予")
                && ContainsAny(text, "银钱", "木材", "金石", "织物", "药材", "食材"))
                return "resourceRequest";

            if (ContainsAny(text, "赠予")
                && ContainsAny(text, "�?, "�?, "茶酒", "蒙顶黄芽", "郁结难疏", "缓郁�?))
                return "teaWineOrItemRequest";

            if (ContainsAny(text, "切磋", "比试一�?, "愿与你比�?, "同意切磋"))
                return "sparringRequest";

            if (ContainsAny(text, "挑战", "接受挑战", "婉言拒绝")
                && ContainsAny(text, "比武", "较艺", "切磋", "挑战"))
                return "challengeOrContestRequest";

            if (ContainsAny(text, "说与你听", "获得了一些历�?, "江湖之事", "心有所�?))
                return "guidanceResult";

            if (ContainsAny(text, "甚是感动", "关系似乎又亲�?, "颇感失望", "转身离去", "只好如此"))
                return "requestResult";

            return string.Empty;
        }

        private static string BuildMonthlyEventCandidateReason(TaiwuEventDisplayData data, string candidateType)
        {
            int optionCount = data.EventOptionInfos?.Count ?? 0;
            switch (candidateType)
            {
                case "resourceRequest":
                    return $"matched resource request wording; options={optionCount}";
                case "teaWineOrItemRequest":
                    return $"matched tea/wine/item request wording; options={optionCount}";
                case "sparringRequest":
                    return $"matched sparring request wording; options={optionCount}";
                case "challengeOrContestRequest":
                    return $"matched challenge/contest request wording; options={optionCount}";
                case "guidanceResult":
                    return $"matched guidance/experience result wording; options={optionCount}";
                case "requestResult":
                    return $"matched request accept/reject result wording; options={optionCount}";
                default:
                    return $"matched candidate wording; options={optionCount}";
            }
        }

        private static string BuildSearchText(TaiwuEventDisplayData data)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(data.EventContent))
                sb.Append(data.EventContent);

            var options = data.EventOptionInfos;
            for (int i = 0; i < (options?.Count ?? 0); i++)
            {
                sb.Append('\n');
                sb.Append(options![i].OptionContent);
                sb.Append('\n');
                sb.Append(options[i].OptionKey);
            }

            return sb.ToString();
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (string value in values)
            {
                if (text.IndexOf(value, StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }

        private static string ClassifyEventWindow(
            TaiwuEventDisplayData data,
            bool isMonthlyEventGuid,
            bool nearMonthlyProcess,
            bool isMonthlyEventCandidate,
            string candidateType)
        {
            if (isMonthlyEventGuid)
                return "monthlyEventGuid";
            if (isMonthlyEventCandidate)
                return "monthlyEventCandidate:" + candidateType;
            if (nearMonthlyProcess)
                return "nearMonthlyProcess";
            return "genericEvent";
        }

        private static string InferEventWindowStage(TaiwuEventDisplayData data)
        {
            int optionCount = data.EventOptionInfos?.Count ?? 0;
            if (optionCount <= 0)
                return "resultOrNotification";
            if (optionCount == 1)
                return "singleChoiceOrContinue";
            return "promptOrChoice";
        }

        private static void AppendMonthlyEventSummaryObject(StringBuilder sb, MonthlyEventItem item)
        {
            sb.Append("{");
            AppendJsonProperty(sb, "templateId", item.TemplateId, false);
            AppendJsonProperty(sb, "name", item.Name, true);
            AppendJsonProperty(sb, "type", item.Type.ToString(), true);
            AppendJsonProperty(sb, "eventGuid", item.Event, true);
            AppendJsonProperty(sb, "desc", item.Desc, true);
            AppendJsonArrayProperty(sb, "parameters", item.Parameters, true);
            AppendJsonProperty(sb, "score", item.Score, true);
            AppendJsonProperty(sb, "node", item.Node, true);
            sb.Append("}");
        }

        private static void AppendEventOptionsArrayProperty(StringBuilder sb, TaiwuEventDisplayData data, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            var options = data.EventOptionInfos;
            sb.Append("\"options\":[");

            for (int i = 0; i < (options?.Count ?? 0); i++)
            {
                EventOptionInfo option = options![i];
                if (i > 0)
                    sb.Append(",");
                sb.Append("{");
                AppendJsonProperty(sb, "optionKey", option.OptionKey, false);
                AppendJsonProperty(sb, "optionContent", option.OptionContent, true);
                AppendJsonProperty(sb, "optionType", option.OptionType, true);
                AppendJsonProperty(sb, "optionState", option.OptionState, true);
                AppendJsonProperty(sb, "behavior", option.Behavior, true);
                sb.Append("}");
            }

            sb.Append("]");
        }

        private static void AppendMonthlyEventSummariesProperty(StringBuilder sb, string key, string eventGuid, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append(JsonString(key)).Append(":[");

            if (!string.IsNullOrEmpty(eventGuid) && _monthlyEventsByGuid.TryGetValue(eventGuid, out List<MonthlyEventItem> items))
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    AppendMonthlyEventSummaryObject(sb, items[i]);
                }
            }

            sb.Append("]");
        }

        private static void AppendRecentMonthlyEventContextsProperty(StringBuilder sb, string key, string eventGuid, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append(JsonString(key)).Append(":[");

            if (!string.IsNullOrEmpty(eventGuid) && _recentMonthlyEventContextsByGuid.TryGetValue(eventGuid, out List<string> contexts))
            {
                for (int i = 0; i < contexts.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append(contexts[i]);
                }
            }

            sb.Append("]");
        }

        private static void AppendRenderArgumentsProperty(StringBuilder sb, string key, List<(sbyte paramType, int index)> values, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append(JsonString(key)).Append(":[");

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append("{");
                    AppendJsonProperty(sb, "paramType", values[i].paramType, false);
                    AppendJsonProperty(sb, "index", values[i].index, true);
                    sb.Append("}");
                }
            }

            sb.Append("]");
        }

        private static void AppendLine(string fileName, string line)
        {
            EnsureDumpDirectory();
            File.AppendAllText(Path.Combine(DumpDirectoryPath, fileName), line + Environment.NewLine, Encoding.UTF8);
        }

        private static bool ReadBool(string content, string key, bool defaultValue)
        {
            var match = Regex.Match(content, key + @"\s*=\s*(true|false)", RegexOptions.IgnoreCase);
            return match.Success ? string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase) : defaultValue;
        }

        private static string ReadString(string content, string key, string defaultValue)
        {
            var match = Regex.Match(content, key + "\\s*=\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : defaultValue;
        }

        private static string Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private static void AppendJsonProperty(StringBuilder sb, string key, string? value, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append(JsonString(key)).Append(":").Append(JsonString(value));
        }

        private static void AppendJsonProperty(StringBuilder sb, string key, int value, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append(JsonString(key)).Append(":").Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendJsonProperty(StringBuilder sb, string key, bool value, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append(JsonString(key)).Append(":").Append(value ? "true" : "false");
        }

        private static void AppendJsonArrayProperty(StringBuilder sb, string key, IEnumerable<string>? values, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append(JsonString(key)).Append(":[");
            if (values != null)
            {
                bool first = true;
                foreach (string value in values)
                {
                    if (string.IsNullOrEmpty(value))
                        continue;
                    if (!first)
                        sb.Append(",");
                    first = false;
                    sb.Append(JsonString(value));
                }
            }
            sb.Append("]");
        }

        private static string JsonString(string? value)
        {
            if (value == null)
                return "null";

            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(c))
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }

    internal static class UI_MonthNotify_OnNotifyGameData_Patch
    {
        public static void Postfix(List<NotificationWrapper> notifications)
        {
            DiscoveryDumper.DumpMonthlyEventCollection(notifications);
        }
    }

    internal static class EventModel_OnNotifyGameData_Patch
    {
        public static void Postfix(EventModel __instance)
        {
            DiscoveryDumper.DumpEventOptions(__instance);
        }
    }
}
