using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace FullDiscovery.Frontend
{
    internal static class FullDiscoveryDumper
    {
        private const string ConfigFileName = "Config.lua";
        private const string ModDirectoryName = "FullDiscovery";
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags StaticPublic = BindingFlags.Public | BindingFlags.Static;
        private const BindingFlags AnyDeclaredMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static bool _enabled = true;
        private static bool _verboseLog;
        private static bool _exportAllConfigTables = true;
        private static bool _exportFocusedIndexes = true;
        private static bool _recordEventWindows = true;
        private static bool _recordMonthlyCollections = true;
        private static bool _exportApiIndex = true;
        private static string _outputDirectoryName = "Dump_out/FullDiscovery";
        private static string[] _apiNamespaceFilters = { "GameData", "Config", "Game.Views", "TaiwuModdingLib" };
        private static int _maxConfigTables;
        private static int _maxItemsPerTable;
        private static int _maxApiMethods;
        private static string _lastEventSignature = string.Empty;
        private static readonly HashSet<string> _monthlyEventGuids = new HashSet<string>();
        private static readonly Dictionary<string, List<object>> _monthlyEventsByGuid = new Dictionary<string, List<object>>();
        private static readonly List<string> _manifestFiles = new List<string>();

        private static string GameRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string ModPath => Path.Combine(GameRootPath, "Mod", ModDirectoryName);
        private static string ConfigFilePath => Path.Combine(ModPath, ConfigFileName);
        private static string OutputRootPath => Path.Combine(GameRootPath, NormalizeRelativePath(_outputDirectoryName));

        public static bool DiscoveryEnabled => _enabled;

        public static void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    WriteGameWarning($"Config not found at {ConfigFilePath}; using defaults.");
                    return;
                }

                string content = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                _enabled = ReadBool(content, "EnableDiscovery", true);
                _outputDirectoryName = ReadString(content, "OutputDirectory", "Dump_out/FullDiscovery");
                _verboseLog = ReadBool(content, "VerboseLog", false);
                _exportAllConfigTables = ReadBool(content, "ExportAllConfigTables", true);
                _exportFocusedIndexes = ReadBool(content, "ExportFocusedIndexes", true);
                _recordEventWindows = ReadBool(content, "RecordEventWindows", true);
                _recordMonthlyCollections = ReadBool(content, "RecordMonthlyCollections", true);
                _exportApiIndex = ReadBool(content, "ExportApiIndex", true);
                _apiNamespaceFilters = SplitCsv(ReadString(content, "ApiNamespaceFilters", "GameData,Config,Game.Views,TaiwuModdingLib"));
                _maxConfigTables = ReadInt(content, "MaxConfigTables", 0);
                _maxItemsPerTable = ReadInt(content, "MaxItemsPerTable", 0);
                _maxApiMethods = ReadInt(content, "MaxApiMethods", 0);

                if (string.IsNullOrWhiteSpace(_outputDirectoryName))
                    _outputDirectoryName = "Dump_out/FullDiscovery";
                if (_apiNamespaceFilters.Length == 0)
                    _apiNamespaceFilters = new[] { "GameData", "Config", "Game.Views", "TaiwuModdingLib" };

                WriteGameInfo($"Config loaded. Enabled={_enabled}, Output={_outputDirectoryName}");
            }
            catch (Exception ex)
            {
                WriteGameError($"Failed to load config: {ex}");
            }
        }

        public static void InitializeOutput()
        {
            if (!DiscoveryEnabled)
                return;

            Directory.CreateDirectory(OutputRootPath);
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "static", "config"));
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "static", "focused", "events"));
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "static", "focused", "buildings"));
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "static", "focused", "characters"));
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "runtime", "events"));
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "runtime", "monthly"));
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "api"));
            Directory.CreateDirectory(Path.Combine(OutputRootPath, "logs"));
            WriteLog("Output initialized: " + OutputRootPath);
        }

        public static void ExportStartupData()
        {
            if (!DiscoveryEnabled)
                return;

            RefreshMonthlyEventGuidIndex();

            if (_exportAllConfigTables)
                ExportAllConfigTables();
            if (_exportFocusedIndexes)
                ExportFocusedIndexes();
            if (_exportApiIndex)
                ExportApiIndex();

            WriteManifest();
        }

        public static void InstallPatches(Harmony harmony)
        {
            if (!DiscoveryEnabled)
                return;

            if (_recordMonthlyCollections)
            {
                Type monthNotifyType = AccessTools.TypeByName("UI_MonthNotify");
                var onNotifyGameData = monthNotifyType == null ? null : AccessTools.Method(monthNotifyType, "OnNotifyGameData");
                if (monthNotifyType == null || onNotifyGameData == null)
                {
                    WriteLog("Monthly hook unavailable: UI_MonthNotify.OnNotifyGameData not found.");
                    WriteGameWarning("[FullDiscovery] Monthly hook unavailable; EventModel runtime capture remains active.");
                }
                else
                {
                    harmony.Patch(onNotifyGameData,
                        postfix: new HarmonyMethod(typeof(UI_MonthNotify_OnNotifyGameData_Patch), nameof(UI_MonthNotify_OnNotifyGameData_Patch.Postfix)));
                    WriteLog("Monthly hook installed: UI_MonthNotify.OnNotifyGameData.");
                }
            }

            if (_recordEventWindows)
            {
                var eventModelNotify = AccessTools.Method(typeof(EventModel), "OnNotifyGameData");
                if (eventModelNotify == null)
                {
                    WriteLog("Event hook unavailable: EventModel.OnNotifyGameData not found.");
                    WriteGameWarning("[FullDiscovery] EventModel.OnNotifyGameData not found.");
                }
                else
                {
                    harmony.Patch(eventModelNotify,
                        postfix: new HarmonyMethod(typeof(EventModel_OnNotifyGameData_Patch), nameof(EventModel_OnNotifyGameData_Patch.Postfix)));
                    WriteLog("Event hook installed: EventModel.OnNotifyGameData.");
                }
            }
        }

        public static void DumpMonthlyEventCollection(List<NotificationWrapper> notifications)
        {
            if (!DiscoveryEnabled || !_recordMonthlyCollections || notifications == null)
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
                WriteGameError($"Failed to dump monthly collection: {ex}");
                WriteLog("Failed to dump monthly collection: " + ex);
            }
        }

        public static void DumpEventWindow(EventModel eventModel)
        {
            if (!DiscoveryEnabled || !_recordEventWindows || eventModel?.DisplayingEventData == null)
                return;

            try
            {
                TaiwuEventDisplayData data = eventModel.DisplayingEventData;
                if (string.IsNullOrEmpty(data.EventGuid))
                    return;

                string signature = BuildEventSignature(data);
                if (signature == _lastEventSignature)
                    return;

                _lastEventSignature = signature;
                string line = BuildEventWindowJsonLine(data, signature);
                AppendLine(Path.Combine("runtime", "events", "event_windows.jsonl"), line);

                if (_monthlyEventGuids.Contains(data.EventGuid))
                    AppendLine(Path.Combine("runtime", "monthly", "monthly_event_windows.jsonl"), line);

                if (_verboseLog)
                    WriteGameInfo($"[FullDiscovery] Event captured. Guid={data.EventGuid}, Options={data.EventOptionInfos?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                WriteGameError($"Failed to dump event window: {ex}");
                WriteLog("Failed to dump event window: " + ex);
            }
        }

        public static void WriteLog(string message)
        {
            try
            {
                if (!DiscoveryEnabled)
                    return;

                string path = Path.Combine(OutputRootPath, "logs", "full_discovery.log");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, Timestamp() + " " + message + Environment.NewLine, Encoding.UTF8);
                RegisterManifestFile(path);
            }
            catch
            {
                // Logging must never break the game.
            }
        }

        private static void ExportAllConfigTables()
        {
            int tableCount = 0;
            foreach (Type type in GetSafeTypes().Where(t => t.Namespace == "Config").OrderBy(t => t.FullName))
            {
                if (_maxConfigTables > 0 && tableCount >= _maxConfigTables)
                    break;

                object? instance = TryGetConfigInstance(type);
                if (instance == null || !(instance is IEnumerable enumerable))
                    continue;

                tableCount++;
                ExportConfigTable(type, enumerable);
            }

            WriteLog($"Static config export completed. Tables={tableCount}");
        }

        private static void ExportConfigTable(Type tableType, IEnumerable enumerable)
        {
            string relativePath = Path.Combine("static", "config", SanitizeFileName(tableType.Name) + ".json");
            string path = ResolveOutputPath(relativePath);
            int count = 0;
            bool truncated = false;

            var sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJsonProperty(sb, "generatedAt", Timestamp(), false);
            AppendJsonProperty(sb, "tableName", tableType.FullName, true);
            sb.AppendLine(",");
            sb.Append("  \"items\": [");

            foreach (object? item in enumerable)
            {
                if (item == null)
                    continue;
                if (_maxItemsPerTable > 0 && count >= _maxItemsPerTable)
                {
                    truncated = true;
                    break;
                }

                if (count > 0)
                    sb.Append(",");
                sb.AppendLine();
                sb.Append("    ");
                sb.Append(SerializeObject(item, 2));
                count++;
            }

            sb.AppendLine();
            sb.Append("  ]");
            AppendJsonProperty(sb, "count", count, true);
            AppendJsonProperty(sb, "truncated", truncated, true);
            sb.AppendLine();
            sb.AppendLine("}");

            SafeWriteAllText(path, sb.ToString());
        }

        private static void ExportFocusedIndexes()
        {
            ExportFocusedIndex("MonthlyEvent", Path.Combine("static", "focused", "events", "monthly_events.index.json"));
            ExportFocusedIndex("InteractionEventOption", Path.Combine("static", "focused", "events", "interaction_options.index.json"));
            ExportFocusedIndex("EventOptionConsumeType", Path.Combine("static", "focused", "events", "event_option_consume_types.index.json"));
            ExportFocusedIndex("BuildingBlock", Path.Combine("static", "focused", "buildings", "building_blocks.index.json"));
            ExportFocusedIndex("BuildingBlockItem", Path.Combine("static", "focused", "buildings", "building_block_items.index.json"));
            ExportFocusedIndex("BuildingFormula", Path.Combine("static", "focused", "buildings", "building_formulas.index.json"));
            ExportFocusedIndex("BuildingScale", Path.Combine("static", "focused", "buildings", "building_scales.index.json"));
            ExportFocusedIndex("Character", Path.Combine("static", "focused", "characters", "character_templates.index.json"));
            ExportFocusedIndex("CharacterFeature", Path.Combine("static", "focused", "characters", "character_features.index.json"));
            ExportFocusedIndex("CharacterTitle", Path.Combine("static", "focused", "characters", "character_titles.index.json"));
        }

        private static void ExportFocusedIndex(string configTypeName, string relativePath)
        {
            try
            {
                Type? type = GetSafeTypes().FirstOrDefault(t => t.Namespace == "Config" && t.Name == configTypeName);
                object? instance = type == null ? null : TryGetConfigInstance(type);
                if (!(instance is IEnumerable enumerable))
                {
                    WriteLog("Focused index skipped; config not found: " + configTypeName);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("{");
                AppendJsonProperty(sb, "generatedAt", Timestamp(), false);
                AppendJsonProperty(sb, "source", type!.FullName, true);
                sb.AppendLine(",");
                sb.Append("  \"items\": [");

                int count = 0;
                foreach (object? item in enumerable)
                {
                    if (item == null)
                        continue;
                    if (_maxItemsPerTable > 0 && count >= _maxItemsPerTable)
                        break;

                    if (count > 0)
                        sb.Append(",");
                    sb.AppendLine();
                    sb.Append("    ");
                    sb.Append(BuildFocusedItemObject(item));
                    count++;
                }

                sb.AppendLine();
                sb.Append("  ]");
                AppendJsonProperty(sb, "count", count, true);
                sb.AppendLine();
                sb.AppendLine("}");
                SafeWriteAllText(ResolveOutputPath(relativePath), sb.ToString());
            }
            catch (Exception ex)
            {
                WriteLog($"Focused index failed: {configTypeName}; {ex}");
            }
        }

        private static string BuildFocusedItemObject(object item)
        {
            Type type = item.GetType();
            var names = new[]
            {
                "TemplateId", "Id", "Key", "Name", "Desc", "Event", "Type", "SubType", "Parameters",
                "Score", "Node", "Age", "Gender", "Grade", "BuildingBlockKey", "Formula",
                "OptionKey", "OptionContent", "Behavior", "OptionType", "ConsumeType"
            };

            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (string name in names)
            {
                if (TryGetMemberValue(item, type, name, out object? value))
                {
                    if (!first)
                        sb.Append(",");
                    first = false;
                    sb.Append(JsonString(ToCamel(name))).Append(":").Append(SerializeValue(value, 1));
                }
            }

            if (!first)
                sb.Append(",");
            sb.Append("\"rawType\":").Append(JsonString(type.FullName));
            sb.Append("}");
            return sb.ToString();
        }

        private static void DumpMonthlyEventCollection(MonthlyEventCollection collection)
        {
            var renderInfos = new List<MonthlyEventRenderInfo>();
            var arguments = new ArgumentCollection();
            collection.GetRenderInfos(renderInfos, arguments);

            foreach (MonthlyEventRenderInfo info in renderInfos)
            {
                object? item = null;
                try
                {
                    item = MonthlyEvent.Instance.GetItem(info.RecordType);
                }
                catch
                {
                    // Keep runtime information even when static lookup fails.
                }

                string line = BuildMonthlyCollectionJsonLine(info, item);
                AppendLine(Path.Combine("runtime", "monthly", "monthly_collections.jsonl"), line);
            }
        }

        private static void ExportApiIndex()
        {
            int methodCount = 0;
            foreach (Type type in GetSafeTypes().Where(IsApiType).OrderBy(t => t.FullName))
            {
                AppendLine(Path.Combine("api", "types.jsonl"), BuildApiTypeLine(type));
                AppendApiMembers(type);
                AppendApiMethods(type, ref methodCount);
            }

            WriteLog("API index export completed. Methods=" + methodCount.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendApiMethods(Type type, ref int methodCount)
        {
            MethodInfo[] methods;
            try
            {
                methods = type.GetMethods(AnyDeclaredMember);
            }
            catch (Exception ex)
            {
                AppendLine(Path.Combine("api", "errors.jsonl"), BuildErrorLine("methods", type.FullName, ex));
                return;
            }

            foreach (MethodInfo method in methods.OrderBy(m => m.Name))
            {
                if (_maxApiMethods > 0 && methodCount >= _maxApiMethods)
                    return;

                AppendLine(Path.Combine("api", "methods.jsonl"), BuildApiMethodLine(type, method));
                methodCount++;
            }
        }

        private static void AppendApiMembers(Type type)
        {
            try
            {
                foreach (FieldInfo field in type.GetFields(AnyDeclaredMember).OrderBy(f => f.Name))
                    AppendLine(Path.Combine("api", "members.jsonl"), BuildApiMemberLine(type, "field", field.Name, field.FieldType, field.IsPublic, field.IsStatic));
                foreach (PropertyInfo property in type.GetProperties(AnyDeclaredMember).OrderBy(p => p.Name))
                {
                    MethodInfo? accessor = property.GetGetMethod(true) ?? property.GetSetMethod(true);
                    AppendLine(Path.Combine("api", "members.jsonl"), BuildApiMemberLine(type, "property", property.Name, property.PropertyType, accessor?.IsPublic ?? false, accessor?.IsStatic ?? false));
                }
            }
            catch (Exception ex)
            {
                AppendLine(Path.Combine("api", "errors.jsonl"), BuildErrorLine("members", type.FullName, ex));
            }
        }

        private static string BuildEventWindowJsonLine(TaiwuEventDisplayData data, string signature)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "source", "EventModel", true);
            AppendJsonProperty(sb, "eventGuid", data.EventGuid, true);
            AppendJsonProperty(sb, "isMonthlyEventGuid", _monthlyEventGuids.Contains(data.EventGuid), true);
            AppendJsonProperty(sb, "eventContent", data.EventContent, true);
            AppendJsonProperty(sb, "mainCharacterId", data.MainCharacter?.CharacterId ?? -1, true);
            AppendJsonProperty(sb, "targetCharacterId", data.TargetCharacter?.CharacterId ?? -1, true);
            AppendJsonProperty(sb, "signature", signature, true);
            AppendJsonProperty(sb, "extraDataSummary", BuildExtraDataSummary(data), true);
            AppendEventOptionsArray(sb, data, true);
            AppendMatchedMonthlyEvents(sb, data.EventGuid, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildMonthlyCollectionJsonLine(MonthlyEventRenderInfo info, object? item)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "source", "MonthlyEventCollection", true);
            AppendJsonProperty(sb, "offset", info.Offset, true);
            AppendJsonProperty(sb, "recordType", info.RecordType, true);
            AppendJsonProperty(sb, "eventGuid", info.EventGuid, true);
            AppendJsonProperty(sb, "text", info.Text, true);
            AppendJsonProperty(sb, "staticItem", item == null ? null : SerializeObject(item, 1), true, rawJson: item != null);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildApiTypeLine(Type type)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "kind", "type", true);
            AppendJsonProperty(sb, "fullName", type.FullName, true);
            AppendJsonProperty(sb, "namespace", type.Namespace, true);
            AppendJsonProperty(sb, "assembly", type.Assembly.GetName().Name, true);
            AppendJsonProperty(sb, "baseType", type.BaseType?.FullName, true);
            AppendJsonProperty(sb, "isClass", type.IsClass, true);
            AppendJsonProperty(sb, "isValueType", type.IsValueType, true);
            AppendJsonProperty(sb, "isInterface", type.IsInterface, true);
            AppendJsonProperty(sb, "isGenericType", type.IsGenericType, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildApiMethodLine(Type owner, MethodInfo method)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "kind", "method", true);
            AppendJsonProperty(sb, "declaringType", owner.FullName, true);
            AppendJsonProperty(sb, "name", method.Name, true);
            AppendJsonProperty(sb, "returnType", SafeTypeName(method.ReturnType), true);
            AppendJsonProperty(sb, "isPublic", method.IsPublic, true);
            AppendJsonProperty(sb, "isStatic", method.IsStatic, true);
            AppendJsonProperty(sb, "isGenericMethod", method.IsGenericMethod, true);
            AppendJsonProperty(sb, "signature", BuildMethodSignature(method), true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildApiMemberLine(Type owner, string kind, string name, Type memberType, bool isPublic, bool isStatic)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "kind", kind, true);
            AppendJsonProperty(sb, "declaringType", owner.FullName, true);
            AppendJsonProperty(sb, "name", name, true);
            AppendJsonProperty(sb, "memberType", SafeTypeName(memberType), true);
            AppendJsonProperty(sb, "isPublic", isPublic, true);
            AppendJsonProperty(sb, "isStatic", isStatic, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildErrorLine(string stage, string? target, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            AppendJsonProperty(sb, "timestamp", Timestamp(), false);
            AppendJsonProperty(sb, "stage", stage, true);
            AppendJsonProperty(sb, "target", target, true);
            AppendJsonProperty(sb, "error", ex.GetType().FullName + ": " + ex.Message, true);
            sb.Append("}");
            return sb.ToString();
        }

        private static void RefreshMonthlyEventGuidIndex()
        {
            _monthlyEventGuids.Clear();
            _monthlyEventsByGuid.Clear();

            try
            {
                foreach (object? item in MonthlyEvent.Instance)
                {
                    if (item == null)
                        continue;
                    if (!TryGetMemberValue(item, item.GetType(), "Event", out object? value))
                        continue;
                    string? eventGuid = value?.ToString();
                    if (string.IsNullOrEmpty(eventGuid))
                        continue;

                    _monthlyEventGuids.Add(eventGuid);
                    if (!_monthlyEventsByGuid.TryGetValue(eventGuid, out List<object> items))
                    {
                        items = new List<object>();
                        _monthlyEventsByGuid.Add(eventGuid, items);
                    }

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                WriteLog("Failed to refresh monthly event guid index: " + ex);
            }
        }

        private static void WriteManifest()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            AppendJsonProperty(sb, "generatedAt", Timestamp(), false);
            AppendJsonProperty(sb, "gameRoot", GameRootPath, true);
            AppendJsonProperty(sb, "outputRoot", OutputRootPath, true);
            AppendJsonProperty(sb, "modVersion", "0.1.0.0", true);
            sb.AppendLine(",");
            sb.Append("  \"enabledModules\": {");
            AppendJsonProperty(sb, "exportAllConfigTables", _exportAllConfigTables, false);
            AppendJsonProperty(sb, "exportFocusedIndexes", _exportFocusedIndexes, true);
            AppendJsonProperty(sb, "recordEventWindows", _recordEventWindows, true);
            AppendJsonProperty(sb, "recordMonthlyCollections", _recordMonthlyCollections, true);
            AppendJsonProperty(sb, "exportApiIndex", _exportApiIndex, true);
            sb.AppendLine();
            sb.Append("  },");
            sb.AppendLine();
            sb.Append("  \"files\": [");
            for (int i = 0; i < _manifestFiles.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.AppendLine();
                sb.Append("    ").Append(JsonString(GetRelativeOutputPath(_manifestFiles[i])));
            }

            sb.AppendLine();
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            SafeWriteAllText(ResolveOutputPath("manifest.json"), sb.ToString());
        }

        private static void AppendEventOptionsArray(StringBuilder sb, TaiwuEventDisplayData data, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append("\"options\":[");
            var options = data.EventOptionInfos;
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

        private static void AppendMatchedMonthlyEvents(StringBuilder sb, string eventGuid, bool prefixComma)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append("\"matchedMonthlyEvents\":[");
            if (!string.IsNullOrEmpty(eventGuid) && _monthlyEventsByGuid.TryGetValue(eventGuid, out List<object> items))
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    sb.Append(BuildFocusedItemObject(items[i]));
                }
            }

            sb.Append("]");
        }

        private static string BuildEventSignature(TaiwuEventDisplayData data)
        {
            var sb = new StringBuilder();
            sb.Append(data.EventGuid).Append("|");
            sb.Append(data.MainCharacter?.CharacterId ?? -1).Append("|");
            sb.Append(data.TargetCharacter?.CharacterId ?? -1).Append("|");
            var options = data.EventOptionInfos;
            for (int i = 0; i < (options?.Count ?? 0); i++)
                sb.Append(options![i].OptionKey).Append(":").Append(options[i].OptionState).Append(";");
            sb.Append("|").Append(data.EventContent?.GetHashCode() ?? 0);
            return sb.ToString();
        }

        private static string BuildExtraDataSummary(TaiwuEventDisplayData data)
        {
            try
            {
                object? value = TryGetReflectionMember(data, "ExtraData");
                if (value == null)
                    return string.Empty;
                return value.GetType().FullName + " " + SerializeValue(value, 1);
            }
            catch (Exception ex)
            {
                return "extraDataError: " + ex.Message;
            }
        }

        private static object? TryGetConfigInstance(Type type)
        {
            try
            {
                PropertyInfo? property = type.GetProperty("Instance", StaticPublic);
                if (property != null)
                    return property.GetValue(null);
                FieldInfo? field = type.GetField("Instance", StaticPublic);
                return field?.GetValue(null);
            }
            catch (Exception ex)
            {
                WriteLog($"Config instance unavailable: {type.FullName}; {ex.Message}");
                return null;
            }
        }

        private static IEnumerable<Type> GetSafeTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                    yield return type;
            }
        }

        private static bool IsApiType(Type type)
        {
            string fullName = type.FullName ?? string.Empty;
            return _apiNamespaceFilters.Any(filter => !string.IsNullOrWhiteSpace(filter) && fullName.StartsWith(filter.Trim(), StringComparison.Ordinal));
        }

        private static string SerializeObject(object value, int depth)
        {
            if (depth < 0 || value == null)
                return "null";

            Type type = value.GetType();
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;

            foreach (FieldInfo field in type.GetFields(PublicInstance).OrderBy(f => f.Name))
            {
                if (!first)
                    sb.Append(",");
                first = false;
                sb.Append(JsonString(field.Name)).Append(":").Append(SafeSerializeMember(() => field.GetValue(value), depth));
            }

            foreach (PropertyInfo property in type.GetProperties(PublicInstance).OrderBy(p => p.Name))
            {
                if (property.GetIndexParameters().Length > 0 || property.GetGetMethod() == null)
                    continue;
                if (!first)
                    sb.Append(",");
                first = false;
                sb.Append(JsonString(property.Name)).Append(":").Append(SafeSerializeMember(() => property.GetValue(value, null), depth));
            }

            if (first)
                sb.Append(JsonString("$value")).Append(":").Append(JsonString(value.ToString()));
            sb.Append("}");
            return sb.ToString();
        }

        private static string SafeSerializeMember(Func<object?> getter, int depth)
        {
            try
            {
                return SerializeValue(getter(), depth - 1);
            }
            catch (Exception ex)
            {
                return JsonString("<error: " + ex.Message + ">");
            }
        }

        private static string SerializeValue(object? value, int depth)
        {
            if (value == null)
                return "null";
            if (value is string s)
                return JsonString(s);
            if (value is bool b)
                return b ? "true" : "false";
            if (value is char c)
                return JsonString(c.ToString());
            if (value is Enum)
                return JsonString(value.ToString());
            if (IsNumeric(value))
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            if (depth <= 0)
                return JsonString(value.ToString());
            if (value is IEnumerable enumerable && !(value is string))
            {
                var sb = new StringBuilder();
                sb.Append("[");
                int count = 0;
                foreach (object? item in enumerable)
                {
                    if (count >= 50)
                    {
                        if (count > 0)
                            sb.Append(",");
                        sb.Append(JsonString("<truncated>"));
                        break;
                    }

                    if (count > 0)
                        sb.Append(",");
                    sb.Append(SerializeValue(item, depth - 1));
                    count++;
                }

                sb.Append("]");
                return sb.ToString();
            }

            return SerializeObject(value, depth - 1);
        }

        private static bool IsNumeric(object value)
        {
            TypeCode code = Type.GetTypeCode(value.GetType());
            return code == TypeCode.Byte || code == TypeCode.SByte || code == TypeCode.Int16 ||
                   code == TypeCode.UInt16 || code == TypeCode.Int32 || code == TypeCode.UInt32 ||
                   code == TypeCode.Int64 || code == TypeCode.UInt64 || code == TypeCode.Single ||
                   code == TypeCode.Double || code == TypeCode.Decimal;
        }

        private static bool TryGetMemberValue(object instance, Type type, string name, out object? value)
        {
            value = null;
            try
            {
                FieldInfo? field = type.GetField(name, PublicInstance);
                if (field != null)
                {
                    value = field.GetValue(instance);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, PublicInstance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(instance, null);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static object? TryGetReflectionMember(object instance, string name)
        {
            Type type = instance.GetType();
            FieldInfo? field = type.GetField(name, PublicInstance);
            if (field != null)
                return field.GetValue(instance);
            PropertyInfo? property = type.GetProperty(name, PublicInstance);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(instance, null);
            return null;
        }

        private static string BuildMethodSignature(MethodInfo method)
        {
            try
            {
                string parameters = string.Join(", ", method.GetParameters().Select(p => SafeTypeName(p.ParameterType) + " " + p.Name).ToArray());
                return SafeTypeName(method.ReturnType) + " " + method.Name + "(" + parameters + ")";
            }
            catch (Exception ex)
            {
                return "<signature error: " + ex.Message + ">";
            }
        }

        private static string SafeTypeName(Type? type)
        {
            if (type == null)
                return string.Empty;
            try
            {
                return type.FullName ?? type.Name;
            }
            catch
            {
                return "<type>";
            }
        }

        private static void AppendLine(string relativePath, string line)
        {
            SafeAppendAllText(ResolveOutputPath(relativePath), line + Environment.NewLine);
        }

        private static void SafeWriteAllText(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text, Encoding.UTF8);
            RegisterManifestFile(path);
        }

        private static void SafeAppendAllText(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, text, Encoding.UTF8);
            RegisterManifestFile(path);
        }

        private static string ResolveOutputPath(string relativePath)
        {
            return Path.Combine(OutputRootPath, NormalizeRelativePath(relativePath));
        }

        private static void RegisterManifestFile(string path)
        {
            if (!_manifestFiles.Contains(path))
                _manifestFiles.Add(path);
        }

        private static string GetRelativeOutputPath(string path)
        {
            try
            {
                if (path.StartsWith(OutputRootPath, StringComparison.OrdinalIgnoreCase))
                    return path.Substring(OutputRootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
            }
            catch
            {
                // Fall through.
            }

            return path.Replace('\\', '/');
        }

        private static string NormalizeRelativePath(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        private static string ToCamel(string value)
        {
            if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
                return value;
            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        private static string[] SplitCsv(string value)
        {
            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToArray();
        }

        private static bool ReadBool(string content, string key, bool defaultValue)
        {
            Match match = Regex.Match(content, @"Key\s*=\s*""" + Regex.Escape(key) + @"""[\s\S]*?DefaultValue\s*=\s*(true|false)", RegexOptions.IgnoreCase);
            if (!match.Success)
                match = Regex.Match(content, key + @"\s*=\s*(true|false)", RegexOptions.IgnoreCase);
            return match.Success ? string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase) : defaultValue;
        }

        private static int ReadInt(string content, string key, int defaultValue)
        {
            string value = ReadString(content, key, defaultValue.ToString(CultureInfo.InvariantCulture));
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : defaultValue;
        }

        private static string ReadString(string content, string key, string defaultValue)
        {
            Match match = Regex.Match(content, @"Key\s*=\s*""" + Regex.Escape(key) + @"""[\s\S]*?DefaultValue\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (!match.Success)
                match = Regex.Match(content, key + @"\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : defaultValue;
        }

        private static void AppendJsonProperty(StringBuilder sb, string key, object? value, bool prefixComma, bool rawJson = false)
        {
            if (prefixComma)
                sb.Append(",");
            sb.Append("\"").Append(key).Append("\":");
            if (rawJson && value is string raw)
                sb.Append(raw);
            else
                sb.Append(SerializeValue(value, 1));
        }

        private static string JsonString(string? value)
        {
            if (value == null)
                return "null";

            var sb = new StringBuilder();
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

        private static string Timestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
        }

        private static void WriteGameInfo(string message)
        {
            AdaptableLog.Info("[FullDiscovery] " + message);
        }

        private static void WriteGameWarning(string message)
        {
            AdaptableLog.Warning("[FullDiscovery] " + message);
        }

        private static void WriteGameError(string message)
        {
            AdaptableLog.Error("[FullDiscovery] " + message);
        }
    }

    internal static class UI_MonthNotify_OnNotifyGameData_Patch
    {
        public static void Postfix(List<NotificationWrapper> notifications)
        {
            FullDiscoveryDumper.DumpMonthlyEventCollection(notifications);
        }
    }

    internal static class EventModel_OnNotifyGameData_Patch
    {
        [HarmonyPriority(Priority.First)]
        public static void Postfix(EventModel __instance)
        {
            FullDiscoveryDumper.DumpEventWindow(__instance);
        }
    }
}
