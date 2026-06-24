using System;
using System.IO;
using System.Reflection;
using FrameWork;
using FrameWork.CommandSystem;
using HarmonyLib;
using GameData.Domains.Mod;
using GameData.Utilities;
using TaiwuModdingLib.Core.Plugin;
using UnityEngine;
using TMPro;

namespace CopyBuildingModernized.Frontend
{
    [PluginConfig("CopyBuildingModernized.Frontend", "Slimoon", "2.0.2")]
    public sealed class FrontendEntry : TaiwuRemakePlugin
    {
        private static string _modIdStr = string.Empty;
        private static Harmony _harmony;

        // 不要用 object 保存 Unity Component。
        // object != null 不会触发 Unity 的 fake-null 判定，UI 被销毁后仍会被当成非空对象。
        private static Component _mainButton;          // 主按钮：太吾村建筑管理
        private static Component _exportButton;
        private static Component _importButton;
        private static Component _convertButton;
        private static TextMeshProUGUI _widthInputLabel;
        private static TMP_InputField _widthInputField;
        private static TextMeshProUGUI _statusLabel;
        private static bool _isExpanded = false;       // UI展开状态
        private static object _currentViewInstance;

        private const string ExportMethodName = "ExportVillage";
        private const string ImportMethodName = "ImportVillage";
        private const string ConvertMethodName = "ConvertVillageWidth";
        private const string BinFileFilter = "村庄数据文件(*.bin)\0*.bin\0";

        public override void Initialize()
        {
            _modIdStr = ModIdStr;
            _harmony = new Harmony(GetGuid());

            // 使用 AccessTools 动态查找 ViewBuildingManage，避免编译时强依赖
            var viewType = AccessTools.TypeByName("Game.Views.Building.BuildingManage.ViewBuildingManage");
            if (viewType != null)
            {
                var onInit = AccessTools.Method(viewType, "OnInit");
                var onDisable = AccessTools.Method(viewType, "OnDisable");
                if (onInit != null)
                    _harmony.Patch(onInit,
                        postfix: new HarmonyMethod(typeof(FrontendEntry), nameof(OnInit_Postfix)));
                if (onDisable != null)
                    _harmony.Patch(onDisable,
                        postfix: new HarmonyMethod(typeof(FrontendEntry), nameof(OnDisable_Postfix)));
            }
            else
            {
                Debug.Log("[CopyBuildingModernized] ViewBuildingManage type not found!");
            }

            Debug.Log("[CopyBuildingModernized] Frontend initialized.");
        }

        public override void Dispose()
        {
            _harmony?.UnpatchSelf();
            _harmony = null;
            ClearControls(true);
        }

        private const short TaiwuVillageTemplateId = 44;

        // ==== Harmony 补丁 ====

