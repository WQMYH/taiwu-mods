using System;
using System.Collections;
using System.Reflection;
using FrameWork;
using FrameWork.CommandSystem;
using GameData.Domains.Item;
using GameData.Utilities;
using HarmonyLib;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;
using TMPro;

namespace ContinuousDigging.Frontend
{
    [PluginConfig("ContinuousDiggingFrontend", "MOD Developer", "1.0.0")]
    public class PluginMainFrontend : TaiwuRemakePlugin
    {
        private static bool _enabled = true;
        private static int _maxGradeLimit = 0;
        private static int _actionPointCost = 30;
        private static bool _enableActionPointCheck = true;
        private static int _maxConsecutiveDigs = 50;
        private static bool _debugLog = true;
        private static bool _findTreasurePatched;
        private static bool _uiBottomActionMethodsPatched;
        private static bool _findTreasureTypeScanLogged;
        
        private static Harmony _harmony;

        public override void Initialize()
        {
            LoadSettings();

            if (_enabled)
            {
                _harmony = new Harmony(ModIdStr);
                
                TryPatchFindTreasure();
                PatchUiBottomActionMethods();
                PatchClickAndCommandDiagnostics();
                PatchUiManagerShowUi();

                var uiBottomOnInitMethod = AccessTools.Method(typeof(UI_Bottom), "OnInit");
                if (uiBottomOnInitMethod != null)
                {
                    var postfix = typeof(PluginMainFrontend).GetMethod("UI_Bottom_OnInit_Postfix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    _harmony.Patch(uiBottomOnInitMethod, null, new HarmonyMethod(postfix));
                    AdaptableLog.Info("[ContinuousDigging] Patched UI_Bottom.OnInit");
                }
                else
                {
                    AdaptableLog.Error("[ContinuousDigging] Failed to find UI_Bottom.OnInit method");
                }
                
                AdaptableLog.Info($"[ContinuousDigging] Frontend initialized. Enabled={_enabled}, Debug={_debugLog}, MaxGrade={_maxGradeLimit}, ActionPointCost={_actionPointCost}, MaxDigs={_maxConsecutiveDigs}, PatchedFindTreasure={_findTreasurePatched}");
            }
        }

        public override void Dispose()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
                _findTreasurePatched = false;
                _uiBottomActionMethodsPatched = false;
            }
        }

