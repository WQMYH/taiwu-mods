using System;
using GameData.Domains.Mod;
using UnityEngine;
using static CharacterStudio.Frontend.StudioLocalization;

namespace CharacterStudio.Frontend;

internal sealed class CharacterStudioPanel : MonoBehaviour
{
    private static bool _visible, _confirm;
    private static int _page;
    private static Vector2 _scroll;
    private static Rect _rect = new(55, 40, 760, 740);
    private static Rect _confirmRect = new(220, 170, 400, 160);
    private static string _status = "";
    private static string _count = "1", _profile = "full_positive_villager";
    private static string _age = "", _attraction = "", _surname = "", _given = "", _npcId = "";
    private static int _gender;
    private static string _editId = "my_profile", _editName = "My Profile";
    private static string _health = "100", _morality = "0";
    private static int _ruleMode = 3;
    private static string _ruleValue = "100", _ruleMin = "80", _ruleMax = "120";
    private static bool _removeNegative = true, _allPositive = true;

    private static FrontendStudioSettings S => FrontendEntry.UiSettings;
    private static readonly int[] Relations = { 0, 8192, 512, 16384, 1024, 32768 };
    private static readonly string[] RelationKeys =
        { "common.none", "relation.friend", "relation.sworn", "relation.adored", "relation.spouse", "relation.enemy" };

    internal static void PollHotkey(KeyCode key)
    {
        if (_visible && Input.GetKeyDown(KeyCode.Escape)) { _visible = false; return; }
        if (GUIUtility.keyboardControl == 0 && Input.GetKeyDown(key)) _visible = !_visible;
    }

    internal static void RequestLegacyPassingConfirmation()
    {
        if (GameApp.Instance != null && GameApp.Instance.GetCurrentGameStateName() == EGameState.InGame)
            _confirm = true;
    }

    private void OnGUI()
    {
        if (_visible)
        {
            _rect.width = Mathf.Min(_rect.width, Screen.width - 20);
            _rect.height = Mathf.Min(_rect.height, Screen.height - 20);
            _rect.x = Mathf.Clamp(_rect.x, 0, Mathf.Max(0, Screen.width - _rect.width));
            _rect.y = Mathf.Clamp(_rect.y, 0, Mathf.Max(0, Screen.height - _rect.height));
            _rect = GUI.Window(GetInstanceID(), _rect, Draw, $"{T("title")} · {FrontendEntry.PanelKey}");
        }
        if (_confirm)
            _confirmRect = GUI.Window(GetInstanceID() + 1, _confirmRect, DrawConfirm, T("legacy.immediate"));
    }

