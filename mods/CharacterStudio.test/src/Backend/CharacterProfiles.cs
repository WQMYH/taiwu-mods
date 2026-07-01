using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameData.Domains.Mod;
using GameData.Utilities;

namespace CharacterStudio.Backend;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CharacterValueMode
{
    Keep,
    Minimum,
    Override,
    RandomRange
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CharacterGenderMode
{
    Random,
    Female,
    Male
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum OriginalTemplateMode
{
    AreaSectByGender,
    Explicit
}

internal sealed class CharacterValueRule
{
    public CharacterValueMode Mode { get; set; } = CharacterValueMode.Keep;
    public int Value { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }

    internal short Resolve(short current, Random random)
    {
        int resolved = Mode switch
        {
            CharacterValueMode.Minimum => Math.Max(current, Value),
            CharacterValueMode.Override => Value,
            CharacterValueMode.RandomRange => random.Next(
                Math.Min(Min, Max),
                Math.Max(Min, Max) + 1),
            _ => current
        };
        return (short)Math.Clamp(resolved, 0, short.MaxValue);
    }
}

internal sealed class CharacterFeatureRule
{
    public bool RemoveNegative { get; set; }
    public bool AddAllPositive { get; set; }
    public List<short> AddIds { get; set; } = new();
    public List<short> RemoveIds { get; set; } = new();
}

internal sealed class CharacterRelationRule
{
    public int RelationType { get; set; }
    public int Favorability { get; set; }
}

internal sealed class CharacterProfile
{
    public string Id { get; set; } = "vanilla_safe";
    public string Name { get; set; } = "原版安全";
    public string Description { get; set; } = "";
    public int DisplayOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public OriginalTemplateMode OriginalTemplateMode { get; set; } = OriginalTemplateMode.AreaSectByGender;
    public short ExplicitTemplateId { get; set; } = -1;
    public CharacterGenderMode Gender { get; set; } = CharacterGenderMode.Random;
    public int AgeMin { get; set; } = 18;
    public int AgeMax { get; set; } = 18;
    public int AttractionMin { get; set; } = 550;
    public int AttractionMax { get; set; } = 550;
    public int BodyType { get; set; } = -1;
    public CharacterValueRule MainAttributes { get; set; } = new();
    public CharacterValueRule LifeQualifications { get; set; } = new();
    public CharacterValueRule CombatQualifications { get; set; } = new();
    public int BaseHealth { get; set; } = 100;
    public bool ApplyMorality { get; set; }
    public int Morality { get; set; } = -1;
    public int ClothingTemplateId { get; set; }
    public bool Bisexual { get; set; }
    public CharacterFeatureRule Features { get; set; } = new();
    public CharacterRelationRule RelationToTaiwu { get; set; } = new();

    [JsonIgnore]
    internal string Hash { get; set; } = "";
    [JsonIgnore]
    internal bool IsPreset { get; set; }

    internal void Normalize()
    {
        Id = string.IsNullOrWhiteSpace(Id) ? "vanilla_safe" : Id.Trim();
        Name = string.IsNullOrWhiteSpace(Name) ? Id : Name.Trim();
        AgeMin = Math.Clamp(AgeMin, 3, 100);
        AgeMax = Math.Clamp(AgeMax, AgeMin, 100);
        AttractionMin = Math.Clamp(AttractionMin, 0, 999);
        AttractionMax = Math.Clamp(AttractionMax, AttractionMin, 999);
        BodyType = BodyType is >= 0 and <= 2 ? BodyType : -1;
        BaseHealth = Math.Clamp(BaseHealth, 0, short.MaxValue);
        Morality = Math.Clamp(Morality, -500, 500);
        ClothingTemplateId = Math.Clamp(ClothingTemplateId, 0, short.MaxValue);
        RelationToTaiwu.RelationType = Math.Max(0, RelationToTaiwu.RelationType);
        RelationToTaiwu.Favorability = Math.Clamp(RelationToTaiwu.Favorability, -30000, 30000);
    }
}

internal sealed class CharacterProfileDocument
{
    public int SchemaVersion { get; set; } = 2;
    public List<CharacterProfile> Profiles { get; set; } = new();
}

internal sealed class CharacterRuleDocument
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, string> Routes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class CharacterProfileRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static Dictionary<string, CharacterProfile> _profiles =
        new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, CharacterProfile> _presets =
        new(StringComparer.OrdinalIgnoreCase);
    private static Dictionary<string, CharacterProfile> _userProfiles =
        new(StringComparer.OrdinalIgnoreCase);
    private static CharacterRuleDocument _rules = new();

    internal static void Initialize()
    {
        EnsureDefaultFiles();
        Reload();
    }

    internal static void Reload()
    {
        try
        {
            _presets = LoadDocument(CharacterStudioPaths.PresetsFile, preset: true);
            _userProfiles = LoadDocument(CharacterStudioPaths.UserProfilesFile, preset: false);
            var loaded = new Dictionary<string, CharacterProfile>(_presets, StringComparer.OrdinalIgnoreCase);
            foreach ((string id, CharacterProfile profile) in _userProfiles)
                if (!loaded.ContainsKey(id))
                    loaded[id] = profile;
            if (!loaded.ContainsKey("vanilla_safe"))
                loaded["vanilla_safe"] = CreateVanillaSafe();
            _profiles = loaded;

            _rules = JsonSerializer.Deserialize<CharacterRuleDocument>(
                File.ReadAllText(CharacterStudioPaths.RulesFile, Encoding.UTF8), JsonOptions)
                ?? new CharacterRuleDocument();
            AdaptableLog.Info($"[CharacterStudio] 已载入 {_profiles.Count} 个人物模板。");
        }
        catch (Exception ex)
        {
            _profiles = new Dictionary<string, CharacterProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["vanilla_safe"] = CreateVanillaSafe()
            };
            _rules = new CharacterRuleDocument();
            AdaptableLog.Warning("[CharacterStudio] 模板载入失败，已回退 vanilla_safe：" + ex);
        }
    }