        public static void OnInit_Postfix(object __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                // 只在太吾村建筑（TemplateId == 44）显示按钮
                var t = Traverse.Create(__instance);
                short templateId = t.Field<short>("_buildingTemplateId").Value;

                if (templateId != TaiwuVillageTemplateId)
                {
                    SetControlsVisible(false);
                    return;
                }

                EnsureControls(__instance);
                SetControlsVisible(true);
                SetBusy(false);
                SetStatus("");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CopyBuildingModernized] OnInit_Postfix failed:\n" + ex);
                ClearControls(false);
            }
        }

        public static void OnDisable_Postfix()
        {
            try
            {
                SetBusy(false);
                SetControlsVisible(false);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CopyBuildingModernized] OnDisable_Postfix failed:\n" + ex);
                ClearControls(false);
            }
        }

        // ==== 控件创建（参考用户测试版的字段名和布局） ====

        private static void EnsureControls(object view)
        {
            if (view == null)
            {
                ClearControls(false);
                return;
            }

            // 同一个 View 且主按钮仍然有效时，不重复创建。
            if (ReferenceEquals(_currentViewInstance, view) && IsAlive(_mainButton))
            {
                UpdateSubControlsVisibility();
                return;
            }

            // ViewBuildingManage 可能被重建。旧引用不能复用。
            ClearControls(true);
            _currentViewInstance = view;
            _isExpanded = false;

            var t = Traverse.Create(view);

            // 按钮模板：优先 btnTemplate，备选 buttonRepair
            object templateBtn = t.Field("btnTemplate").GetValue();
            if (templateBtn == null)
                templateBtn = t.Field("buttonRepair").GetValue();

            Component templateComponent = templateBtn as Component;
            if (!IsAlive(templateComponent))
            {
                Debug.LogWarning("[CopyBuildingModernized] Cannot find valid button template.");
                ClearControls(false);
                return;
            }

            // 文字模板：优先从按钮内部获取按钮级别的 TMP，备选 textTitle
            TextMeshProUGUI textTemplate = templateComponent.GetComponentInChildren<TextMeshProUGUI>(true);
            if (!IsAlive(textTemplate))
                textTemplate = t.Field<TextMeshProUGUI>("textTitle").Value;

            if (!IsAlive(textTemplate))
            {
                Debug.LogWarning("[CopyBuildingModernized] Cannot find valid text template.");
                ClearControls(false);
                return;
            }

            GameObject templateGo = templateComponent.gameObject;
            if (templateGo == null)
            {
                Debug.LogWarning("[CopyBuildingModernized] Button template gameObject is null.");
                ClearControls(false);
                return;
            }

            Transform parent = templateGo.transform.parent;
            if (parent == null)
            {
                Debug.LogWarning("[CopyBuildingModernized] Button template parent is null.");
                ClearControls(false);
                return;
            }

            RectTransform templateRect = templateGo.GetComponent<RectTransform>();
            if (templateRect == null)
            {
                Debug.LogWarning("[CopyBuildingModernized] Button template RectTransform is null.");
                ClearControls(false);
                return;
            }

            Vector2 basePos = templateRect.anchoredPosition;

            Debug.Log($"[CopyBuildingModernized] Creating UI controls at base position: {basePos}");
            Debug.Log("[CopyBuildingModernized] *** VERSION: Vertical Layout with Auto-Reload Enabled ***");

            // 1. 创建主按钮：太吾村建筑管理
            _mainButton = CreateButton(templateGo, parent,
                "CM_MainBtn",
                basePos + new Vector2(0f, 60f),
                new Vector2(180f, 40f),
                "太吾村建筑管理",
                OnClickMainButton);
            Debug.Log($"[CopyBuildingModernized] Main button position: {basePos + new Vector2(0f, 60f)}");

            // 2. 创建子控件（初始隐藏）- 竖排布局
            Debug.Log("[CopyBuildingModernized] *** CREATING VERTICAL LAYOUT ***");
            
            // 导入按钮
            _importButton = CreateButton(templateGo, parent,
                "CM_ImportBtn",
                basePos + new Vector2(0f, 15f),
                new Vector2(160f, 35f),
                "导入蓝图",
                OnClickImport);
            Debug.Log($"[CopyBuildingModernized] Import button position: {basePos + new Vector2(0f, 15f)}");

            // 导出按钮
            _exportButton = CreateButton(templateGo, parent,
                "CM_ExportBtn",
                basePos + new Vector2(0f, -25f),
                new Vector2(160f, 35f),
                "导出蓝图",
                OnClickExport);
            Debug.Log($"[CopyBuildingModernized] Export button position: {basePos + new Vector2(0f, -25f)}");

            // 转换按钮
            _convertButton = CreateButton(templateGo, parent,
                "CM_ConvertBtn",
                basePos + new Vector2(0f, -65f),
                new Vector2(160f, 35f),
                "蓝图转换",
                OnClickConvert);
            Debug.Log($"[CopyBuildingModernized] Convert button position: {basePos + new Vector2(0f, -65f)}");

            // 宽度输入标签
            _widthInputLabel = CreateStatusLabel(textTemplate, parent,
                "CM_WidthInputLabel",
                basePos + new Vector2(0f, -100f),
                new Vector2(80f, 25f));
            if (IsAlive(_widthInputLabel))
            {
                _widthInputLabel.text = "目标大小:";
                _widthInputLabel.alignment = TextAlignmentOptions.Right;
                _widthInputLabel.fontSize = 16;
            }

            // 宽度输入框
            _widthInputField = CreateInputField(textTemplate, parent,
                "CM_WidthInput",
                basePos + new Vector2(85f, -102f),
                new Vector2(60f, 28f),
                "24");

            // 状态标签
            _statusLabel = CreateStatusLabel(textTemplate, parent,
                "CM_StatusLabel",
                basePos + new Vector2(0f, -135f),
                new Vector2(160f, 25f));

            if (!IsAlive(_mainButton) || !IsAlive(_exportButton) || !IsAlive(_importButton) || 
                !IsAlive(_convertButton) || !IsAlive(_statusLabel) || 
                !IsAlive(_widthInputLabel) || !IsAlive(_widthInputField))
            {
                Debug.LogWarning("[CopyBuildingModernized] Failed to create all controls.");
                ClearControls(true);
                return;
            }

            // 验证所有按钮的实际位置
            VerifyButtonPositions();

            // 初始状态：只显示主按钮
            Debug.Log($"[CopyBuildingModernized] Initial state: _isExpanded = {_isExpanded}");
            UpdateSubControlsVisibility();
            Debug.Log("[CopyBuildingModernized] Controls created.");
        }

        private static Component CreateButton(GameObject template,
            Transform parent, string name, Vector2 pos, Vector2 size, string text, Action onClick)
        {
            GameObject clone = null;
            try
            {
                // 复制模板按钮（保留原版样式）
                clone = UnityEngine.Object.Instantiate(template, parent, false);
                clone.name = name;

                RectTransform rect = clone.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = pos;
                    rect.sizeDelta = size;
                    rect.localScale = Vector3.one;
                    
                    // 关键：设置锚点为中心，确保坐标系统一致
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                }

                // 获取 CButton 组件（通过反射，因为 CButton 在 FrameWork 命名空间）
                var cbtnType = AccessTools.TypeByName("FrameWork.UISystem.UIElements.CButton");
                if (cbtnType == null)
                {
                    Debug.LogWarning("[CopyBuildingModernized] CButton type not found.");
                    SafeDestroy(clone);
                    return null;
                }

                Component button = clone.GetComponent(cbtnType);
                if (!IsAlive(button))
                {
                    Debug.LogWarning("[CopyBuildingModernized] CButton component not found on clone.");
                    SafeDestroy(clone);
                    return null;
                }

                // 注册点击事件
                Traverse.Create(button).Method("ClearAndAddListener", new object[] { onClick }).GetValue();

                // 复用按钮自带标签，保留原版字体、字号、颜色
                var label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = text;

                Debug.Log($"[CopyBuildingModernized] Created button '{name}' at position: {pos}");
                return button;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CopyBuildingModernized] CreateButton failed:\n" + ex);
                SafeDestroy(clone);
                return null;
            }
        }

        private static TextMeshProUGUI CreateStatusLabel(TextMeshProUGUI template, Transform parent,
            string name, Vector2 pos, Vector2 size)
        {
            try
            {
                TextMeshProUGUI label = UnityEngine.Object.Instantiate(template, parent, false);
                label.name = name;

                RectTransform rect = label.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = pos;
                    rect.sizeDelta = size;
                    rect.localScale = Vector3.one;
                }

                label.text = "";
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 16f;
                label.color = new Color(0.85f, 0.92f, 0.98f, 1f);
                label.raycastTarget = false;
                return label;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CopyBuildingModernized] CreateStatusLabel failed:\n" + ex);
                return null;
            }
        }

        private static TMP_InputField CreateInputField(TextMeshProUGUI template, Transform parent,
            string name, Vector2 pos, Vector2 size, string placeholder)
        {
            try
            {
                // 创建输入框背景
                GameObject inputGo = new GameObject(name);
                inputGo.transform.SetParent(parent, false);

                RectTransform rect = inputGo.AddComponent<RectTransform>();
                rect.anchoredPosition = pos;
                rect.sizeDelta = size;
                rect.localScale = Vector3.one;

                // 添加背景图片（可选）
                var image = inputGo.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

                // 添加 TMP_InputField 组件
                var inputField = inputGo.AddComponent<TMP_InputField>();
                
                // 创建文本区域
                GameObject textAreaGo = new GameObject("TextArea");
                textAreaGo.transform.SetParent(inputGo.transform, false);
                RectTransform textAreaRect = textAreaGo.AddComponent<RectTransform>();
                textAreaRect.anchorMin = Vector2.zero;
                textAreaRect.anchorMax = Vector2.one;
                textAreaRect.offsetMin = new Vector2(5, 2);
                textAreaRect.offsetMax = new Vector2(-5, -2);

                // 创建文本对象
                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(textAreaGo.transform, false);
                RectTransform textRect = textGo.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(5, 2);
                textRect.offsetMax = new Vector2(-5, -2);

                var textComponent = textGo.AddComponent<TextMeshProUGUI>();
                textComponent.fontSize = 18f;
                textComponent.color = Color.white;
                textComponent.alignment = TextAlignmentOptions.Left;
                textComponent.text = placeholder;

                // 配置输入框
                inputField.textComponent = textComponent;
                inputField.placeholder = null; // 可以后续添加placeholder
                inputField.characterLimit = 3; // 限制3位数字（18-126）
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;

                return inputField;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CopyBuildingModernized] CreateInputField failed:\n" + ex);
                return null;
            }
        }

        // ==== UI 状态 ====

        private static void SetControlsVisible(bool visible)
        {
            // 只控制主按钮的可见性
            SafeSetActive(_mainButton, visible);
            
            // 子控件的可见性由_isExpanded和UpdateSubControlsVisibility控制
            if (visible)
            {
                UpdateSubControlsVisibility();
            }
            else
            {
                // 隐藏时，所有控件都隐藏
                SafeSetActive(_exportButton, false);
                SafeSetActive(_importButton, false);
                SafeSetActive(_convertButton, false);
                SafeSetActive(_statusLabel, false);
                SafeSetActive(_widthInputLabel, false);
                SafeSetActive(_widthInputField, false);
            }
        }

        private static void SetBusy(bool busy)
        {
            SetInteractable(_exportButton, !busy);
            SetInteractable(_importButton, !busy);
            SetInteractable(_convertButton, !busy);
            if (IsAlive(_widthInputField))
                _widthInputField.interactable = !busy;
        }

        private static void SetStatus(string message)
        {
            if (!IsAlive(_statusLabel))
                return;

            _statusLabel.text = message ?? "";
        }

        private static void SetStatusColor(bool success)
        {
            if (!IsAlive(_statusLabel))
                return;

            _statusLabel.color = success
                ? new Color(0.85f, 0.92f, 0.98f, 1f)
                : new Color(0.93f, 0.45f, 0.45f, 1f);
        }

        private static bool IsAlive(UnityEngine.Object obj)
        {
            return obj != null;
        }

        private static void SafeSetActive(Component component, bool visible)
        {
            if (!IsAlive(component))
                return;

            try
            {
                GameObject go = component.gameObject;
                if (go != null)
                {
                    Debug.Log($"[CopyBuildingModernized] SafeSetActive: {go.name} from {go.activeSelf} to {visible}");
                    if (go.activeSelf != visible)
                        go.SetActive(visible);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CopyBuildingModernized] SafeSetActive failed:\n" + ex);
            }
        }

        private static void SafeSetActive(TMP_InputField inputField, bool visible)
        {
            if (!IsAlive(inputField))
                return;

            try
            {
                GameObject go = inputField.gameObject;
                if (go != null && go.activeSelf != visible)
                    go.SetActive(visible);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CopyBuildingModernized] SafeSetActive (InputField) failed:\n" + ex);
            }
        }

        private static void SetInteractable(Component button, bool interactable)
        {
            if (!IsAlive(button))
                return;

            try
            {
                Traverse.Create(button).Property("interactable").SetValue(interactable);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CopyBuildingModernized] SetInteractable failed:\n" + ex);
            }
        }

        private static void ClearControls(bool destroyGameObjects)
        {
            if (destroyGameObjects)
            {
                SafeDestroy(_mainButton);
                SafeDestroy(_exportButton);
                SafeDestroy(_importButton);
                SafeDestroy(_convertButton);
                SafeDestroy(_statusLabel);
                SafeDestroy(_widthInputLabel);
                SafeDestroy(_widthInputField);
            }

            _mainButton = null;
            _exportButton = null;
            _importButton = null;
            _convertButton = null;
            _statusLabel = null;
            _widthInputLabel = null;
            _widthInputField = null;
            _isExpanded = false;
            _currentViewInstance = null;
        }

        private static void SafeDestroy(Component component)
        {
            if (!IsAlive(component))
                return;

            SafeDestroy(component.gameObject);
        }

        private static void SafeDestroy(GameObject go)
        {
            if (go == null)
                return;

            try
            {
                UnityEngine.Object.Destroy(go);
            }
            catch
            {
                // Dispose/域卸载阶段可能不允许 Destroy；这里只需要清引用，避免再访问旧 UI。
            }
        }

        // ==== 导入/导出（调用后端 ModMethod） ====

        private static void OnClickExport()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string defaultName = $"SlimoonCopyBuilding{timestamp}.bin";
            string defaultPath = Path.Combine(Directory.GetCurrentDirectory(), defaultName);
            string filePath = LocalDialog.SelectSaveFilePath(BinFileFilter, Path.GetDirectoryName(defaultPath));
            if (string.IsNullOrWhiteSpace(filePath))
            {
                SetStatus("已取消导出。");
                return;
            }

            // 用户可能没输后缀，补上 .bin
            if (!filePath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                filePath += ".bin";

            SetBusy(true);
            SetStatus("正在导出村庄数据...");
            SerializableModData parameter = new SerializableModData();
            parameter.Set("Path", filePath);
            ModDomainMethod.AsyncCall.CallModMethodWithParamAndRet(
                null, _modIdStr, ExportMethodName, parameter,
                (offset, pool) => HandleResult("导出", false, offset, pool));
        }

        private static void OnClickImport()
        {
            string filePath = LocalDialog.SelectLoadFilePath(BinFileFilter, Directory.GetCurrentDirectory());
            if (string.IsNullOrWhiteSpace(filePath))
            {
                SetStatus("已取消导入。");
                return;
            }

            SetBusy(true);
            SetStatus("正在导入村庄数据...");
            SerializableModData parameter = new SerializableModData();
            parameter.Set("Path", filePath);
            ModDomainMethod.AsyncCall.CallModMethodWithParamAndRet(
                null, _modIdStr, ImportMethodName, parameter,
                (offset, pool) => HandleResult("导入", true, offset, pool));
        }

        private static void OnClickConvert()
        {
            // 获取目标宽度
            string widthStr = IsAlive(_widthInputField) ? _widthInputField.text : "24";
            if (!sbyte.TryParse(widthStr, out sbyte targetWidth) || targetWidth < 18 || targetWidth >= 127)
            {
                SetStatus("错误：目标宽度必须在 18-126 之间");
                SetStatusColor(false);
                return;
            }

            // 选择输入文件
            string inputPath = LocalDialog.SelectLoadFilePath(BinFileFilter, Directory.GetCurrentDirectory());
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                SetStatus("已取消转换。");
                return;
            }

            // 生成输出文件名
            string directory = Path.GetDirectoryName(inputPath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            string outputPath = Path.Combine(directory, $"{fileNameWithoutExt}_width{targetWidth}.bin");

            // 确认输出路径
            SetBusy(true);
            SetStatus($"正在转换蓝图宽度: {targetWidth}...");
            
            SerializableModData parameter = new SerializableModData();
            parameter.Set("InputPath", inputPath);
            parameter.Set("OutputPath", outputPath);
            parameter.Set("TargetWidth", targetWidth.ToString()); // 使用字符串传递
            
            ModDomainMethod.AsyncCall.CallModMethodWithParamAndRet(
                null, _modIdStr, ConvertMethodName, parameter,
                (offset, pool) => HandleConvertResult(offset, pool, outputPath));
        }

        /// <summary>
        /// 主按钮点击：展开/收起子菜单
        /// </summary>
        private static void OnClickMainButton()
        {
            _isExpanded = !_isExpanded;
            UpdateSubControlsVisibility();
            
            string stateText = _isExpanded ? "已展开" : "已收起";
            SetStatus($"建筑管理 {stateText}");
            Debug.Log($"[CopyBuildingModernized] Main button clicked. Expanded: {_isExpanded}");
        }

        /// <summary>
        /// 更新子控件可见性
        /// </summary>
        private static void UpdateSubControlsVisibility()
        {
            bool visible = _isExpanded;
            Debug.Log($"[CopyBuildingModernized] UpdateSubControlsVisibility: visible={visible}");
            
            SafeSetActive(_exportButton, visible);
            SafeSetActive(_importButton, visible);
            SafeSetActive(_convertButton, visible);
            SafeSetActive(_widthInputLabel, visible);
            SafeSetActive(_widthInputField, visible);
            SafeSetActive(_statusLabel, visible);
        }

        /// <summary>
        /// 验证所有按钮的实际位置（用于调试）
        /// </summary>
        private static void VerifyButtonPositions()
        {
            try
            {
                Debug.Log("[CopyBuildingModernized] === VERIFYING BUTTON POSITIONS ===");
                
                if (IsAlive(_mainButton))
                {
                    var rect = ((Component)_mainButton).GetComponent<RectTransform>();
                    if (rect != null)
                        Debug.Log($"[CopyBuildingModernized] MainBtn actual position: {rect.anchoredPosition}");
                }
                
                if (IsAlive(_importButton))
                {
                    var rect = ((Component)_importButton).GetComponent<RectTransform>();
                    if (rect != null)
                        Debug.Log($"[CopyBuildingModernized] ImportBtn actual position: {rect.anchoredPosition}");
                }
                
                if (IsAlive(_exportButton))
                {
                    var rect = ((Component)_exportButton).GetComponent<RectTransform>();
                    if (rect != null)
                        Debug.Log($"[CopyBuildingModernized] ExportBtn actual position: {rect.anchoredPosition}");
                }
                
                if (IsAlive(_convertButton))
                {
                    var rect = ((Component)_convertButton).GetComponent<RectTransform>();
                    if (rect != null)
                        Debug.Log($"[CopyBuildingModernized] ConvertBtn actual position: {rect.anchoredPosition}");
                }
                
                Debug.Log("[CopyBuildingModernized] === VERIFICATION COMPLETE ===");
            }
            catch (Exception ex)
            {
                Debug.LogError("[CopyBuildingModernized] VerifyButtonPositions failed:\n" + ex);
            }
        }

        /// <summary>
        /// 禁用父容器上的所有自动布局组件
        /// </summary>
        private static void DisableAutoLayoutOnParent(Transform parent)
        {
            try
            {
                // 检查并禁用 HorizontalLayoutGroup
                var hlg = parent.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    hlg.enabled = false;
                    Debug.Log("[CopyBuildingModernized] Disabled HorizontalLayoutGroup on parent");
                }

                // 检查并禁用 VerticalLayoutGroup
                var vlg = parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.enabled = false;
                    Debug.Log("[CopyBuildingModernized] Disabled VerticalLayoutGroup on parent");
                }

                // 检查并禁用 GridLayoutGroup
                var glg = parent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                if (glg != null)
                {
                    glg.enabled = false;
                    Debug.Log("[CopyBuildingModernized] Disabled GridLayoutGroup on parent");
                }

                // 检查并禁用 ContentSizeFitter
                var csf = parent.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (csf != null)
                {
                    csf.enabled = false;
                    Debug.Log("[CopyBuildingModernized] Disabled ContentSizeFitter on parent");
                }

                // 检查并禁用 LayoutElement
                var le = parent.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le != null)
                {
                    le.enabled = false;
                    Debug.Log("[CopyBuildingModernized] Disabled LayoutElement on parent");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CopyBuildingModernized] DisableAutoLayoutOnParent failed:\n" + ex);
            }
        }

        // ==== 结果处理（反射反序列化，避免前端加载 Backend DLL 类型失败） ====

        private static void HandleConvertResult(int offset, object pool, string outputPath)
        {
            try
            {
                SerializableModData result = null;

                // 通过反射调用 Serializer.Deserialize，兼容前端运行环境
                var poolType = AccessTools.TypeByName("GameData.Utilities.RawDataPool");
                var deserMethod = AccessTools.Method(
                    typeof(GameData.Serializer.Serializer), "Deserialize",
                    new Type[] { poolType ?? typeof(object), typeof(int), typeof(SerializableModData).MakeByRefType() });

                if (deserMethod != null)
                {
                    var args = new object[] { pool, offset, null };
                    deserMethod.Invoke(null, args);
                    result = args[2] as SerializableModData;
                }

                bool success = false;
                string message = "";
                if (result != null)
                {
                    result.Get("Success", out success);
                    result.Get("Message", out message);
                }

                var msg = string.IsNullOrWhiteSpace(message)
                    ? (success ? "转换完成！" : "转换失败。")
                    : message;

                if (success)
                {
                    msg += $"\n输出文件: {Path.GetFileName(outputPath)}";
                    SetStatus(msg);
                    SetStatusColor(true);
                    Debug.Log($"[CopyBuildingModernized] 转换成功: {outputPath}");
                }
                else
                {
                    SetStatus(msg);
                    SetStatusColor(false);
                    Debug.LogError($"[CopyBuildingModernized] 转换失败: {msg}");
                }
            }
            catch (Exception ex)
            {
                SetStatus("转换异常：" + ex.Message);
                SetStatusColor(false);
                Debug.LogError("[CopyBuildingModernized] HandleConvertResult failed:\n" + ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private static void HandleResult(string action, bool reloadBuildingArea, int offset, object pool)
        {
            try
            {
                SerializableModData result = null;

                // 通过反射调用 Serializer.Deserialize，兼容前端运行环境
                var poolType = AccessTools.TypeByName("GameData.Utilities.RawDataPool");
                var deserMethod = AccessTools.Method(
                    typeof(GameData.Serializer.Serializer), "Deserialize",
                    new Type[] { poolType ?? typeof(object), typeof(int), typeof(SerializableModData).MakeByRefType() });

                if (deserMethod != null)
                {
                    var args = new object[] { pool, offset, null };
                    deserMethod.Invoke(null, args);
                    result = args[2] as SerializableModData;
                }

                bool success = false;
                string message = "";
                if (result != null)
                {
                    result.Get("Success", out success);
                    result.Get("Message", out message);
                }

                var msg = string.IsNullOrWhiteSpace(message)
                    ? (success ? action + "完成。" : action + "失败。")
                    : message;

                SetStatus(msg);
                SetStatusColor(success);

                if (success && reloadBuildingArea)
                {
                    SetStatus(msg + " 正在重载产业界面...");
                    if (!TryReloadBuildingArea())
                        SetStatus(msg + " 请手动退出产业界面后重新进入。");
                }
            }
            catch (Exception ex)
            {
                SetStatus(action + "异常：" + ex.Message);
                SetStatusColor(false);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // 导入会整体替换后端建筑数据，但当前产业总览和单建筑页仍持有导入前的
        // BlockList、BuildingAreaData 与 BuildingBlockData。只刷新 ViewBuildingManage
        // 无法清掉这些跨页面缓存，因此复用游戏原生的“退出产业 -> 重新进入”流程。
        private static bool TryReloadBuildingArea()
        {
            Action reopenBuildingArea = null;

            try
            {
                if (UIManager.Instance == null ||
                    UIElement.BuildingArea == null ||
                    !UIManager.Instance.IsElementActive(UIElement.BuildingArea) ||
                    UIElement.BuildingArea.UiBase == null)
                {
                    Debug.LogWarning("[CopyBuildingModernized] BuildingArea is not active; skipped automatic reload.");
                    return false;
                }

                // 正常进入产业时，世界地图状态在 UI 栈中。没有可返回页面时不强行 StackBack，
                // 避免把玩家留在一个无法恢复的 UI 状态。
                if (!UIManager.Instance.IsInStack(UIElement.WorldMap))
                {
                    Debug.LogWarning("[CopyBuildingModernized] WorldMap is not in UI stack; skipped automatic reload.");
                    return false;
                }

                var areaView = Traverse.Create(UIElement.BuildingArea.UiBase);
                short areaId = areaView.Field<short>("_areaId").Value;
                short blockId = areaView.Field<short>("_blockId").Value;

                reopenBuildingArea = () =>
                {
                    try
                    {
                        var argsBox = EasyPool.Get<ArgumentBox>();
                        argsBox.Set("AreaId", areaId);
                        argsBox.Set("BlockId", blockId);
                        UIElement.BuildingArea.SetOnInitArgs(argsBox);
                        EasyPool.Free<ArgumentBox>(argsBox);

                        CommandManager.AddCommandStackUI(
                            EPriority.StackUINormal,
                            UIElement.StateBuilding);

                        Debug.Log($"[CopyBuildingModernized] Re-entering BuildingArea ({areaId}, {blockId}) after import.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[CopyBuildingModernized] Failed to re-enter BuildingArea after import:\n" + ex);
                    }
                };

                // 必须等产业页完成 OnDisable/OnHide，才重新注册监听并拉取新数据。
                UIElement.BuildingArea.OnHide += reopenBuildingArea;

                if (UIElement.BuildingManage != null &&
                    UIManager.Instance.IsElementActive(UIElement.BuildingManage))
                {
                    UIManager.Instance.HideUI(UIElement.BuildingManage);
                }

                UIManager.Instance.StackBack();
                Debug.Log($"[CopyBuildingModernized] Leaving BuildingArea ({areaId}, {blockId}) after import.");
                return true;
            }
            catch (Exception ex)
            {
                if (reopenBuildingArea != null && UIElement.BuildingArea != null)
                    UIElement.BuildingArea.OnHide -= reopenBuildingArea;

                Debug.LogError("[CopyBuildingModernized] Failed to reload BuildingArea after import:\n" + ex);
                return false;
            }
        }
    }
}