    private static void Draw(int id)
    {
        string[] keys = { "page.villager", "page.template", "page.friend", "page.legacy", "page.help" };
        GUILayout.BeginHorizontal();
        for (int i = 0; i < keys.Length; i++)
            if (GUILayout.Toggle(_page == i, T(keys[i]), GUI.skin.button)) _page = i;
        GUILayout.EndHorizontal();
        if (_status.Length > 0) GUILayout.Label(_status);
        _scroll = GUILayout.BeginScrollView(_scroll);
        if (_page == 0) DrawVillagers();
        else if (_page == 1) DrawTemplate();
        else if (_page == 2) DrawFriends();
        else if (_page == 3) DrawLegacy();
        else DrawHelp();
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (_page != 1 && GUILayout.Button(T("common.save"), GUILayout.Height(32))) SaveSettings();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(T("common.close"), GUILayout.Width(110))) _visible = false;
        GUILayout.EndHorizontal();
        GUI.DragWindow(new Rect(0, 0, 10000, 25));
    }

    private static void DrawVillagers()
    {
        GUILayout.Label(T("villager.sources"));
        S.EnableManualCreate = GUILayout.Toggle(S.EnableManualCreate, T("villager.create"));
        S.ProcessInitialVillage = GUILayout.Toggle(S.ProcessInitialVillage, "Initial village");
        S.ProcessRecruitCreated = GUILayout.Toggle(S.ProcessRecruitCreated, "Recruited characters");
        S.ProcessJoinedVillage = GUILayout.Toggle(S.ProcessJoinedVillage, "New villagers");
        S.ProcessExistingVillagers = GUILayout.Toggle(S.ProcessExistingVillagers, "Existing villagers each month");
        S.ExpandVillageCapacity = GUILayout.Toggle(S.ExpandVillageCapacity, "Expand village capacity");
        S.VillageCapacity = IntField("Capacity", S.VillageCapacity, 0, 10000);
        Field("Initial profile", ref S.InitialVillageProfile);
        Field("Recruit profile", ref S.RecruitCreatedProfile);
        Field("Joined profile", ref S.JoinedVillageProfile);
        Field("Monthly profile", ref S.ExistingVillagerProfile);
        GUILayout.Space(8);
        Field(T("common.profile"), ref _profile);
        _gender = Choice("Gender", _gender, new[] { "Profile", "Female", "Male" });
        Field(T("common.count"), ref _count);
        Field("Age override", ref _age); Field("Attraction override", ref _attraction);
        Field("Surname", ref _surname); Field("Given name", ref _given);
        if (GUILayout.Button(T("villager.create"), GUILayout.Height(38)))
        {
            var data = new SerializableModData();
            data.Set("Count", Parse(_count, 1, 1, 100)); data.Set("ProfileId", _profile.Trim());
            data.Set("GenderOverride", _gender == 0 ? -1 : _gender - 1);
            data.Set("AgeOverride", ParseOptional(_age, 3, 100));
            data.Set("AttractionOverride", ParseOptional(_attraction, 0, 999));
            data.Set("Surname", _surname.Trim()); data.Set("GivenName", _given.Trim());
            Call("CreateCharacters", data); _status = T("status.created");
        }
        GUILayout.Label(T("villager.batch"));
        S.VillagerBatchRelationType = RelationChoice(S.VillagerBatchRelationType);
        S.VillagerBatchFavorability = IntField(T("common.favor"), S.VillagerBatchFavorability, -30000, 30000);
        DrawReincarnation("Villager", ref S.EnableVillagerReincarnation,
            ref S.VillagerReincarnationSource, ref S.VillagerReincarnationCount, ref S.VillagerReincarnationProfile);
    }

    private static void DrawTemplate()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Vanilla Safe")) { _editId = "my_vanilla"; _health = "100"; _morality = "0"; _ruleMode = 0; _removeNegative = _allPositive = false; }
        if (GUILayout.Button("Full Positive")) { _editId = "my_positive"; _health = "10000"; _morality = "250"; _ruleMode = 3; _removeNegative = _allPositive = true; }
        GUILayout.EndHorizontal();
        Field("Template ID", ref _editId); Field("Name", ref _editName);
        Field(T("template.health"), ref _health);
        Field(T("template.morality"), ref _morality);
        GUILayout.Label(MoralityName(Parse(_morality, 0, -500, 500)));
        _ruleMode = Choice("Attributes / qualifications", _ruleMode,
            new[] { "Keep", "Minimum", "Override", "Random range" });
        if (_ruleMode is 1 or 2) Field("Value", ref _ruleValue);
        else if (_ruleMode == 3) { Field("Minimum", ref _ruleMin); Field("Maximum", ref _ruleMax); }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All keep")) _ruleMode = 0;
        if (GUILayout.Button("All min 80")) { _ruleMode = 1; _ruleValue = "80"; }
        if (GUILayout.Button("All 100")) { _ruleMode = 2; _ruleValue = "100"; }
        if (GUILayout.Button("Random 80-120")) { _ruleMode = 3; _ruleMin = "80"; _ruleMax = "120"; }
        GUILayout.EndHorizontal();
        _removeNegative = GUILayout.Toggle(_removeNegative, "Remove negative features");
        _allPositive = GUILayout.Toggle(_allPositive, "Add all positive features");
        if (GUILayout.Button("All positive")) { _removeNegative = true; _allPositive = true; }
        if (GUILayout.Button("Save user template", GUILayout.Height(36))) SaveTemplate();
    }

    private static void DrawFriends()
    {
        S.SyncCloseFriend = GUILayout.Toggle(S.SyncCloseFriend, "Sync Taiwu attributes and features");
        S.CloseFriendCount = IntField(T("common.count"), S.CloseFriendCount, 1, 10);
        S.CloseFriendGender = Choice("Gender", S.CloseFriendGender, new[] { "Original", "Female", "Male" });
        S.CloseFriendAgeMin = IntField("Age min", S.CloseFriendAgeMin, 3, 100);
        S.CloseFriendAgeMax = IntField("Age max", S.CloseFriendAgeMax, S.CloseFriendAgeMin, 100);
        Field(T("common.profile"), ref S.CloseFriendProfile);
        GUILayout.Label(T("friend.names"));
        Field("Surname pool (, or ;)", ref S.CloseFriendSurnames);
        Field("Given-name pool (, or ;)", ref S.CloseFriendGivenNames);
        S.CloseFriendNameMode = Choice("Name order", S.CloseFriendNameMode,
            new[] { "Random repeat", "Random", "Sequential" });
        GUILayout.Label(T("friend.to_taiwu"));
        S.CloseFriendRelationType = RelationChoice(S.CloseFriendRelationType);
        S.CloseFriendFavorability = IntField(T("common.favor"), S.CloseFriendFavorability, -30000, 30000);
        GUILayout.Label(T("friend.each_other"));
        S.CloseFriendBatchRelationType = RelationChoice(S.CloseFriendBatchRelationType);
        S.CloseFriendBatchFavorability = IntField(T("common.favor"), S.CloseFriendBatchFavorability, -30000, 30000);
        DrawReincarnation("Close friend", ref S.EnableCloseFriendReincarnation,
            ref S.CloseFriendReincarnationSource, ref S.CloseFriendReincarnationCount, ref S.CloseFriendReincarnationProfile);
    }

    private static void DrawLegacy()
    {
        GUILayout.Label("Config master: " + (FrontendEntry.LegacyMaster ? "ON" : "OFF"));
        S.EnableImmediateLegacyPassing = GUILayout.Toggle(S.EnableImmediateLegacyPassing, T("legacy.immediate"));
        S.ForceXiangshuInfectionBeforePassing = GUILayout.Toggle(S.ForceXiangshuInfectionBeforePassing, "Force full infection");
        if (FrontendEntry.LegacyMaster && S.EnableImmediateLegacyPassing && GUILayout.Button(T("legacy.immediate"), GUILayout.Height(36)))
            RequestLegacyPassingConfirmation();
        S.TransferInheritAvatar = GUILayout.Toggle(S.TransferInheritAvatar, "Inherit avatar");
        S.TransferInheritName = GUILayout.Toggle(S.TransferInheritName, "Inherit name");
        S.TransferMergeMainAttributes = GUILayout.Toggle(S.TransferMergeMainAttributes, "Merge attributes");
        S.TransferMergeQualifications = GUILayout.Toggle(S.TransferMergeQualifications, "Merge qualifications");
        S.TransferInheritMorality = GUILayout.Toggle(S.TransferInheritMorality, "Inherit morality");
        S.EnableTransferPreexistence = GUILayout.Toggle(S.EnableTransferPreexistence, "Record previous Taiwu as preexistence");
        S.EnableRevealPreviousIdentity = GUILayout.Toggle(S.EnableRevealPreviousIdentity, "Reveal previous identity");
        DrawReincarnation("Taiwu at creation", ref S.EnableTaiwuReincarnation,
            ref S.TaiwuReincarnationSource, ref S.TaiwuReincarnationCount, ref S.TaiwuReincarnationProfile);
        if (S.EnableRevealPreviousIdentity)
        {
            Field("NPC ID", ref _npcId);
            if (GUILayout.Button("Reveal"))
            {
                var data = new SerializableModData(); data.Set("NpcId", Parse(_npcId, -1, -1, int.MaxValue));
                Call("RevealPreviousIdentity", data);
            }
        }
    }

    private static void DrawHelp()
    {
        GUILayout.Label(T("help.text"));
        int language = S.Language == "en-US" ? 1 : 0;
        int selected = Choice(T("language"), language, new[] { "中文", "English" });
        if (selected != language)
        {
            S.Language = selected == 1 ? "en-US" : "zh-Hans";
            StudioLocalization.Load(S.Language);
        }
        if (GUILayout.Button("Reload language files")) StudioLocalization.Load(S.Language);
    }

    private static void DrawReincarnation(
        string title, ref bool enabled, ref int source, ref int count, ref string profile)
    {
        GUILayout.Space(8); GUILayout.Label(title + " · " + T("reincarnation"));
        enabled = GUILayout.Toggle(enabled, T("common.enabled"));
        if (!enabled) return;
        source = Choice("Source", source,
            new[] { T("reincarnation.copy"), T("reincarnation.sword"), T("reincarnation.random") });
        count = IntField(T("common.count"), count, 1, 9);
        if (source == 2) Field(T("common.profile"), ref profile);
    }

    private static int RelationChoice(int value)
    {
        int index = Array.IndexOf(Relations, value); if (index < 0) index = 0;
        string[] names = new string[RelationKeys.Length];
        for (int i = 0; i < names.Length; i++) names[i] = T(RelationKeys[i]);
        return Relations[Choice(T("common.relation"), index, names)];
    }

    private static int Choice(string label, int current, string[] names)
    {
        GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(190));
        if (GUILayout.Button(names[Mathf.Clamp(current, 0, names.Length - 1)]))
            current = (current + 1) % names.Length;
        GUILayout.EndHorizontal(); return current;
    }

    private static int IntField(string label, int value, int min, int max)
    {
        string text = value.ToString(); Field(label, ref text); return Parse(text, value, min, max);
    }

    private static void Field(string label, ref string value)
    {
        GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(190));
        value = GUILayout.TextField(value); GUILayout.EndHorizontal();
    }

    private static string MoralityName(int value) =>
        value <= -375 ? "唯我 / Egoistic" : value <= -125 ? "叛逆 / Rebel"
        : value <= 124 ? "中庸 / Even" : value <= 374 ? "仁善 / Kind" : "刚正 / Just";

    private static void SaveSettings()
    {
        string json = JsonUtility.ToJson(S, true);
        var data = new SerializableModData(); data.Set("Json", json);
        Call("SaveStudioSettings", data); FrontendEntry.ApplyUiSettings(); _status = T("status.saved");
    }

    private static void SaveTemplate()
    {
        var data = new SerializableModData();
        data.Set("Id", _editId.Trim()); data.Set("Name", _editName.Trim()); data.Set("Description", "");
        data.Set("Gender", 0); data.Set("AgeMin", 18); data.Set("AgeMax", 30);
        data.Set("AttractionMin", 550); data.Set("AttractionMax", 900); data.Set("BodyType", -1);
        data.Set("BaseHealth", Parse(_health, 100, 0, short.MaxValue));
        data.Set("Morality", Parse(_morality, 0, -500, 500)); data.Set("ClothingTemplateId", 0);
        data.Set("Bisexual", false); data.Set("RemoveNegative", _removeNegative); data.Set("AddAllPositive", _allPositive);
        foreach (string prefix in new[] { "Main", "Life", "Combat" })
        {
            data.Set(prefix + "Mode", _ruleMode); data.Set(prefix + "Value", Parse(_ruleValue, 100, 0, short.MaxValue));
            data.Set(prefix + "Min", Parse(_ruleMin, 80, 0, short.MaxValue));
            data.Set(prefix + "Max", Parse(_ruleMax, 120, 0, short.MaxValue));
        }
        data.Set("RelationType", 0); data.Set("Favorability", 0);
        Call("SaveCharacterProfile", data); _profile = _editId.Trim(); _status = T("status.saved");
    }

    private static void DrawConfirm(int id)
    {
        GUILayout.Label("Enter the original legacy-passing flow?");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("OK")) { _confirm = false; Call("RequestLegacyPassing", new SerializableModData()); }
        if (GUILayout.Button("Cancel")) _confirm = false;
        GUILayout.EndHorizontal(); GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private static void Call(string method, SerializableModData data) =>
        ModDomainMethod.Call.CallModMethodWithParam(FrontendEntry.ModId, method, data);
    private static int Parse(string text, int fallback, int min, int max) =>
        int.TryParse(text, out int value) ? Math.Max(min, Math.Min(max, value)) : fallback;
    private static int ParseOptional(string text, int min, int max) =>
        int.TryParse(text, out int value) ? Math.Max(min, Math.Min(max, value)) : -1;
}
