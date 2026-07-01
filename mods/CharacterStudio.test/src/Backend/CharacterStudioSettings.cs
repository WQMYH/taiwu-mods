using System;

namespace CharacterStudio.Backend;

internal sealed class CharacterStudioSettings
{
    public int SchemaVersion = 1;
    public string Language = "zh-Hans";

    // Config masters.
    public bool EnableMod = true;
    public bool EnableCustomVillagers = true;
    public bool EnableCustomCloseFriends = true;
    public bool EnableLegacyFeatures;
    public string CharacterStudioPanelKey = "F9";
    public string ImmediateLegacyPassingKey = "T";
    public bool EnableDebugLog = true;
    public bool LogValueSnapshots = true;

    // Creation and village processing.
    public bool EnableInfiniteCreationPoints;
    public bool EnableManualCreate = true;
    public bool ProcessInitialVillage = true;
    public bool ProcessRecruitCreated = true;
    public bool ProcessJoinedVillage = true;
    public bool ProcessExistingVillagers = true;
    public bool ExpandVillageCapacity = true;
    public int VillageCapacity = 500;
    public string InitialVillageProfile = "@rules";
    public string ManualCreateProfile = "@rules";
    public string RecruitCreatedProfile = "@rules";
    public string JoinedVillageProfile = "@rules";
    public string ExistingVillagerProfile = "@rules";
    public bool ReloadProfilesOnSettingUpdate = true;
    public int VillagerBatchRelationType;
    public int VillagerBatchFavorability;

    // Close friends.
    public bool SyncCloseFriend = true;
    public int CloseFriendCount = 1;
    public int CloseFriendGender;
    public int CloseFriendAgeMin = 16;
    public int CloseFriendAgeMax = 30;
    public string CloseFriendProfile = "full_positive_villager";
    public string CloseFriendSurnames = "";
    public string CloseFriendGivenNames = "";
    public int CloseFriendNameMode;
    public string CloseFriendExcludedMutexGroups = "172,184";
    public int CloseFriendRelationType = 8192;
    public int CloseFriendFavorability = 30000;
    public int CloseFriendBatchRelationType = 8192;
    public int CloseFriendBatchFavorability = 30000;

    // Reincarnation: 0 copy self, 1 sword ancestors, 2 random template.
    public bool EnableTaiwuReincarnation;
    public int TaiwuReincarnationSource;
    public int TaiwuReincarnationCount = 1;
    public string TaiwuReincarnationProfile = "vanilla_safe";
    public bool EnableCloseFriendReincarnation;
    public int CloseFriendReincarnationSource;
    public int CloseFriendReincarnationCount = 1;
    public string CloseFriendReincarnationProfile = "vanilla_safe";
    public bool EnableVillagerReincarnation;
    public int VillagerReincarnationSource;
    public int VillagerReincarnationCount = 1;
    public string VillagerReincarnationProfile = "vanilla_safe";

    // Legacy passing.
    public bool EnableImmediateLegacyPassing;
    public bool ForceXiangshuInfectionBeforePassing;
    public bool TransferInheritAvatar;
    public bool TransferInheritName;
    public bool TransferMergeQualifications;
    public bool TransferMergeMainAttributes;
    public bool TransferInheritMorality;
    public int TransferFeatureMode;
    public string TransferExcludedFeatureGroups = "172,184";
    public bool EnableTransferPreexistence;
    public int PreexistenceOverflowMode;
    public bool GrantReincarnationFeatureAtCapacity;
    public bool EnableRevealPreviousIdentity;
    public bool RevealTransferFavorability = true;
    public bool RevealTransferRelationTypes = true;
    public bool RevealRemoveOldRelation = true;

    internal void Normalize()
    {
        SchemaVersion = 1;
        Language = Language == "en-US" ? "en-US" : "zh-Hans";
        CloseFriendCount = Math.Clamp(CloseFriendCount, 1, 10);
        CloseFriendGender = Math.Clamp(CloseFriendGender, 0, 2);
        CloseFriendAgeMin = Math.Clamp(CloseFriendAgeMin, 3, 100);
        CloseFriendAgeMax = Math.Clamp(CloseFriendAgeMax, CloseFriendAgeMin, 100);
        CloseFriendNameMode = Math.Clamp(CloseFriendNameMode, 0, 2);
        CloseFriendRelationType = SafeRelation(CloseFriendRelationType);
        CloseFriendBatchRelationType = SafeRelation(CloseFriendBatchRelationType);
        VillagerBatchRelationType = SafeRelation(VillagerBatchRelationType);
        CloseFriendFavorability = Math.Clamp(CloseFriendFavorability, -30000, 30000);
        CloseFriendBatchFavorability = Math.Clamp(CloseFriendBatchFavorability, -30000, 30000);
        VillagerBatchFavorability = Math.Clamp(VillagerBatchFavorability, -30000, 30000);
        VillageCapacity = Math.Clamp(VillageCapacity, 0, 10000);
        TaiwuReincarnationSource = Math.Clamp(TaiwuReincarnationSource, 0, 2);
        CloseFriendReincarnationSource = Math.Clamp(CloseFriendReincarnationSource, 0, 2);
        VillagerReincarnationSource = Math.Clamp(VillagerReincarnationSource, 0, 2);
        TaiwuReincarnationCount = Math.Clamp(TaiwuReincarnationCount, 1, 9);
        CloseFriendReincarnationCount = Math.Clamp(CloseFriendReincarnationCount, 1, 9);
        VillagerReincarnationCount = Math.Clamp(VillagerReincarnationCount, 1, 9);
        TransferFeatureMode = Math.Clamp(TransferFeatureMode, 0, 4);
        PreexistenceOverflowMode = Math.Clamp(PreexistenceOverflowMode, 0, 1);
        CharacterStudioPanelKey = Clean(CharacterStudioPanelKey, "F9");
        ImmediateLegacyPassingKey = Clean(ImmediateLegacyPassingKey, "T");
        InitialVillageProfile = Clean(InitialVillageProfile, "@rules");
        ManualCreateProfile = Clean(ManualCreateProfile, "@rules");
        RecruitCreatedProfile = Clean(RecruitCreatedProfile, "@rules");
        JoinedVillageProfile = Clean(JoinedVillageProfile, "@rules");
        ExistingVillagerProfile = Clean(ExistingVillagerProfile, "@rules");
        CloseFriendProfile = Clean(CloseFriendProfile, "full_positive_villager");
        TaiwuReincarnationProfile = Clean(TaiwuReincarnationProfile, "vanilla_safe");
        CloseFriendReincarnationProfile = Clean(CloseFriendReincarnationProfile, "vanilla_safe");
        VillagerReincarnationProfile = Clean(VillagerReincarnationProfile, "vanilla_safe");
    }

    private static string Clean(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    internal static int SafeRelation(int value) =>
        value is 0 or 512 or 1024 or 8192 or 16384 or 32768 ? value : 0;
}
