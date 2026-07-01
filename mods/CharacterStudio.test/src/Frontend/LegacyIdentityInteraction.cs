using System;
using System.Reflection;
using FrameWork;
using Game.Views.EventWindow;
using GameData.Domains.Character.Display;
using GameData.Domains.Mod;
using GameData.Domains.TaiwuEvent.DisplayEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CharacterStudio.Frontend;

internal static class LegacyIdentityInteraction
{
    internal static void OnUiElementShow(ArgumentBox args)
    {
        if (!FrontendEntry.RevealPreviousIdentity ||
            !args.Get("Element", out UIElement element) ||
            element.Name != "ViewEventWindow")
            return;

        ViewEventWindow window = element.UiBaseAs<ViewEventWindow>();
        if (window == null)
            return;

        LegacyIdentityInteractionHost host =
            window.GetComponentInChildren<LegacyIdentityInteractionHost>(true);
        if (host == null)
        {
            var go = new GameObject("CharacterStudio.LegacyIdentity");
            go.transform.SetParent(window.transform, false);
            host = go.AddComponent<LegacyIdentityInteractionHost>();
        }
        host.Bind(window);
    }
}

internal sealed class LegacyIdentityInteractionHost : MonoBehaviour
{
    private const short InteractTabIndex = 5;
    private static readonly FieldInfo? OptionContainerField =
        typeof(ViewEventWindow).GetField("optionContainer", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? OptionItemField =
        typeof(ViewEventWindow).GetField("optionItem", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? CurrentTabField =
        typeof(ViewEventWindow).GetField("_lastUserSelectedCommonOptionIndex",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? DescriptionField =
        typeof(EventWindowOption).GetField("txtOptionDesc", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? OptionIndexField =
        typeof(EventWindowOption).GetField("optionIndex", BindingFlags.Instance | BindingFlags.NonPublic);

    private ViewEventWindow? _window;
    private GameObject? _buttonObject;
    private int _npcId = -1;
    private bool _rebuild;

    internal void Bind(ViewEventWindow window)
    {
        _window = window;
        RefreshTarget();
        _rebuild = true;
        GEvent.Remove(UiEvents.OnEventWindowDisplayDataChanged, OnDisplayDataChanged);
        GEvent.Add(UiEvents.OnEventWindowDisplayDataChanged, OnDisplayDataChanged);
    }

    private void OnDestroy()
    {
        GEvent.Remove(UiEvents.OnEventWindowDisplayDataChanged, OnDisplayDataChanged);
        DestroyButton();
    }

    private void OnDisplayDataChanged(ArgumentBox _)
    {
        RefreshTarget();
        _rebuild = true;
    }

    private void LateUpdate()
    {
        if (!_rebuild)
            return;
        _rebuild = false;
        DestroyButton();
        if (IsMainInteractionPage())
            CreateButton();
    }

    private void RefreshTarget()
    {
        EventModel model = SingletonObject.getInstance<EventModel>();
        TaiwuEventDisplayData? data = model?.DisplayingEventData;
        CharacterDisplayData? target = data?.TargetCharacter;
        _npcId = target != null && target.CreatingType == 1 ? target.CharacterId : -1;
    }

    private bool IsMainInteractionPage()
    {
        if (_window == null || _npcId < 0 ||
            CurrentTabField?.GetValue(_window) is not short tab ||
            tab != InteractTabIndex)
            return false;
        EventModel model = SingletonObject.getInstance<EventModel>();
        return model?.DisplayingEventData?.ExtraData?.ShowCommonOptionIndex >= 0;
    }

    private void CreateButton()
    {
        if (_window == null ||
            OptionContainerField?.GetValue(_window) is not RectTransform container ||
            OptionItemField?.GetValue(_window) is not EventWindowOption template)
        {
            Debug.LogWarning("[CharacterStudio] 无法解析事件窗口选项容器，袒露身份注入已跳过。");
            return;
        }

        _buttonObject = Instantiate(template.gameObject, container, false);
        _buttonObject.name = "CharacterStudio.RevealPreviousIdentity";
        EventWindowOption option = _buttonObject.GetComponent<EventWindowOption>();
        if (option == null)
        {
            DestroyButton();
            return;
        }

        if (DescriptionField?.GetValue(option) is TextMeshProUGUI description)
            description.text = "袒露身份";
        else
        {
            DestroyButton();
            return;
        }
        TextMeshProUGUI? index = OptionIndexField?.GetValue(option) as TextMeshProUGUI;
        if (index != null)
            index.text = "[Mod]";

        foreach (TextMeshProUGUI text in _buttonObject.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (text != description && text != index)
                text.text = "";

        Button button = _buttonObject.GetComponentInChildren<Button>(true);
        if (button == null)
        {
            DestroyButton();
            return;
        }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(RevealIdentity);
        Destroy(option);
        _buttonObject.transform.SetAsLastSibling();
        _buttonObject.SetActive(true);
    }

    private void RevealIdentity()
    {
        if (_npcId < 0)
            return;
        var data = new SerializableModData();
        data.Set("NpcId", _npcId);
        ModDomainMethod.Call.CallModMethodWithParam(
            FrontendEntry.ModId, "RevealPreviousIdentity", data);
    }

    private void DestroyButton()
    {
        if (_buttonObject != null)
            Destroy(_buttonObject);
        _buttonObject = null;
    }
}
