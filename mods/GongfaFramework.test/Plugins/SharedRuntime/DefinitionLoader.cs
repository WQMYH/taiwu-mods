using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Config;
using GongfaFramework.Test.Contracts;
using Newtonsoft.Json.Linq;

namespace GongfaFramework.Test.Runtime;

internal static class DefinitionLoader
{
    private sealed class Prepared
    {
        internal string Table;
        internal int Id;
        internal object Original;
        internal object Modified;
    }

    internal static ValidationResult LoadAndApply(string definitionsDirectory)
    {
        var result = new ValidationResult();
        if (!Directory.Exists(definitionsDirectory))
        {
            result.Success = true;
            result.Message = "Definitions 目录不存在，未加载补丁。";
            return result;
        }

        try
        {
            List<PatchDefinition> definitions = ReadDefinitions(definitionsDirectory);
            List<Prepared> prepared = Prepare(definitions, result);
            if (result.Errors.Count > 0)
            {
                result.Message = $"发现 {result.Errors.Count} 个定义错误，未应用任何补丁。";
                return result;
            }

            try
            {
                foreach (Prepared item in prepared) Commit(item);
            }
            catch
            {
                foreach (Prepared item in prepared) Restore(item);
                throw;
            }
            result.Success = true;
            result.Message = $"成功应用 {prepared.Count} 个字段补丁。";
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.ToString());
            result.Message = "定义加载失败，已保持或恢复原始配置。";
        }
        return result;
    }

    internal static ValidationResult ValidateText(string text, string extension)
    {
        var result = new ValidationResult();
        try
        {
            List<PatchDefinition> values = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? ParseCsv(text) : ParseJson(text);
            Prepare(values, result);
            result.Success = result.Errors.Count == 0;
            result.Message = result.Success ? "定义有效。" : "定义包含错误。";
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
            result.Message = "解析失败。";
        }
        return result;
    }

    private static List<PatchDefinition> ReadDefinitions(string root)
    {
        var result = new List<PatchDefinition>();
        foreach (string path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (path.IndexOf($"{Path.DirectorySeparatorChar}examples{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase) >= 0) continue;
            string extension = Path.GetExtension(path);
            if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                result.AddRange(ParseJson(File.ReadAllText(path)));
            else if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                result.AddRange(ParseCsv(File.ReadAllText(path)));
        }
        return result;
    }

    private static List<PatchDefinition> ParseJson(string text)
    {
        JToken root = JToken.Parse(text);
        if (root.Type == JTokenType.Array)
            return root.ToObject<List<PatchDefinition>>() ?? new List<PatchDefinition>();
        JObject obj = (JObject)root;
        JToken records = obj.GetValue("records", StringComparison.OrdinalIgnoreCase);
        if (records == null)
            return new List<PatchDefinition> { obj.ToObject<PatchDefinition>() };

        var result = new List<PatchDefinition>();
        foreach (JObject record in records.Children<JObject>())
        {
            AddSnapshotObject(result, "CombatSkill", record["CombatSkillJson"]);
            AddSnapshotObject(result, "SkillBook", record["SkillBookJson"]);
            AddSnapshotObject(result, "SpecialEffect", record["DirectEffectJson"]);
            AddSnapshotObject(result, "SpecialEffect", record["ReverseEffectJson"]);
        }
        return result.GroupBy(x => $"{x.Table}:{x.TemplateId}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First()).ToList();
    }

    private static void AddSnapshotObject(List<PatchDefinition> result, string table, JToken raw)
    {
        string json = raw?.Type == JTokenType.String ? raw.Value<string>() : raw?.ToString();
        if (string.IsNullOrWhiteSpace(json)) return;
        JObject item = JObject.Parse(json);
        JToken idToken = item.GetValue("TemplateId", StringComparison.OrdinalIgnoreCase);
        if (idToken == null) throw new FormatException($"{table} 快照缺少 TemplateId。");
        var fields = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (JProperty property in item.Properties())
            if (!property.Name.Equals("TemplateId", StringComparison.OrdinalIgnoreCase))
                fields[property.Name] = property.Value;
        result.Add(new PatchDefinition
        {
            Table = table, TemplateId = idToken.Value<int>(), Fields = fields, Source = "snapshot"
        });
    }

    private static List<Prepared> Prepare(IEnumerable<PatchDefinition> definitions, ValidationResult result)
    {
        var preparedByTarget = new Dictionary<string, Prepared>(StringComparer.OrdinalIgnoreCase);
        foreach (PatchDefinition definition in definitions)
        {
            if (definition == null) { result.Errors.Add("发现空定义。"); continue; }
            if (definition.SchemaVersion != 1) { result.Errors.Add("仅支持 schemaVersion=1。"); continue; }
            if (!string.Equals(definition.Operation, "patch", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"0.1 仅支持 patch，收到 {definition.Operation}。");
                continue;
            }
            string key = $"{definition.Table}:{definition.TemplateId}";
            if (!preparedByTarget.TryGetValue(key, out Prepared prepared))
            {
                object original = GetItem(definition.Table, definition.TemplateId);
                if (original == null)
                {
                    result.Errors.Add($"{key} 不存在；0.1 不允许新增条目。");
                    continue;
                }
                prepared = new Prepared
                {
                    Table = definition.Table,
                    Id = definition.TemplateId,
                    Original = original,
                    Modified = Duplicate(original, definition.TemplateId)
                };
                preparedByTarget.Add(key, prepared);
            }
            foreach (KeyValuePair<string, object> field in definition.Fields)
                ApplyField(prepared, field.Key, field.Value, result);
        }
        return preparedByTarget.Values.ToList();
    }

    private static void ApplyField(Prepared prepared, string path, object raw, ValidationResult result)
    {
        string fieldName = path;
        int index = -1;
        int bracket = path.IndexOf('[');
        if (bracket >= 0 && path.EndsWith("]", StringComparison.Ordinal))
        {
            fieldName = path.Substring(0, bracket);
            int.TryParse(path.Substring(bracket + 1, path.Length - bracket - 2), out index);
        }
        FieldInfo field = prepared.Modified.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        if (field == null)
        {
            result.Errors.Add($"{prepared.Table}:{prepared.Id} 没有字段 {fieldName}。");
            return;
        }
        try
        {
            object before = field.GetValue(prepared.Modified);
            object converted = ConvertValue(raw, field.FieldType);
            if (index >= 0)
            {
                Array source = before as Array ?? throw new InvalidOperationException($"{fieldName} 不是数组。");
                if (index >= source.Length) throw new IndexOutOfRangeException($"{fieldName}[{index}] 越界。");
                Array clone = (Array)source.Clone();
                object element = ConvertValue(raw, field.FieldType.GetElementType());
                clone.SetValue(element, index);
                converted = clone;
            }
            field.SetValue(prepared.Modified, converted);
            result.Differences.Add(new PatchDifference
            {
                Table = prepared.Table, TemplateId = prepared.Id, Field = path,
                Before = ValueText(before), After = ValueText(converted)
            });
        }
        catch (Exception ex)
        {
            result.Errors.Add($"{prepared.Table}:{prepared.Id}.{path}：{ex.Message}");
        }
    }

    private static object ConvertValue(object raw, Type target)
    {
        if (raw == null) return null;
        JToken token = raw as JToken ?? JToken.FromObject(raw);
        if (target.IsEnum) return Enum.Parse(target, token.ToString(), true);
        return token.ToObject(target);
    }

    private static string ValueText(object value) =>
        value == null ? "null" : JToken.FromObject(value).ToString(Newtonsoft.Json.Formatting.None);

    private static object GetItem(string table, int id) => table?.ToLowerInvariant() switch
    {
        "combatskill" => CombatSkill.Instance.GetItem((short)id),
        "skillbook" => SkillBook.Instance.GetItem((short)id),
        "specialeffect" => SpecialEffect.Instance.GetItem((short)id),
        _ => null
    };

    private static object Duplicate(object item, int id) => item switch
    {
        CombatSkillItem value => value.Duplicate(id),
        SkillBookItem value => value.Duplicate(id),
        SpecialEffectItem value => value.Duplicate(id),
        _ => throw new InvalidOperationException("不支持的配置类型。")
    };

    private static void Commit(Prepared item)
    {
        switch (item.Modified)
        {
            case CombatSkillItem value: CombatSkill.Instance.AddOrModifyItem(value); break;
            case SkillBookItem value: SkillBook.Instance.AddOrModifyItem(value); break;
            case SpecialEffectItem value: SpecialEffect.Instance.AddOrModifyItem(value); break;
        }
    }

    private static void Restore(Prepared item)
    {
        switch (item.Original)
        {
            case CombatSkillItem value: CombatSkill.Instance.AddOrModifyItem(value); break;
            case SkillBookItem value: SkillBook.Instance.AddOrModifyItem(value); break;
            case SpecialEffectItem value: SpecialEffect.Instance.AddOrModifyItem(value); break;
        }
    }

    private static List<PatchDefinition> ParseCsv(string text)
    {
        var result = new List<PatchDefinition>();
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            List<string> cells = SplitCsvLine(lines[i]);
            if (cells.Count < 5) throw new FormatException($"CSV 第 {i + 1} 行少于 5 列。");
            object value = ParseScalar(cells[3], cells[4]);
            result.Add(new PatchDefinition
            {
                Table = cells[0], TemplateId = int.Parse(cells[1], CultureInfo.InvariantCulture),
                Fields = new Dictionary<string, object> { [cells[2]] = value }, Source = "csv"
            });
        }
        return result;
    }

    private static object ParseScalar(string type, string value) => type.Trim().ToLowerInvariant() switch
    {
        "bool" => bool.Parse(value), "byte" => byte.Parse(value, CultureInfo.InvariantCulture),
        "sbyte" => sbyte.Parse(value, CultureInfo.InvariantCulture),
        "short" => short.Parse(value, CultureInfo.InvariantCulture),
        "int" => int.Parse(value, CultureInfo.InvariantCulture),
        "long" => long.Parse(value, CultureInfo.InvariantCulture),
        "float" => float.Parse(value, CultureInfo.InvariantCulture),
        "double" => double.Parse(value, CultureInfo.InvariantCulture),
        _ => value
    };

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var value = new System.Text.StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { value.Append('"'); i++; }
            else if (c == '"') quoted = !quoted;
            else if (c == ',' && !quoted) { result.Add(value.ToString()); value.Clear(); }
            else value.Append(c);
        }
        result.Add(value.ToString());
        return result;
    }
}
