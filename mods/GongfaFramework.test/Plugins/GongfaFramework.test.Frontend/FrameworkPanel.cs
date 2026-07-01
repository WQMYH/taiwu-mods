using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GongfaFramework.Test.Contracts;
using GongfaFramework.Test.Runtime;
using UnityEngine;

namespace GongfaFramework.Test.Frontend;

internal sealed class FrameworkPanel : MonoBehaviour, IFrameworkView
{
    private static bool _visible;
    private Rect _window = new Rect(80, 50, 1120, 760);
    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private Vector2 _importScroll;
    private int _page;
    private string _search = "";
    private string _sect = "-1";
    private string _type = "-1";
    private string _grade = "-1";
    private GongfaRecord _selected;
    private FrameworkController _controller;
    private string _status = "就绪。";
    private string _editName = "";
    private string _editDesc = "";
    private string _editSect = "";
    private string _editType = "";
    private string _editGrade = "";
    private string _editEquipType = "";
    private string _editOrder = "";
    private string _editBookName = "";
    private string _editBookDesc = "";
    private string _editDirectName = "";
    private string _editDirectDesc = "";
    private string _editReverseName = "";
    private string _editReverseDesc = "";
    private List<string> _imports = new List<string>();
    private string _selectedImport = "";
    private ValidationResult _importPreview;

    private void Awake() => _controller = new FrameworkController(this);

    private void Update()
    {
        if (GUIUtility.keyboardControl == 0 && FrontendRuntime.IsHotkeyPressed()) _visible = !_visible;
        if (_visible && Input.GetKeyDown(KeyCode.Escape)) _visible = false;
    }

    private void OnGUI()
    {
        if (_visible)
            _window = GUI.Window(GetInstanceID(), _window, DrawWindow,
                $"功法框架 0.1.0.0 · {FrontendRuntime.Hotkey.Normalized}");
    }

    public void SetStatus(string message)
    {
        _status = message ?? "";
        FrontendRuntime.Status = _status;
    }

    private void DrawWindow(int id)
    {
        string[] pages = { "总览", "功法浏览", "简易编辑", "导入导出", "诊断", "帮助" };
        _page = GUILayout.Toolbar(_page, pages);
        GUILayout.Label(_status);
        switch (_page)
        {
            case 0: DrawOverview(); break;
            case 1: DrawBrowser(); break;
            case 2: DrawEditor(); break;
            case 3: DrawImportExport(); break;
            case 4: DrawDiagnostics(); break;
            default: DrawHelp(); break;
        }
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭", GUILayout.Width(100))) _visible = false;
        GUILayout.EndHorizontal();
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void DrawOverview()
    {
        GUILayout.Space(10);
        GUILayout.Label($"前端功法数量：{FrontendRuntime.Snapshot.Records.Count}");
        GUILayout.Label($"后端功法数量：{FrontendRuntime.BackendCount}");
        GUILayout.Label($"前端哈希：{FrontendRuntime.Snapshot.Hash}");
        GUILayout.Label($"后端哈希：{FrontendRuntime.BackendHash}");
        GUILayout.Label("一致性：" + (FrontendRuntime.HashMatches ? "通过" : "未通过或后端尚未响应"));
        GUILayout.Label("定义加载：" + FrontendRuntime.LoadResult.Message);
        if (GUILayout.Button("重新请求后端摘要", GUILayout.Width(200))) BackendBridge.RequestSummary();
        GUILayout.Space(15);
        GUILayout.Label("0.1：读取、浏览、编辑、JSON/CSV 导入与 JSON 导出。");
        GUILayout.Label("正式定义在重启游戏后由前后端共同加载。运行中不热替换配置。");
    }

    private void DrawBrowser()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("搜索", GUILayout.Width(45)); _search = GUILayout.TextField(_search, GUILayout.Width(180));
        GUILayout.Label("门派", GUILayout.Width(40)); _sect = GUILayout.TextField(_sect, GUILayout.Width(40));
        GUILayout.Label("类型", GUILayout.Width(40)); _type = GUILayout.TextField(_type, GUILayout.Width(40));
        GUILayout.Label("品级", GUILayout.Width(40)); _grade = GUILayout.TextField(_grade, GUILayout.Width(40));
        if (GUILayout.Button("导出筛选结果", GUILayout.Width(130)))
            _controller.Export(CurrentFilter(), "filtered-gongfa");
        GUILayout.EndHorizontal();

        List<GongfaRecord> values = CurrentFilter();
        GUILayout.Label($"结果：{values.Count}（界面最多绘制前 200 项，导出不受限制）");
        GUILayout.BeginHorizontal();
        _leftScroll = GUILayout.BeginScrollView(_leftScroll, GUILayout.Width(360), GUILayout.Height(610));
        foreach (GongfaRecord item in values.Take(200))
            if (GUILayout.Button($"{item.Id}  {item.Name}  [门派{item.SectId}/类型{item.Type}/品级{item.Grade}]"))
            {
                Select(item);
                _page = 1;
            }
        GUILayout.EndScrollView();
        _rightScroll = GUILayout.BeginScrollView(_rightScroll, GUILayout.Height(610));
        if (_selected == null) GUILayout.Label("请选择一项功法。");
        else
        {
            GUILayout.Label($"ID：{_selected.Id}");
            GUILayout.Label($"名称：{_selected.Name}");
            GUILayout.Label($"说明：{_selected.Description}");
            GUILayout.Label($"门派/类型/品级/装备类型：{_selected.SectId}/{_selected.Type}/{_selected.Grade}/{_selected.EquipType}");
            GUILayout.Label($"秘籍：{_selected.BookId} {_selected.BookName}");
            GUILayout.Label($"秘籍说明：{_selected.BookDescription}");
            GUILayout.Label($"正练特效：{_selected.DirectEffectId} {_selected.DirectEffectName}");
            GUILayout.Label($"逆练特效：{_selected.ReverseEffectId} {_selected.ReverseEffectName}");
            if (GUILayout.Button("导出当前功法")) _controller.Export(new[] { _selected }, $"gongfa-{_selected.Id}");
            if (GUILayout.Button("在简易编辑器中打开")) _page = 2;
            GUILayout.Label("CombatSkill 原始 JSON：");
            GUILayout.TextArea(_selected.CombatSkillJson ?? "", GUILayout.MinHeight(260));
        }
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();
    }