    private static Dictionary<string, CharacterProfile> LoadDocument(string path, bool preset)
    {
        CharacterProfileDocument document = JsonSerializer.Deserialize<CharacterProfileDocument>(
            File.ReadAllText(path, Encoding.UTF8), JsonOptions)
            ?? throw new InvalidDataException($"{Path.GetFileName(path)} 内容为空");
        if (document.SchemaVersion is < 1 or > 2)
            throw new InvalidDataException($"不支持的模板 schemaVersion={document.SchemaVersion}");

        var result = new Dictionary<string, CharacterProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (CharacterProfile profile in document.Profiles)
        {
            profile.Normalize();
            profile.IsPreset = preset;
            profile.Hash = ComputeHash(profile);
            result[profile.Id] = profile;
        }
        return result;
    }

    internal static CharacterProfile Resolve(CreationSource source, string? requestedId = null)
    {
        string id = string.IsNullOrWhiteSpace(requestedId) ||
                    requestedId.Equals("@rules", StringComparison.OrdinalIgnoreCase)
            ? ResolveConfiguredRoute(source)
            : requestedId.Trim();
        if (_profiles.TryGetValue(id, out CharacterProfile? profile))
            return profile;
        AdaptableLog.Warning($"[CharacterStudio] 找不到模板 {id}，回退 vanilla_safe。");
        return _profiles["vanilla_safe"];
    }

    internal static IReadOnlyCollection<string> GetProfileIds() => _profiles.Keys.ToArray();

    internal static SerializableModData SaveUserProfile(SerializableModData data)
    {
        var result = new SerializableModData();
        try
        {
            CharacterProfile profile = FromModData(data);
            if (_presets.ContainsKey(profile.Id))
                return OperationResult(false, "预设模板不可覆盖，请使用新的模板 ID。");
            profile.Normalize();
            profile.UpdatedAtUtc = DateTime.UtcNow;
            profile.Hash = ComputeHash(profile);
            _userProfiles[profile.Id] = profile;
            SaveUserProfiles();
            Reload();
            return OperationResult(true, $"已保存“{profile.Name}”（{profile.Id}）。");
        }
        catch (Exception ex)
        {
            return OperationResult(false, ex.Message);
        }
    }

