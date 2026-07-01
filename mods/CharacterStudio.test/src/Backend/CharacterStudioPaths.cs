using System;
using System.IO;

namespace CharacterStudio.Backend;

internal static class CharacterStudioPaths
{
    internal static string ModRoot { get; private set; } = "";
    internal static string ProfilesDirectory => Path.Combine(ModRoot, "Profiles");
    internal static string UserDataDirectory => Path.Combine(ModRoot, "UserData");
    internal static string PresetsFile => Path.Combine(ProfilesDirectory, "preset_profiles.json");
    internal static string UserProfilesFile => Path.Combine(UserDataDirectory, "character_profiles.json");
    internal static string RulesFile => Path.Combine(ProfilesDirectory, "character_rules.json");
    internal static string StateFile => Path.Combine(UserDataDirectory, "applied_state.json");
    internal static string SettingsFile => Path.Combine(UserDataDirectory, "studio_settings.json");
    internal static string LanguagesDirectory => Path.Combine(ModRoot, "Languages");
    internal static bool IsInitialized => ModRoot.Length > 0;

    internal static void Initialize(string assemblyLocation)
    {
        string directory = Path.GetDirectoryName(assemblyLocation) ?? AppContext.BaseDirectory;
        DirectoryInfo info = new(directory);
        ModRoot = info.Name.Equals("Plugins", StringComparison.OrdinalIgnoreCase) && info.Parent != null
            ? info.Parent.FullName
            : info.FullName;
        Directory.CreateDirectory(ProfilesDirectory);
        Directory.CreateDirectory(UserDataDirectory);
        Directory.CreateDirectory(LanguagesDirectory);
    }
}
