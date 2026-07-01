using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Organization;
using GameData.Utilities;

namespace CharacterStudio.Backend;

internal sealed class AppliedProfileRecord
{
    public string ProfileId { get; set; } = "";
    public string ProfileHash { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTime AppliedAtUtc { get; set; }
}

internal sealed class AppliedProfileDocument
{
    public int SchemaVersion { get; set; } = 1;
    public Dictionary<string, AppliedProfileRecord> Characters { get; set; } = new();
}

internal static class VillagerProcessingState
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static AppliedProfileDocument _document = new();
    private static HashSet<int> _knownVillagers = new();

    internal static void Initialize()
    {
        try
        {
            if (File.Exists(CharacterStudioPaths.StateFile))
                _document = JsonSerializer.Deserialize<AppliedProfileDocument>(
                    File.ReadAllText(CharacterStudioPaths.StateFile, Encoding.UTF8), JsonOptions)
                    ?? new AppliedProfileDocument();
        }
        catch (Exception ex)
        {
            _document = new AppliedProfileDocument();
            AdaptableLog.Warning("[CharacterStudio] 应用状态读取失败，将重建：" + ex.Message);
        }
    }

    internal static void CaptureKnownVillagers()
    {
        short settlementId = DomainManager.Taiwu.GetTaiwuVillageSettlementId();
        if (settlementId < 0 ||
            DomainManager.Organization.GetSettlementOrDefault(settlementId) == null)
        {
            Debug("村落数据尚未就绪，保留现有村民快照。");
            return;
        }

        _knownVillagers = GetCurrentVillagerIds().ToHashSet();
        Debug($"已记录当前村民 {_knownVillagers.Count} 人。");
    }

    internal static bool ApplyCharacter(
        int charId,
        DataContext context,
        CreationSource source,
        string? requestedProfileId = null,
        bool persist = true)
    {
        if (charId < 0 || charId == DomainManager.Taiwu.GetTaiwuCharId())
            return false;
        if (!DomainManager.Character.TryGetElement_Objects(charId, out Character? character) || character == null)
            return false;

        CharacterProfile profile = CharacterProfileRepository.Resolve(source, requestedProfileId);
        string stateKey = BuildStateKey(charId);
        if (_document.Characters.TryGetValue(stateKey, out AppliedProfileRecord? existing) &&
            existing.ProfileId.Equals(profile.Id, StringComparison.OrdinalIgnoreCase) &&
            existing.ProfileHash == profile.Hash)
            return true;

        if (!VillagerProfileService.Apply(character, context, profile, source))
            return false;

        _document.Characters[stateKey] = new AppliedProfileRecord
        {
            ProfileId = profile.Id,
            ProfileHash = profile.Hash,
            Source = source.ToString(),
            AppliedAtUtc = DateTime.UtcNow
        };
        _knownVillagers.Add(charId);
        if (persist)
            Save();
        return true;
    }

    internal static void ProcessMonth()
    {
        if (!BackendEntry.Settings.EnableCustomVillagers)
            return;
        DataContext? context = DomainManager.TaiwuEvent.MainThreadDataContext;
        if (context == null)
            return;

        List<int> current = GetCurrentVillagerIds();
        var currentSet = current.ToHashSet();
        foreach (int charId in current)
        {
            bool joined = !_knownVillagers.Contains(charId);
            if (joined && BackendEntry.Settings.ProcessJoinedVillage)
                ApplyCharacter(charId, context, CreationSource.JoinedVillage, persist: false);
            else if (BackendEntry.Settings.ProcessExistingVillagers)
                ApplyCharacter(charId, context, CreationSource.ExistingVillagerMonthly, persist: false);
        }
        _knownVillagers = currentSet;
        Save();
    }

    internal static List<int> GetCurrentVillagerIds()
    {
        var result = new List<int>();
        try
        {
            short settlementId = DomainManager.Taiwu.GetTaiwuVillageSettlementId();
            if (settlementId < 0)
                return result;
            Settlement? settlement = DomainManager.Organization.GetSettlementOrDefault(settlementId);
            if (settlement == null)
                return result;
            settlement.GetMembers().GetAllMembers(result);
            result.Remove(DomainManager.Taiwu.GetTaiwuCharId());
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning("[CharacterStudio] 村民名单暂不可用：" + ex);
        }
        return result;
    }

    private static void Save()
    {
        try
        {
            File.WriteAllText(
                CharacterStudioPaths.StateFile,
                JsonSerializer.Serialize(_document, JsonOptions),
                new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            AdaptableLog.Warning("[CharacterStudio] 应用状态保存失败：" + ex.Message);
        }
    }

    private static string BuildStateKey(int charId)
    {
        int taiwuId;
        short settlementId;
        try
        {
            taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
            settlementId = DomainManager.Taiwu.GetTaiwuVillageSettlementId();
        }
        catch
        {
            taiwuId = -1;
            settlementId = -1;
        }
        return $"{taiwuId}:{settlementId}:{charId}";
    }

    private static void Debug(string message)
    {
        if (BackendEntry.Settings.EnableDebugLog)
            AdaptableLog.Info("[CharacterStudio] " + message);
    }
}