        public override void OnModSettingUpdate()
        {
            LoadSettings();
            
            if (_enabled && _harmony == null)
            {
                // 重新初始化 Patch
                Initialize();
            }
            else if (!_enabled && _harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
                _findTreasurePatched = false;
                _uiBottomActionMethodsPatched = false;
                AdaptableLog.Info("[ContinuousDigging] Disabled and unpatched");
            }
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

        /// <summary>
        /// Hook UI_Bottom.OnInit，在底部栏添加“连续挖宝”按钮
        /// </summary>
        public static void UI_Bottom_OnInit_Postfix(object __instance)
        {
            try
            {
                DebugLog($"UI_Bottom.OnInit postfix entered. InstanceType={__instance?.GetType().FullName}, FindTreasurePatched={_findTreasurePatched}");
                TryPatchFindTreasure();
                if (!_findTreasurePatched && __instance is MonoBehaviour behaviourForPatch)
                {
                    DebugLog("Starting delayed UI_FindTreasure patch retry from UI_Bottom.OnInit.");
                    behaviourForPatch.StartCoroutine(PatchFindTreasureWhenLoaded());
                }

                AdaptableLog.Info("[ContinuousDigging] Creating continuous digging button...");
                
                // 获取 UI_Bottom 的 Transform
                var uiBottom = (UnityEngine.MonoBehaviour)__instance;
                var transform = uiBottom.transform;
                
                // 查找现有的挖宝按钮作为模板
                UnityEngine.Transform findTreasureBtn = null;
                for (int i = 0; i < transform.childCount; i++)
                {
                    var child = transform.GetChild(i);
                    DebugLog($"UI_Bottom child[{i}] name={child.name}");
                    if (child.name.Contains("FindTreasure") || child.name.Contains("Treasure"))
                    {
                        findTreasureBtn = child;
                        AdaptableLog.Info($"[ContinuousDigging] Found template button: {child.name}");
                        break;
                    }
                }
                
                if (findTreasureBtn == null)
                {
                    AdaptableLog.Error("[ContinuousDigging] Failed to find FindTreasure button template");
                    return;
                }
                
                // 复制按钮
                var newButton = UnityEngine.Object.Instantiate(findTreasureBtn.gameObject, transform, false);
                newButton.name = "ContinuousDiggingBtn";
                
                // 修改按钮文本
                var textComponent = newButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (textComponent != null)
                {
                    textComponent.text = "连续挖宝";
                }
                else
                {
                    var tmpText = newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        tmpText.text = "连续挖宝";
                    }
                }
                
                // 添加点击事件 - 启动真正的连续挖掘
                var button = newButton.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        AdaptableLog.Info($"[ContinuousDigging] Continuous digging button clicked. PatchedFindTreasure={_findTreasurePatched}");
                        StartContinuousDigging(__instance);
                    });
                }
                
                AdaptableLog.Info("[ContinuousDigging] Continuous digging button created successfully!");
            }
            catch (System.Exception ex)
            {
                AdaptableLog.Error($"[ContinuousDigging] Failed to create button: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 启动真正的连续挖掘逻辑
        /// </summary>
        private static void StartContinuousDigging(object uiBottomInstance)
        {
            try
            {
                AdaptableLog.Info("[ContinuousDigging] === Starting True Continuous Digging ===");

                TryPatchFindTreasure();
                if (!_findTreasurePatched && uiBottomInstance is MonoBehaviour behaviour)
                {
                    DebugLog("Starting delayed UI_FindTreasure patch retry from continuous button click.");
                    behaviour.StartCoroutine(PatchFindTreasureWhenLoaded());
                }

                // Use the game's built-in series mode, then patch only the "stop on success" decision.
                DebugLog($"Invoking UI_Bottom.FindTreasureSeries. InstanceType={uiBottomInstance?.GetType().FullName}, PatchedFindTreasure={_findTreasurePatched}");
                Traverse.Create(uiBottomInstance).Method("FindTreasureSeries").GetValue();
            }
            catch (System.Exception ex)
            {
                AdaptableLog.Error($"[ContinuousDigging] StartContinuousDigging failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static bool TryPatchFindTreasure()
        {
            if (_findTreasurePatched || _harmony == null)
                return _findTreasurePatched;

            Type uiFindTreasureType = typeof(UI_Bottom).Assembly.GetType("UI_FindTreasure", false)
                ?? AccessTools.TypeByName("UI_FindTreasure")
                ?? FindLoadedType("UI_FindTreasure");
            var animFailedAndProgressRotateMethod = uiFindTreasureType == null ? null : AccessTools.Method(uiFindTreasureType, "AnimFailedAndProgressRotate");
            if (animFailedAndProgressRotateMethod == null)
            {
                DebugLog("UI_FindTreasure.AnimFailedAndProgressRotate not available yet; will retry.");
                LogFindTreasureTypeScanOnce();
                return false;
            }

            var prefix = typeof(PluginMainFrontend).GetMethod(nameof(UI_FindTreasure_AnimFailedAndProgressRotate_Prefix), BindingFlags.Static | BindingFlags.Public);
            var postfix = typeof(PluginMainFrontend).GetMethod(nameof(UI_FindTreasure_AnimFailedAndProgressRotate_Postfix), BindingFlags.Static | BindingFlags.Public);
            _harmony.Patch(animFailedAndProgressRotateMethod, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            _findTreasurePatched = true;
            AdaptableLog.Info($"[ContinuousDigging] Patched UI_FindTreasure.AnimFailedAndProgressRotate. Type={uiFindTreasureType.FullName}, Assembly={uiFindTreasureType.Assembly.GetName().Name}");
            return true;
        }

        private static void PatchUiBottomActionMethods()
        {
            if (_uiBottomActionMethodsPatched || _harmony == null)
                return;

            var prefix = typeof(PluginMainFrontend).GetMethod(nameof(UI_Bottom_FindTreasureAction_Prefix), BindingFlags.Static | BindingFlags.Public);
            var postfix = typeof(PluginMainFrontend).GetMethod(nameof(UI_Bottom_FindTreasureAction_Postfix), BindingFlags.Static | BindingFlags.Public);
            bool patchedAny = false;

            foreach (string methodName in new[] { "FindTreasureSeries", "FindTreasureOnce" })
            {
                var method = AccessTools.Method(typeof(UI_Bottom), methodName);
                if (method == null)
                {
                    AdaptableLog.Warning($"[ContinuousDigging] UI_Bottom.{methodName} not found.");
                    continue;
                }

                _harmony.Patch(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                patchedAny = true;
                AdaptableLog.Info($"[ContinuousDigging] Patched UI_Bottom.{methodName}");
            }

            _uiBottomActionMethodsPatched = patchedAny;
        }

        private static void PatchClickAndCommandDiagnostics()
        {
            if (_harmony == null)
                return;

            var onClick = AccessTools.Method(typeof(UI_Bottom), "OnClick");
            if (onClick != null)
            {
                var prefix = typeof(PluginMainFrontend).GetMethod(nameof(UI_Bottom_OnClick_Prefix), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(onClick, new HarmonyMethod(prefix));
                AdaptableLog.Info("[ContinuousDigging] Patched UI_Bottom.OnClick");
            }
            else
            {
                AdaptableLog.Warning("[ContinuousDigging] UI_Bottom.OnClick not found.");
            }

            var addCommandShowUi = AccessTools.Method(typeof(CommandManager), "AddCommandShowUI");
            if (addCommandShowUi != null)
            {
                var prefix = typeof(PluginMainFrontend).GetMethod(nameof(CommandManager_AddCommandShowUI_Prefix), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(addCommandShowUi, new HarmonyMethod(prefix));
                AdaptableLog.Info("[ContinuousDigging] Patched CommandManager.AddCommandShowUI");
            }
            else
            {
                AdaptableLog.Warning("[ContinuousDigging] CommandManager.AddCommandShowUI not found.");
            }
        }

        private static void PatchUiManagerShowUi()
        {
            if (_harmony == null)
                return;

            var showUi = AccessTools.Method(typeof(UIManager), "ShowUI");
            if (showUi != null)
            {
                var prefix = typeof(PluginMainFrontend).GetMethod(nameof(UIManager_ShowUI_Prefix), BindingFlags.Static | BindingFlags.Public);
                var postfix = typeof(PluginMainFrontend).GetMethod(nameof(UIManager_ShowUI_Postfix), BindingFlags.Static | BindingFlags.Public);
                _harmony.Patch(showUi, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                AdaptableLog.Info("[ContinuousDigging] Patched UIManager.ShowUI");
            }
            else
            {
                AdaptableLog.Warning("[ContinuousDigging] UIManager.ShowUI not found.");
            }
        }

        public static void UIManager_ShowUI_Prefix(object __instance, object elem)
        {
            try
            {
                if (!IsFindTreasureElement(elem))
                    return;

                DebugLog($"UIManager.ShowUI prefix FindTreasure. Element={DescribeUiElement(elem)}, PatchedFindTreasure={_findTreasurePatched}");
                var args = EasyPool.Get<ArgumentBox>().Set("series", true);
                Traverse.Create(elem).Method("SetOnInitArgs", args).GetValue();
                DebugLog("Forced UI_FindTreasure init args: series=true.");
                TryPatchFindTreasure();
                StartPatchRetryFromUiBottom(__instance, "UIManager.ShowUI prefix");
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[ContinuousDigging] UIManager.ShowUI prefix failed: {ex.Message}");
            }
        }

        public static void UIManager_ShowUI_Postfix(object __instance, object elem)
        {
            try
            {
                if (!IsFindTreasureElement(elem))
                    return;

                DebugLog($"UIManager.ShowUI postfix FindTreasure. Element={DescribeUiElement(elem)}, PatchedFindTreasure={_findTreasurePatched}");
                TryPatchFindTreasure();
                StartPatchRetryFromUiBottom(__instance, "UIManager.ShowUI postfix");
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[ContinuousDigging] UIManager.ShowUI postfix failed: {ex.Message}");
            }
        }

        public static void UI_Bottom_OnClick_Prefix(object btn)
        {
            try
            {
                string name = btn is UnityEngine.Object obj ? obj.name : btn?.ToString();
                if (string.IsNullOrEmpty(name) || name.IndexOf("Treasure", StringComparison.OrdinalIgnoreCase) < 0)
                    return;

                DebugLog($"UI_Bottom.OnClick prefix. ButtonName={name}, ButtonType={btn?.GetType().FullName}, PatchedFindTreasure={_findTreasurePatched}");
                TryPatchFindTreasure();
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[ContinuousDigging] UI_Bottom.OnClick diagnostic failed: {ex.Message}");
            }
        }

        public static void CommandManager_AddCommandShowUI_Prefix(EPriority priority, object element, ArgumentBox argBox)
        {
            try
            {
                if (!IsFindTreasureElement(element))
                    return;

                bool series = false;
                bool hasSeries = argBox != null && argBox.Get("series", out series);
                DebugLog($"CommandManager.AddCommandShowUI FindTreasure. Priority={priority}, hasSeries={hasSeries}, series={series}, PatchedFindTreasure={_findTreasurePatched}");
                TryPatchFindTreasure();
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[ContinuousDigging] AddCommandShowUI diagnostic failed: {ex.Message}");
            }
        }

        private static bool IsFindTreasureElement(object element)
        {
            string path = Convert.ToString(ReadMember(element, "_path") ?? ReadMember(element, "Path") ?? element);
            return string.Equals(path, "UI_FindTreasure", StringComparison.Ordinal);
        }

        private static string DescribeUiElement(object element)
        {
            return $"type={element?.GetType().FullName}, path={ReadMember(element, "_path") ?? ReadMember(element, "Path") ?? element}";
        }

        public static void UI_Bottom_FindTreasureAction_Prefix(object __instance, MethodBase __originalMethod)
        {
            DebugLog($"UI_Bottom.{__originalMethod?.Name} prefix entered. InstanceType={__instance?.GetType().FullName}, PatchedFindTreasure={_findTreasurePatched}");
            TryPatchFindTreasure();
            StartPatchRetryFromUiBottom(__instance, $"UI_Bottom.{__originalMethod?.Name} prefix");
        }

        public static void UI_Bottom_FindTreasureAction_Postfix(object __instance, MethodBase __originalMethod)
        {
            DebugLog($"UI_Bottom.{__originalMethod?.Name} postfix entered. PatchedFindTreasure={_findTreasurePatched}");
            TryPatchFindTreasure();
            StartPatchRetryFromUiBottom(__instance, $"UI_Bottom.{__originalMethod?.Name} postfix");
        }

        private static IEnumerator PatchFindTreasureWhenLoaded()
        {
            float elapsed = 0f;
            while (!_findTreasurePatched && elapsed < 20f)
            {
                if (TryPatchFindTreasure())
                {
                    DebugLog($"Delayed patch succeeded after {elapsed:0.00}s.");
                    yield break;
                }
                elapsed += 0.25f;
                yield return new WaitForSeconds(0.25f);
            }

            if (!_findTreasurePatched)
                AdaptableLog.Error("[ContinuousDigging] Failed to patch UI_FindTreasure after delayed retries.");
        }

        private static void StartPatchRetryFromUiBottom(object uiBottomInstance, string source)
        {
            if (_findTreasurePatched)
                return;

            if (uiBottomInstance is MonoBehaviour behaviour)
            {
                DebugLog($"Starting delayed UI_FindTreasure patch retry from {source}.");
                behaviour.StartCoroutine(PatchFindTreasureWhenLoaded());
            }
            else
            {
                DebugLog($"Cannot start delayed UI_FindTreasure patch retry from {source}: instance is not MonoBehaviour.");
            }
        }

        public static void UI_FindTreasure_AnimFailedAndProgressRotate_Prefix(object __instance, out bool __state)
        {
            __state = ShouldRestoreSeriesAfterOriginalStop(__instance);
            DebugLog($"AnimFailedAndProgressRotate Prefix: shouldRestore={__state}; {DescribeUiFindTreasureState(__instance)}");
        }

        public static void UI_FindTreasure_AnimFailedAndProgressRotate_Postfix(object __instance, bool __state)
        {
            DebugLog($"AnimFailedAndProgressRotate Postfix enter: shouldRestore={__state}; {DescribeUiFindTreasureState(__instance)}");
            if (!__state)
                return;

            try
            {
                Traverse.Create(__instance).Field("_series").SetValue(true);
                var seriesText = ((RefersBase)__instance).CGet<TextMeshProUGUI>("SeriesText");
                if (seriesText != null)
                    seriesText.text = LocalStringManager.Get((LanguageKey)7692);
                var continueStop = ((RefersBase)__instance).CGet<GameObject>("SeriesContinueStop");
                if (continueStop != null)
                    continueStop.SetActive(false);
                var hotkeyDisplay = ((RefersBase)__instance).CGet<GameObject>("SeriesHotkeyDisplay");
                if (hotkeyDisplay != null)
                    hotkeyDisplay.SetActive(true);
                DebugLog($"AnimFailedAndProgressRotate Postfix restored _series=true; {DescribeUiFindTreasureState(__instance)}");
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[ContinuousDigging] Failed to restore series state: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static bool ShouldRestoreSeriesAfterOriginalStop(object instance)
        {
            try
            {
                if (!_enabled)
                {
                    DebugLog("Do not restore: mod disabled.");
                    return false;
                }

                Traverse ui = Traverse.Create(instance);
                if (!ui.Field("_series").GetValue<bool>())
                {
                    DebugLog("Do not restore: _series was false before original stop check.");
                    return false;
                }

                int seriesTimes = ui.Field("_seriesTimes").GetValue<int>();
                if (_maxConsecutiveDigs > 0 && seriesTimes >= _maxConsecutiveDigs)
                {
                    AdaptableLog.Info($"[ContinuousDigging] Stop: reached max consecutive digs ({seriesTimes}/{_maxConsecutiveDigs}).");
                    return false;
                }

                if (_enableActionPointCheck && !SingletonObject.getInstance<TimeManager>().IsActionPointEnough(_actionPointCost))
                {
                    AdaptableLog.Info($"[ContinuousDigging] Stop: action point is below configured cost {_actionPointCost}.");
                    return false;
                }

                object result = ui.Field("_result").GetValue();
                if (ReadBool(result, "RequestInvalid") || !ReadBool(result, "Success"))
                {
                    DebugLog($"Do not restore: request invalid or not success. {DescribeTreasureResult(result)}");
                    return false;
                }

                if (ReadBool(result, "AnyMaterial"))
                {
                    AdaptableLog.Info("[ContinuousDigging] Stop: special material event result is left to original flow.");
                    return false;
                }

                int bestGrade = GetBestRewardGrade(result);
                DebugLog($"Restore candidate: seriesTimes={seriesTimes}, bestGrade={bestGrade}, result={DescribeTreasureResult(result)}");
                if (_maxGradeLimit > 0 && bestGrade > 0 && bestGrade <= _maxGradeLimit)
                {
                    AdaptableLog.Info($"[ContinuousDigging] Stop: reward grade {bestGrade} reached configured limit {_maxGradeLimit}.");
                    return false;
                }

                bool shouldRestore = ReadBool(result, "AnyItem") || ReadBool(result, "AnyExtraItem");
                DebugLog($"Restore decision={shouldRestore}. AnyItem={ReadBool(result, "AnyItem")}, AnyExtraItem={ReadBool(result, "AnyExtraItem")}");
                return shouldRestore;
            }
            catch (Exception ex)
            {
                AdaptableLog.Error($"[ContinuousDigging] Failed to evaluate continuous digging state: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private static int GetBestRewardGrade(object result)
        {
            int bestGrade = 0;
            if (ReadBool(result, "AnyItem"))
                bestGrade = MergeBestGrade(bestGrade, GetItemGrade(ReadMember(result, "ItemKey")));
            if (ReadBool(result, "AnyExtraItem"))
            {
                object extraItems = ReadMember(result, "ExtraItems");
                if (extraItems is IEnumerable enumerable)
                {
                    foreach (object itemKey in enumerable)
                        bestGrade = MergeBestGrade(bestGrade, GetItemGrade(itemKey));
                }
            }
            return bestGrade;
        }

        private static int MergeBestGrade(int current, int candidate)
        {
            if (candidate <= 0)
                return current;
            return current <= 0 ? candidate : Math.Min(current, candidate);
        }

        private static int GetItemGrade(object itemKey)
        {
            try
            {
                if (itemKey == null)
                    return 0;
                sbyte itemType = Convert.ToSByte(ReadMember(itemKey, "ItemType"));
                short templateId = Convert.ToInt16(ReadMember(itemKey, "TemplateId"));
                if (templateId < 0)
                    return 0;
                return ItemTemplateHelper.GetGrade(itemType, templateId);
            }
            catch (Exception ex)
            {
                AdaptableLog.Warning($"[ContinuousDigging] Failed to read item grade for {itemKey}: {ex.Message}");
                return 0;
            }
        }

        private static bool ReadBool(object instance, string memberName)
        {
            object value = ReadMember(instance, memberName);
            return value is bool flag && flag;
        }

        private static object ReadMember(object instance, string memberName)
        {
            if (instance == null)
                return null;
            Type type = instance.GetType();
            PropertyInfo property = AccessTools.Property(type, memberName);
            if (property != null)
                return property.GetValue(instance, null);
            FieldInfo field = AccessTools.Field(type, memberName);
            return field == null ? null : field.GetValue(instance);
        }

        private static Type FindLoadedType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static void DebugLog(string message)
        {
            if (_debugLog)
                AdaptableLog.Info("[ContinuousDigging][Debug] " + message);
        }

        private static string DescribeUiFindTreasureState(object instance)
        {
            try
            {
                if (instance == null)
                    return "instance=null";
                Traverse ui = Traverse.Create(instance);
                object result = ui.Field("_result").GetValue();
                return $"type={instance.GetType().FullName}, series={ui.Field("_series").GetValue<bool>()}, seriesTimes={ui.Field("_seriesTimes").GetValue<int>()}, result=({DescribeTreasureResult(result)})";
            }
            catch (Exception ex)
            {
                return "stateError=" + ex.Message;
            }
        }

        private static string DescribeTreasureResult(object result)
        {
            if (result == null)
                return "result=null";
            return $"type={result.GetType().FullName}, requestInvalid={ReadBool(result, "RequestInvalid")}, success={ReadBool(result, "Success")}, anyItem={ReadBool(result, "AnyItem")}, anyExtraItem={ReadBool(result, "AnyExtraItem")}, anyMaterial={ReadBool(result, "AnyMaterial")}, anyResource={ReadBool(result, "AnyResource")}, itemKey={ReadMember(result, "ItemKey")}, itemCount={ReadMember(result, "ItemCount")}, materialTemplateId={ReadMember(result, "MaterialTemplateId")}, resourceType={ReadMember(result, "ResourceType")}, resourceCount={ReadMember(result, "ResourceCount")}";
        }

        private static void LogFindTreasureTypeScanOnce()
        {
            if (!_debugLog || _findTreasureTypeScanLogged)
                return;

            _findTreasureTypeScanLogged = true;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;
                if (assemblyName != "Assembly-CSharp" && assemblyName != "GameData.Shared")
                    continue;

                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        string name = type.FullName ?? type.Name;
                        if (name.IndexOf("FindTreasure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("TreasureFind", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            DebugLog($"Loaded type candidate: {name}, assembly={assemblyName}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    DebugLog($"Type scan failed for {assemblyName}: {ex.Message}");
                    foreach (Type type in ex.Types)
                    {
                        if (type == null)
                            continue;
                        string name = type.FullName ?? type.Name;
                        if (name.IndexOf("FindTreasure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("TreasureFind", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            DebugLog($"Loaded type candidate: {name}, assembly={assemblyName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Type scan failed for {assemblyName}: {ex.Message}");
                }
            }
        }

        public const string ModuleName = "ContinuousDiggingFrontend";
    }
}
