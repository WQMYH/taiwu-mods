using System;
using GameData.Domains.Mod;
using UnityEngine;

namespace CharacterStudio.Frontend;

internal sealed class CharacterStudioPanel : MonoBehaviour
{
    private static bool _visible;
    private static string _count = "1";
    private static string _age = "18";
    private static string _attraction = "550";
    private static string _surname = "";
    private static string _givenName = "";
    private static int _gender = 2;
    private Rect _rect = new(80, 80, 360, 410);

    internal static void PollHotkey()
    {
        if (Input.GetKeyDown(KeyCode.F9))
            _visible = !_visible;
    }

    private void OnGUI()
    {
        if (_visible)
            _rect = GUI.Window(GetInstanceID(), _rect, Draw, "人物工坊");
    }

    private static void Draw(int id)
    {
        GUILayout.Label("创建太吾村民（F9 开关）");
        _gender = GUILayout.SelectionGrid(_gender, new[] { "女", "男", "随机" }, 3);
        Field("数量", ref _count);
        Field("年龄", ref _age);
        Field("魅力", ref _attraction);
        Field("姓氏（留空随机）", ref _surname);
        Field("名字（留空随机）", ref _givenName);

        if (GUILayout.Button("创建村民", GUILayout.Height(34)))
        {
            var data = new SerializableModData();
            data.Set("Count", Parse(_count, 1, 1, 100));
            data.Set("Gender", _gender == 2 ? -1 : _gender);
            data.Set("Age", Parse(_age, 18, 3, 100));
            data.Set("Attraction", Parse(_attraction, 550, 0, 999));
            data.Set("Surname", _surname.Trim());
            data.Set("GivenName", _givenName.Trim());
            ModDomainMethod.Call.CallModMethodWithParam(
                FrontendEntry.ModId, "CreateCharacters", data);
            _visible = false;
        }
        if (GUILayout.Button("关闭"))
            _visible = false;
        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private static void Field(string label, ref string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(130));
        value = GUILayout.TextField(value);
        GUILayout.EndHorizontal();
    }

    private static int Parse(string text, int fallback, int min, int max) =>
        int.TryParse(text, out int value) ? Math.Max(min, Math.Min(max, value)) : fallback;
}
