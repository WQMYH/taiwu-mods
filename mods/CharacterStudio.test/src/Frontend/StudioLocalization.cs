using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace CharacterStudio.Frontend;

internal static class StudioLocalization
{
    private static readonly Dictionary<string, string> Zh = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> Current = new(StringComparer.OrdinalIgnoreCase);
    internal static string Language { get; private set; } = "zh-Hans";

    internal static void Load(string language)
    {
        Language = language == "en-US" ? "en-US" : "zh-Hans";
        Zh.Clear(); Current.Clear();
        string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        if (Path.GetFileName(root).Equals("Plugins", StringComparison.OrdinalIgnoreCase))
            root = Directory.GetParent(root)?.FullName ?? root;
        Read(Path.Combine(root, "Languages", "zh-Hans.lng"), Zh);
        Read(Path.Combine(root, "Languages", Language + ".lng"), Current);
    }

    internal static string T(string key, params object[] args)
    {
        string value = Current.TryGetValue(key, out string? current) ? current
            : Zh.TryGetValue(key, out string? fallback) ? fallback : key;
        try { return args.Length == 0 ? value : string.Format(value, args); }
        catch { return value; }
    }

    private static void Read(string path, Dictionary<string, string> target)
    {
        if (!File.Exists(path)) return;
        foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            int split = line.IndexOf('=');
            if (split <= 0) continue;
            target[line[..split].Trim()] = line[(split + 1)..].Replace("\\n", "\n").Replace("\\=", "=");
        }
    }
}

[Serializable]
internal sealed class FrontendStudioSettings
{
    public int SchemaVersion = 1;
    public string Language = "zh-Hans";
    public bool EnableInfiniteCreationPoints;
    public bool EnableManualCreate = true, ProcessInitialVillage = true, ProcessRecruitCreated = true;
    public bool ProcessJoinedVillage = true, ProcessExistingVillagers = true, ExpandVillageCapacity = true;
    public int VillageCapacity = 500;
    public string InitialVillageProfile = "@rules", ManualCreateProfile = "@rules";
    public string RecruitCreatedProfile = "@rules", JoinedVillageProfile = "@rules", ExistingVillagerProfile = "@rules";
    public int VillagerBatchRelationType, VillagerBatchFavorability;
    public bool SyncCloseFriend = true;
    public int CloseFriendCount = 1, CloseFriendGender, CloseFriendAgeMin = 16, CloseFriendAgeMax = 30;
    public string CloseFriendProfile = "full_positive_villager", CloseFriendSurnames = "", CloseFriendGivenNames = "";
    public int CloseFriendNameMode, CloseFriendRelationType = 8192, CloseFriendFavorability = 30000;
    public int CloseFriendBatchRelationType = 8192, CloseFriendBatchFavorability = 30000;
    public string CloseFriendExcludedMutexGroups = "172,184";
    public bool EnableTaiwuReincarnation, EnableCloseFriendReincarnation, EnableVillagerReincarnation;
    public int TaiwuReincarnationSource, CloseFriendReincarnationSource, VillagerReincarnationSource;
    public int TaiwuReincarnationCount = 1, CloseFriendReincarnationCount = 1, VillagerReincarnationCount = 1;
    public string TaiwuReincarnationProfile = "vanilla_safe", CloseFriendReincarnationProfile = "vanilla_safe";
    public string VillagerReincarnationProfile = "vanilla_safe";
    public bool EnableImmediateLegacyPassing, ForceXiangshuInfectionBeforePassing;
    public bool TransferInheritAvatar, TransferInheritName, TransferMergeQualifications;
    public bool TransferMergeMainAttributes, TransferInheritMorality;
    public int TransferFeatureMode;
    public string TransferExcludedFeatureGroups = "172,184";
    public bool EnableTransferPreexistence, GrantReincarnationFeatureAtCapacity;
    public int PreexistenceOverflowMode;
    public bool EnableRevealPreviousIdentity, RevealTransferFavorability = true;
    public bool RevealTransferRelationTypes = true, RevealRemoveOldRelation = true;
}