    private void DrawEditor()
    {
        if (_selected == null) { GUILayout.Label("请先在“功法浏览”中选择功法。"); return; }
        _rightScroll = GUILayout.BeginScrollView(_rightScroll, GUILayout.Height(590));
        GUILayout.Label($"正在编辑：{_selected.Id} {_selected.Name}");
        Field("功法名称", ref _editName);
        Field("功法说明", ref _editDesc, 120);
        Field("门派 ID", ref _editSect);
        Field("类型", ref _editType);
        Field("品级", ref _editGrade);
        Field("装备类型", ref _editEquipType);
        Field("门派排序", ref _editOrder);
        Field("秘籍名称", ref _editBookName);
        Field("秘籍说明", ref _editBookDesc, 100);
        Field("正练特效名称", ref _editDirectName);
        Field("正练特效说明", ref _editDirectDesc, 80);
        Field("逆练特效名称", ref _editReverseName);
        Field("逆练特效说明", ref _editReverseDesc, 80);
        GUILayout.Label("数组、复杂结构与特效组件在 0.1 中只读。");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("保存草稿")) SaveEditor(false);
        GUI.enabled = FrontendRuntime.HashMatches;
        if (GUILayout.Button("应用并保存（重启生效）")) SaveEditor(true);
        GUI.enabled = true;
        if (GUILayout.Button("恢复当前值")) Select(_selected);
        GUILayout.EndHorizontal();
        if (!FrontendRuntime.HashMatches) GUILayout.Label("前后端哈希不一致，正式保存已锁定；仍可保存草稿。");
        GUILayout.EndScrollView();
    }

    private void DrawImportExport()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("导出全部功法", GUILayout.Width(160)))
            _controller.Export(FrontendRuntime.Snapshot.Records, "all-gongfa");
        if (GUILayout.Button("扫描 imports", GUILayout.Width(160)))
        {
            _imports = FrameworkFileService.FindImports();
            SetStatus($"发现 {_imports.Count} 个导入文件。");
        }
        GUILayout.EndHorizontal();
        GUILayout.Label("导入目录：" + Path.Combine(FrontendRuntime.ModDirectory, "UserData", "imports"));
        GUILayout.BeginHorizontal();
        _importScroll = GUILayout.BeginScrollView(_importScroll, GUILayout.Width(430), GUILayout.Height(500));
        foreach (string file in _imports)
            if (GUILayout.Button(Path.GetFileName(file)))
            {
                _selectedImport = file;
                _importPreview = DefinitionLoader.ValidateText(File.ReadAllText(file), Path.GetExtension(file));
                SetStatus(_importPreview.Message);
            }
        GUILayout.EndScrollView();
        _rightScroll = GUILayout.BeginScrollView(_rightScroll, GUILayout.Height(500));
        if (_importPreview == null) GUILayout.Label("选择文件后显示差异预览。");
        else
        {
            GUILayout.Label(_selectedImport);
            GUILayout.Label(_importPreview.Success ? "验证通过" : "验证失败");
            foreach (string error in _importPreview.Errors) GUILayout.Label("错误：" + error);
            foreach (PatchDifference diff in _importPreview.Differences.Take(100))
                GUILayout.Label($"{diff.Table}:{diff.TemplateId}.{diff.Field}\n  {diff.Before} → {diff.After}");
            GUI.enabled = _importPreview.Success && FrontendRuntime.HashMatches;
            if (GUILayout.Button("安装到 Definitions（重启生效）"))
            {
                string target = FrameworkFileService.InstallImport(_selectedImport);
                SetStatus("已安装：" + target + "；请重启游戏。");
            }
            GUI.enabled = true;
        }
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();
    }

    private void DrawDiagnostics()
    {
        GUILayout.Label("MOD 目录：" + FrontendRuntime.ModDirectory);
        GUILayout.Label("前端日志：" + FrontendRuntime.LogPath);
        GUILayout.Label("外部定义：" + FrontendRuntime.LoadResult.Message);
        foreach (string error in FrontendRuntime.LoadResult.Errors) GUILayout.Label("定义错误：" + error);
        foreach (string error in FrontendRuntime.Snapshot.Errors) GUILayout.Label("前端读取错误：" + error);
        if (!string.IsNullOrWhiteSpace(FrontendRuntime.BackendErrors))
            GUILayout.TextArea(FrontendRuntime.BackendErrors, GUILayout.MinHeight(180));
        GUILayout.Label("前端程序集：" + typeof(FrameworkPanel).Assembly.FullName);
        GUILayout.Label("共享配置程序集：" + typeof(Config.CombatSkill).Assembly.FullName);
    }

    private void DrawHelp()
    {
        GUILayout.Label("0.1 使用流程");
        GUILayout.Label("1. 在功法浏览中搜索并选择功法。");
        GUILayout.Label("2. 导出原始数据，或进入简易编辑器生成字段补丁。");
        GUILayout.Label("3. 草稿写入 UserData/drafts；正式定义写入 Definitions/user。");
        GUILayout.Label("4. 正式定义需要重启游戏，由前后端同时加载。");
        GUILayout.Label("5. 外部文件放入 UserData/imports，在导入页验证后安装。");
        GUILayout.Space(12);
        GUILayout.Label("未来计划：M2 指定门派新增功法；M3 复用原版特效；M4 JSON 组合效果；");
        GUILayout.Label("M5 按原版梯度随机生成功法；M6 稳定 API 与外部特效 DLL。");
        GUILayout.Label("详细教程：" + Path.Combine(FrontendRuntime.ModDirectory, "docs"));
    }

    private List<GongfaRecord> CurrentFilter()
    {
        int.TryParse(_sect, out int sect); if (string.IsNullOrWhiteSpace(_sect)) sect = -1;
        int.TryParse(_type, out int type); if (string.IsNullOrWhiteSpace(_type)) type = -1;
        int.TryParse(_grade, out int grade); if (string.IsNullOrWhiteSpace(_grade)) grade = -1;
        return _controller.Filter(_search, sect, type, grade);
    }

    private void Select(GongfaRecord value)
    {
        _selected = value;
        _editName = value.Name;
        _editDesc = value.Description;
        _editSect = value.SectId.ToString();
        _editType = value.Type.ToString();
        _editGrade = value.Grade.ToString();
        _editEquipType = value.EquipType.ToString();
        _editOrder = value.OrderIdInSect.ToString();
        _editBookName = value.BookName;
        _editBookDesc = value.BookDescription;
        ReadEffectText(value.DirectEffectJson, out _editDirectName, out _editDirectDesc);
        ReadEffectText(value.ReverseEffectJson, out _editReverseName, out _editReverseDesc);
    }

    private void SaveEditor(bool formal)
    {
        var fields = new Dictionary<string, object>();
        AddChanged(fields, "Name", _selected.Name, _editName);
        AddChanged(fields, "Desc", _selected.Description, _editDesc);
        bool valid = true;
        valid &= AddNumber(fields, "SectId", _selected.SectId, _editSect, sbyte.MinValue, sbyte.MaxValue);
        valid &= AddNumber(fields, "Type", _selected.Type, _editType, 0, 13);
        valid &= AddNumber(fields, "Grade", _selected.Grade, _editGrade, 0, 8);
        valid &= AddNumber(fields, "EquipType", _selected.EquipType, _editEquipType, -1, 4);
        valid &= AddNumber(fields, "OrderIdInSect", _selected.OrderIdInSect, _editOrder, sbyte.MinValue, sbyte.MaxValue);
        if (!valid) return;
        if (fields.Count > 0)
            _controller.Save(new PatchDefinition { Table = "CombatSkill", TemplateId = _selected.Id, Fields = fields }, formal);
        if (_selected.BookId >= 0 && (_editBookName != _selected.BookName || _editBookDesc != _selected.BookDescription))
        {
            var book = new Dictionary<string, object>();
            AddChanged(book, "Name", _selected.BookName, _editBookName);
            AddChanged(book, "Desc", _selected.BookDescription, _editBookDesc);
            _controller.Save(new PatchDefinition { Table = "SkillBook", TemplateId = _selected.BookId, Fields = book }, formal);
        }
        SaveEffect(_selected.DirectEffectId, _selected.DirectEffectJson, _editDirectName, _editDirectDesc, formal);
        SaveEffect(_selected.ReverseEffectId, _selected.ReverseEffectJson, _editReverseName, _editReverseDesc, formal);
        if (fields.Count == 0 && _editBookName == _selected.BookName && _editBookDesc == _selected.BookDescription
            && !EffectChanged(_selected.DirectEffectJson, _editDirectName, _editDirectDesc)
            && !EffectChanged(_selected.ReverseEffectJson, _editReverseName, _editReverseDesc))
            SetStatus("没有字段发生变化。");
    }

    private bool AddNumber(Dictionary<string, object> fields, string name, int original, string text, int min, int max)
    {
        if (!int.TryParse(text, out int value) || value < min || value > max)
        {
            SetStatus($"{name} 必须在 {min}～{max}。");
            return false;
        }
        if (value != original) fields[name] = value;
        return true;
    }

    private void SaveEffect(int id, string json, string name, string desc, bool formal)
    {
        if (id < 0 || !EffectChanged(json, name, desc)) return;
        ReadEffectText(json, out string oldName, out string oldDesc);
        var fields = new Dictionary<string, object>();
        AddChanged(fields, "Name", oldName, name);
        AddChanged(fields, "Desc[0]", oldDesc, desc);
        _controller.Save(new PatchDefinition { Table = "SpecialEffect", TemplateId = id, Fields = fields }, formal);
    }

    private static bool EffectChanged(string json, string name, string desc)
    {
        ReadEffectText(json, out string oldName, out string oldDesc);
        return oldName != (name ?? "") || oldDesc != (desc ?? "");
    }

    private static void ReadEffectText(string json, out string name, out string desc)
    {
        name = ""; desc = "";
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var value = Newtonsoft.Json.Linq.JObject.Parse(json);
            name = value["Name"]?.ToString() ?? "";
            desc = value["Desc"] is Newtonsoft.Json.Linq.JArray array && array.Count > 0
                ? array[0]?.ToString() ?? "" : value["Desc"]?.ToString() ?? "";
        }
        catch { }
    }

    private static void AddChanged(Dictionary<string, object> fields, string name, string original, string value)
    {
        if ((original ?? "") != (value ?? "")) fields[name] = value ?? "";
    }

    private static void Field(string label, ref string value, float height = 24)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(110));
        value = height > 30 ? GUILayout.TextArea(value ?? "", GUILayout.Height(height)) : GUILayout.TextField(value ?? "");
        GUILayout.EndHorizontal();
    }
}
