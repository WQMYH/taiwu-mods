using System;
using FrameWork.UISystem.UIElements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoMonthlyEvent.Executor.Frontend
{
    /// <summary>
    /// 参考 CharacterStudio.test：入口仍位于月度页面，设置内容使用独立、可拖动、分页且可滚动的 IMGUI 窗口。
    /// 这样不会挤压原版月度布局，也能在低分辨率下完整访问所有选项。
    /// </summary>
    internal static class MonthlySettingsPanel
    {
        private const string ButtonName = "AutoMonthlyEventSettingsButton";
        private static PanelHost? _host;
        private static GameObject? _entryButton;

        public static void EnsureButton(UI_MonthlyEvent instance)
        {
            EnsureHost();
            CButton source = instance.CGet<CButton>("DefaultAll");
            if (source == null || source.transform.parent == null)
                return;

            Transform existing = source.transform.parent.Find(ButtonName);
            GameObject button = existing == null ? CreateEntryButton(source) : existing.gameObject;
            RectTransform sourceRect = source.transform as RectTransform;
            RectTransform targetRect = button.transform as RectTransform;
            if (sourceRect != null && targetRect != null)
            {
                targetRect.anchorMin = sourceRect.anchorMin;
                targetRect.anchorMax = sourceRect.anchorMax;
                targetRect.pivot = sourceRect.pivot;
                targetRect.sizeDelta = sourceRect.sizeDelta;
                targetRect.anchoredPosition = sourceRect.anchoredPosition
                    + new Vector2((sourceRect.sizeDelta.x > 0 ? sourceRect.sizeDelta.x : 140f) + 12f, 0f);
            }

            Button click = button.GetComponent<Button>();
            click.onClick.RemoveAllListeners();
            click.onClick.AddListener(() => _host?.Toggle());
        }

        public static void Dispose()
        {
            if (_entryButton != null)
                UnityEngine.Object.Destroy(_entryButton);
            if (_host != null)
                UnityEngine.Object.Destroy(_host.gameObject);
            _entryButton = null;
            _host = null;
        }

        private static void EnsureHost()
        {
            if (_host != null)
                return;
            GameObject go = new GameObject("AutoMonthlyEvent.SettingsUI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<PanelHost>();
        }

        private static GameObject CreateEntryButton(CButton source)
        {
            GameObject go = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(source.transform.parent, false);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.43f, 0.20f, 0.08f, 0.96f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI sourceText = source.GetComponentInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI text;
            if (sourceText != null)
            {
                text = UnityEngine.Object.Instantiate(sourceText, go.transform, false);
                text.text = "自动处理设置";
            }
            else
            {
                GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(go.transform, false);
                text = textGo.GetComponent<TextMeshProUGUI>();
                text.text = "自动处理设置";
                text.fontSize = 22f;
                text.color = new Color(0.94f, 0.88f, 0.74f, 1f);
                text.alignment = TextAlignmentOptions.Center;
            }
            RectTransform textRect = text.transform as RectTransform;
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
            _entryButton = go;
            return go;
        }

        private sealed class PanelHost : MonoBehaviour
        {
            private bool _visible;
            private int _page;
            private Vector2 _scroll;
            private Rect _window = new Rect(70, 45, 820, 760);
            private string _status = "设置会保存到 UserData/settings.json，并在下一项月度事件处理前生效。";
            private string _threshold = string.Empty;
            private string _adoptionAge = string.Empty;
            private string _generation = string.Empty;
            private string _givenName = string.Empty;

            public void Toggle()
            {
                _visible = !_visible;
                if (_visible)
                    LoadFields();
            }

            private void Update()
            {
                if (_visible && Input.GetKeyDown(KeyCode.Escape))
                    _visible = false;
            }

            private void OnGUI()
            {
                if (!_visible)
                    return;
                _window.width = Mathf.Min(820f, Screen.width - 24f);
                _window.height = Mathf.Min(760f, Screen.height - 24f);
                _window.x = Mathf.Clamp(_window.x, 0f, Mathf.Max(0f, Screen.width - _window.width));
                _window.y = Mathf.Clamp(_window.y, 0f, Mathf.Max(0f, Screen.height - _window.height));
                _window = GUI.Window(GetInstanceID(), _window, DrawWindow, "指定事件自动处理");
            }

            private void DrawWindow(int id)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                string[] pages = { "总览", "人物请求", "怀孕与生育", "人情往来", "剧情与对话", "帮助与日志" };
                for (int i = 0; i < pages.Length; i++)
                {
                    if (GUILayout.Toggle(_page == i, pages[i], GUI.skin.button, GUILayout.Height(34)))
                    {
                        if (_page != i) _scroll = Vector2.zero;
                        _page = i;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUIStyle statusStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, wordWrap = true };
                GUILayout.Box(_status, statusStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(42));
                GUILayout.Space(6);

                _scroll = GUILayout.BeginScrollView(_scroll);
                switch (_page)
                {
                    case 0: DrawOverview(); break;
                    case 1: DrawRequests(); break;
                    case 2: DrawFamily(); break;
                    case 3: DrawSocial(); break;
                    case 4: DrawDialogInfo(); break;
                    default: DrawHelp(); break;
                }
                GUILayout.EndScrollView();

                GUILayout.FlexibleSpace();
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("保存并立即应用", GUILayout.Height(38)))
                    SaveFields();
                if (GUILayout.Button("重新读取设置", GUILayout.Height(38)))
                {
                    MonthlyAutomationSettings.Reload();
                    LoadFields();
                    _status = "已重新读取设置文件。";
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("关闭", GUILayout.Width(110), GUILayout.Height(38)))
                    _visible = false;
                GUILayout.EndHorizontal();
                GUI.DragWindow(new Rect(0, 0, 10000, 28));
            }

            private static void DrawOverview()
            {
                MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
                Section("自动处理总开关", "只影响明确登记的事件；其他月度事件不会被接管。");
                s.Enabled = GUILayout.Toggle(s.Enabled, "启用指定月度事件自动处理");
                GUILayout.Space(12);
                Section("功能分组", "关闭某一组后，该组事件会正常进入原版月度事件列表。");
                s.EnableRequests = GUILayout.Toggle(s.EnableRequests, "人物请求（66～87）");
                s.EnableFamily = GUILayout.Toggle(s.EnableFamily, "怀孕、生育、取名与无名遗孤");
                s.EnableSocial = GUILayout.Toggle(s.EnableSocial, "指点武学、推恩施义与笼络人心");
                s.EnableResultSkip = GUILayout.Toggle(s.EnableResultSkip, "自动略过痛失与指定难产结果");
                GUILayout.Space(14);
                GUILayout.Label("当前接管范围：13～32、66～87、109、110、280、281 中已登记的事件。");
                GUILayout.Label("10～12及所有未登记事件保持原版。");
            }

            private void DrawRequests()
            {
                MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
                Section("请求处理规则", "命中允许关系或关系数值达到门槛时自动同意，否则自动婉拒。");
                s.EnableRequests = GUILayout.Toggle(s.EnableRequests, "自动处理 66～87 的所有请求");
                GUILayout.Space(8);
                GUILayout.Label("无条件自动同意的关系");
                s.RelationMode = GUILayout.SelectionGrid(
                    Mathf.Clamp(s.RelationMode - 1, 0, 2),
                    new[] { "亲子、配偶", "再加结义、同道", "所有正面关系" }, 3) + 1;
                GUILayout.Space(8);
                Field("其他人物自动同意门槛", ref _threshold, "默认 25000；低于该值时自动婉拒。");
                GUILayout.Space(12);
                Section("覆盖事件", "外伤、内伤、驱毒、续命、内息、内力、灭蛊、食物、茶酒、资源、物品、修理、淬毒、技艺、武艺、研读、突破与切磋请求。");
            }

            private void DrawFamily()
            {
                MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
                Section("怀孕与胎教", "数字表示事件中从上到下第几个可用选项；目标不存在时转为原版窗口。");
                GUILayout.Label("身怀六甲默认选择");
                s.PregnancyChoice = GUILayout.SelectionGrid(Mathf.Clamp(s.PregnancyChoice - 1, 0, 4), new[] { "第1项", "第2项", "第3项", "第4项", "第5项" }, 5) + 1;
                GUILayout.Label("母亲胎教默认选择");
                s.PrenatalChoice = GUILayout.SelectionGrid(Mathf.Clamp(s.PrenatalChoice - 1, 0, 2), new[] { "第1项", "第2项", "第3项" }, 3) + 1;
                GUILayout.Space(14);

                Section("生育与取名", "优先选择随太吾姓；没有该选项时选择首个可用答案。名字最多使用两个字符。");
                s.EnableFamily = GUILayout.Toggle(s.EnableFamily, "自动处理生育、取名和无名遗孤");
                Field("字辈（可留空）", ref _generation, "例如“清”。");
                Field("名字字符（可留空）", ref _givenName, "例如“和”；组合结果为“清和”。");
                GUILayout.Space(14);

                Section("无名遗孤", "可固定收养、固定拒绝，或沿用年龄和立场筛选。");
                s.AdoptionMode = GUILayout.SelectionGrid(Mathf.Clamp(s.AdoptionMode, 0, 2),
                    new[] { "总是拒绝", "总是收养", "按年龄与立场判断" }, 3);
                if (s.AdoptionMode == 2)
                    Field("允许收养的最大年龄", ref _adoptionAge, "默认 3 岁。");
                GUILayout.Space(14);

                Section("结果自动略过", "完成原版事件流程后从月度集合移除，不只是隐藏界面。");
                s.EnableResultSkip = GUILayout.Toggle(s.EnableResultSkip, "略过 14～19、24～27 的痛失与难产结果");
            }

            private static void DrawSocial()
            {
                MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
                Section("人情往来", "为每类事件指定从上到下第几个可用答案。选项变化时不会盲目执行。");
                s.EnableSocial = GUILayout.Toggle(s.EnableSocial, "自动处理人情往来");
                Choice("指点武学", ref s.GuidanceChoice);
                Choice("推恩施义", ref s.BenevolenceChoice);
                Choice("笼络人心", ref s.RecruitChoice);
            }

            private static void DrawDialogInfo()
            {
                Section("普通剧情与对话", "这部分沿用模组现有处理器，与指定月度事件白名单相互独立。");
                GUILayout.Label("• 记住玩家手动选择并在相同场景复用");
                GUILayout.Label("• 安全单选项自动继续");
                GUILayout.Label("• 请求结果和指点结果自动退出");
                GUILayout.Label("• 关键词选择与自定义对话跳过");
                GUILayout.Space(12);
                GUILayout.Label("现有选项仍从原模组设置迁移读取。本页面不会把剧情事件误认成月度事件。");
            }

            private static void DrawHelp()
            {
                Section("工作方式", "指定事件会在原版月度列表渲染前执行；处理完成后重新获取集合，因此不会出现在最终列表。");
                GUILayout.Label("自动选择失败时会显示当前原版事件窗口，供玩家直接完成。");
                GUILayout.Label("未登记事件不会进入自动队列。");
                GUILayout.Space(14);
                Section("设置与日志", "技术标识只写入日志，普通设置页面不显示 GUID 或 OptionKey。");
                GUILayout.TextArea("设置文件：\n" + MonthlyAutomationSettings.FilePath + "\n\n日志目录：\nMod/AutoMonthlyEvent.Executor/Logs", GUILayout.MinHeight(100));
            }

            private void LoadFields()
            {
                MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
                _threshold = s.FavorabilityThreshold.ToString();
                _adoptionAge = s.AdoptionMaxAge.ToString();
                _generation = s.GenerationCharacter;
                _givenName = s.GivenNameCharacter;
            }

            private void SaveFields()
            {
                MonthlyAutomationSettings s = MonthlyAutomationSettings.Current;
                s.FavorabilityThreshold = Parse(_threshold, 25000, 0, 60000);
                s.AdoptionMaxAge = Parse(_adoptionAge, 3, 0, 18);
                s.GenerationCharacter = OneChar(_generation);
                s.GivenNameCharacter = OneChar(_givenName);
                _generation = s.GenerationCharacter;
                _givenName = s.GivenNameCharacter;
                MonthlyAutomationSettings.Save();
                _status = "设置已保存并立即应用。正在处理的单个事件仍使用开始处理时的规则。";
            }

            private static void Section(string title, string help)
            {
                GUILayout.Label(title, GUI.skin.box, GUILayout.Height(30));
                GUIStyle style = new GUIStyle(GUI.skin.label) { wordWrap = true };
                GUILayout.Label(help, style);
                GUILayout.Space(6);
            }

            private static void Choice(string label, ref int value)
            {
                GUILayout.Space(6);
                GUILayout.Label(label);
                value = GUILayout.SelectionGrid(Mathf.Clamp(value - 1, 0, 4),
                    new[] { "第1项", "第2项", "第3项", "第4项", "第5项" }, 5) + 1;
            }

            private static void Field(string label, ref string value, string help)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(190));
                value = GUILayout.TextField(value ?? string.Empty, GUILayout.MinWidth(180));
                GUILayout.EndHorizontal();
                GUIStyle style = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 };
                GUILayout.Label(help, style);
            }

            private static int Parse(string text, int fallback, int min, int max) =>
                int.TryParse(text, out int value) ? Math.Max(min, Math.Min(max, value)) : fallback;

            private static string OneChar(string value) =>
                string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Substring(0, 1);
        }
    }
}
