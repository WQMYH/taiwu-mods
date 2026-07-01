using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace GongfaFramework.Test.Contracts;

[Serializable]
public sealed class GongfaSnapshot
{
    public string FrameworkVersion = "0.1.0.0";
    public string Side = "";
    public string GameVersion = "1.0.40";
    public string Hash = "";
    public List<GongfaRecord> Records = new List<GongfaRecord>();
    public List<string> Errors = new List<string>();

    public void RecalculateHash()
    {
        string canonical = string.Join("\n", Records.OrderBy(x => x.Id).Select(x => x.Canonical()));
        using SHA256 sha = SHA256.Create();
        Hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", "").ToLowerInvariant();
    }
}

[Serializable]
public sealed class GongfaRecord
{
    public short Id;
    public string Name = "";
    public string Description = "";
    public sbyte SectId;
    public sbyte Type;
    public sbyte Grade;
    public sbyte EquipType;
    public sbyte OrderIdInSect;
    public short BookId;
    public int DirectEffectId;
    public int ReverseEffectId;
    public string BookName = "";
    public string BookDescription = "";
    public string DirectEffectName = "";
    public string ReverseEffectName = "";
    public string CombatSkillJson = "";
    public string SkillBookJson = "";
    public string DirectEffectJson = "";
    public string ReverseEffectJson = "";

    public string Canonical() => string.Join("|",
        Id, Name ?? "", Description ?? "", SectId, Type, Grade, EquipType, OrderIdInSect,
        BookId, DirectEffectId, ReverseEffectId, BookName ?? "", BookDescription ?? "",
        DirectEffectName ?? "", ReverseEffectName ?? "",
        Compact(CombatSkillJson), Compact(SkillBookJson), Compact(DirectEffectJson), Compact(ReverseEffectJson));

    private static string Compact(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        try { return Newtonsoft.Json.Linq.JToken.Parse(json).ToString(Formatting.None); }
        catch { return json.Trim(); }
    }
}

[Serializable]
public sealed class PatchDefinition
{
    public int SchemaVersion = 1;
    public string Operation = "patch";
    public string Table = "CombatSkill";
    public int TemplateId;
    public Dictionary<string, object> Fields = new Dictionary<string, object>();
    public string Source = "user";
}

[Serializable]
public sealed class PatchDifference
{
    public string Table = "";
    public int TemplateId;
    public string Field = "";
    public string Before = "";
    public string After = "";
}

[Serializable]
public sealed class ValidationResult
{
    public bool Success;
    public string Message = "";
    public List<string> Errors = new List<string>();
    public List<PatchDifference> Differences = new List<PatchDifference>();
}

public static class FrameworkJson
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
        TypeNameHandling = TypeNameHandling.None
    };

    public static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);
    public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
}
