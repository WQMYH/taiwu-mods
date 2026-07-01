using System;
using System.IO;
using System.Text;
using System.Text.Json;
using GameData.Domains.Mod;

namespace CharacterStudio.Backend;

internal static class StudioSettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, IncludeFields = true };

    internal static CharacterStudioSettings Load()
    {
        try
        {
            if (!File.Exists(CharacterStudioPaths.SettingsFile))
                return new CharacterStudioSettings();
            return JsonSerializer.Deserialize<CharacterStudioSettings>(
                File.ReadAllText(CharacterStudioPaths.SettingsFile, Encoding.UTF8), Options)
                ?? new CharacterStudioSettings();
        }
        catch (Exception)
        {
            string backup = CharacterStudioPaths.SettingsFile + ".broken." + DateTime.Now.ToString("yyyyMMddHHmmss");
            try { File.Copy(CharacterStudioPaths.SettingsFile, backup, true); } catch { }
            return new CharacterStudioSettings();
        }
    }

    internal static SerializableModData Save(SerializableModData data)
    {
        try
        {
            string json = "";
            data.Get("Json", out json);
            CharacterStudioSettings value = JsonSerializer.Deserialize<CharacterStudioSettings>(json, Options)
                ?? throw new InvalidDataException("empty settings");
            CharacterStudioSettings current = BackendEntry.Settings;
            value.EnableCustomVillagers = current.EnableCustomVillagers;
            value.EnableCustomCloseFriends = current.EnableCustomCloseFriends;
            value.EnableLegacyFeatures = current.EnableLegacyFeatures;
            value.CharacterStudioPanelKey = current.CharacterStudioPanelKey;
            value.ImmediateLegacyPassingKey = current.ImmediateLegacyPassingKey;
            value.EnableDebugLog = current.EnableDebugLog;
            value.LogValueSnapshots = current.LogValueSnapshots;
            value.Normalize();
            string temp = CharacterStudioPaths.SettingsFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(value, Options), new UTF8Encoding(false));
            File.Move(temp, CharacterStudioPaths.SettingsFile, true);
            BackendEntry.ApplyUiSettings(value);
            return Result(true, "settings saved");
        }
        catch (Exception ex) { return Result(false, ex.Message); }
    }

    private static SerializableModData Result(bool success, string message)
    {
        var result = new SerializableModData();
        result.Set("Success", success); result.Set("Message", message);
        return result;
    }
}