    internal static SerializableModData DeleteUserProfile(SerializableModData data)
    {
        string id = "";
        data.Get("Id", out id);
        id = id?.Trim() ?? "";
        if (id.Length == 0)
            return OperationResult(false, "请填写模板 ID。");
        if (_presets.ContainsKey(id))
            return OperationResult(false, "预设模板不能删除。");
        if (!_userProfiles.Remove(id))
            return OperationResult(false, "没有找到该用户模板。");
        SaveUserProfiles();
        Reload();
        return OperationResult(true, $"已删除用户模板 {id}。");
    }

    private static CharacterProfile FromModData(SerializableModData data)
    {
        string id = "", name = "", description = "";
        int gender = 0, ageMin = 18, ageMax = 18, attractionMin = 550, attractionMax = 550;
        int bodyType = -1, mainMode = 0, mainValue = 0, mainMin = 0, mainMax = 0;
        int lifeMode = 0, lifeValue = 0, lifeMin = 0, lifeMax = 0;
        int combatMode = 0, combatValue = 0, combatMin = 0, combatMax = 0;
        int health = 0, morality = -1, clothing = 0, relationType = 0, favorability = 0;
        bool bisexual = false, removeNegative = false, addAllPositive = false;
        data.Get("Id", out id); data.Get("Name", out name); data.Get("Description", out description);
        data.Get("Gender", out gender); data.Get("AgeMin", out ageMin); data.Get("AgeMax", out ageMax);
        data.Get("AttractionMin", out attractionMin); data.Get("AttractionMax", out attractionMax);
        data.Get("BodyType", out bodyType); data.Get("BaseHealth", out health);
        data.Get("Morality", out morality); data.Get("ClothingTemplateId", out clothing);
        data.Get("Bisexual", out bisexual); data.Get("RemoveNegative", out removeNegative);
        data.Get("AddAllPositive", out addAllPositive); data.Get("RelationType", out relationType);
        data.Get("Favorability", out favorability);
        data.Get("MainMode", out mainMode); data.Get("MainValue", out mainValue);
        data.Get("MainMin", out mainMin); data.Get("MainMax", out mainMax);
        data.Get("LifeMode", out lifeMode); data.Get("LifeValue", out lifeValue);
        data.Get("LifeMin", out lifeMin); data.Get("LifeMax", out lifeMax);
        data.Get("CombatMode", out combatMode); data.Get("CombatValue", out combatValue);
        data.Get("CombatMin", out combatMin); data.Get("CombatMax", out combatMax);
        if (string.IsNullOrWhiteSpace(id) || id.Any(ch => !(char.IsLetterOrDigit(ch) || ch is '_' or '-')))
            throw new InvalidDataException("模板 ID 只能包含字母、数字、下划线和短横线。");
        return new CharacterProfile
        {
            Id = id.Trim(), Name = string.IsNullOrWhiteSpace(name) ? id.Trim() : name.Trim(),
            Description = description?.Trim() ?? "",
            Gender = (CharacterGenderMode)Math.Clamp(gender, 0, 2),
            AgeMin = ageMin, AgeMax = ageMax, AttractionMin = attractionMin, AttractionMax = attractionMax,
            BodyType = bodyType, BaseHealth = health, Morality = morality,
            ApplyMorality = true,
            ClothingTemplateId = clothing, Bisexual = bisexual,
            MainAttributes = Rule(mainMode, mainValue, mainMin, mainMax),
            LifeQualifications = Rule(lifeMode, lifeValue, lifeMin, lifeMax),
            CombatQualifications = Rule(combatMode, combatValue, combatMin, combatMax),
            Features = new CharacterFeatureRule { RemoveNegative = removeNegative, AddAllPositive = addAllPositive },
            RelationToTaiwu = new CharacterRelationRule { RelationType = relationType, Favorability = favorability }
        };
    }

    private static CharacterValueRule Rule(int mode, int value, int min, int max) => new()
    {
        Mode = (CharacterValueMode)Math.Clamp(mode, 0, 3), Value = value, Min = min, Max = max
    };

    private static SerializableModData OperationResult(bool success, string message)
    {
        var result = new SerializableModData();
        result.Set("Success", success);
        result.Set("Message", message);
        return result;
    }

    private static void SaveUserProfiles()
    {
        var document = new CharacterProfileDocument
        {
            SchemaVersion = 2,
            Profiles = _userProfiles.Values.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToList()
        };
        string json = JsonSerializer.Serialize(document, JsonOptions);
        string temp = CharacterStudioPaths.UserProfilesFile + ".tmp";
        File.WriteAllText(temp, json, new UTF8Encoding(false));
        File.Move(temp, CharacterStudioPaths.UserProfilesFile, true);
    }

    private static string ResolveConfiguredRoute(CreationSource source)
    {
        CharacterStudioSettings settings = BackendEntry.Settings;
        string configured = source switch
        {
            CreationSource.InitialVillage => settings.InitialVillageProfile,
            CreationSource.ManualCreate => settings.ManualCreateProfile,
            CreationSource.RecruitCreated => settings.RecruitCreatedProfile,
            CreationSource.JoinedVillage => settings.JoinedVillageProfile,
            CreationSource.ExistingVillagerMonthly => settings.ExistingVillagerProfile,
            _ => "vanilla_safe"
        };
        if (!configured.Equals("@rules", StringComparison.OrdinalIgnoreCase))
            return configured;
        return _rules.Routes.TryGetValue(source.ToString(), out string? routed) &&
               !string.IsNullOrWhiteSpace(routed)
            ? routed.Trim()
            : "vanilla_safe";
    }

    private static void EnsureDefaultFiles()
    {
        if (!File.Exists(CharacterStudioPaths.PresetsFile))
        {
            var document = new CharacterProfileDocument
            {
                Profiles = { CreateVanillaSafe(), CreateFullPositive() }
            };
            File.WriteAllText(
                CharacterStudioPaths.PresetsFile,
                JsonSerializer.Serialize(document, JsonOptions),
                new UTF8Encoding(false));
        }
        if (!File.Exists(CharacterStudioPaths.UserProfilesFile))
            File.WriteAllText(
                CharacterStudioPaths.UserProfilesFile,
                JsonSerializer.Serialize(new CharacterProfileDocument(), JsonOptions),
                new UTF8Encoding(false));
        if (!File.Exists(CharacterStudioPaths.RulesFile))
        {
            var rules = new CharacterRuleDocument
            {
                Routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [nameof(CreationSource.InitialVillage)] = "full_positive_villager",
                    [nameof(CreationSource.ManualCreate)] = "full_positive_villager",
                    [nameof(CreationSource.RecruitCreated)] = "full_positive_villager",
                    [nameof(CreationSource.JoinedVillage)] = "full_positive_villager",
                    [nameof(CreationSource.ExistingVillagerMonthly)] = "full_positive_villager"
                }
            };
            File.WriteAllText(
                CharacterStudioPaths.RulesFile,
                JsonSerializer.Serialize(rules, JsonOptions),
                new UTF8Encoding(false));
        }
    }

    private static CharacterProfile CreateVanillaSafe()
    {
        var profile = new CharacterProfile();
        profile.Normalize();
        profile.Hash = ComputeHash(profile);
        return profile;
    }

    private static CharacterProfile CreateFullPositive()
    {
        var profile = new CharacterProfile
        {
            Id = "full_positive_villager",
            Name = "全正面太吾村民",
            AgeMin = 16,
            AgeMax = 30,
            AttractionMin = 550,
            AttractionMax = 900,
            MainAttributes = new CharacterValueRule { Mode = CharacterValueMode.RandomRange, Min = 75, Max = 120 },
            LifeQualifications = new CharacterValueRule { Mode = CharacterValueMode.RandomRange, Min = 75, Max = 120 },
            CombatQualifications = new CharacterValueRule { Mode = CharacterValueMode.RandomRange, Min = 75, Max = 120 },
            BaseHealth = 10000,
            ApplyMorality = true,
            Morality = 250,
            ClothingTemplateId = 84,
            Features = new CharacterFeatureRule { RemoveNegative = true, AddAllPositive = true },
            RelationToTaiwu = new CharacterRelationRule { RelationType = 8192, Favorability = 30000 }
        };
        profile.Normalize();
        profile.Hash = ComputeHash(profile);
        return profile;
    }

    private static string ComputeHash(CharacterProfile profile)
    {
        string json = JsonSerializer.Serialize(profile, JsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
