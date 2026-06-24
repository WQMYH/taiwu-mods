using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Building;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.Map;
using GameData.Domains.Taiwu;
using HarmonyLib;
using LifeSkillItem = GameData.Domains.Character.LifeSkillItem;

namespace CopyBuildingModernized.Backend
{
    public static class BuildingDataCollector
    {
        public struct VillageBuildingData
        {
            public Dictionary<BuildingBlockKey, BuildingBlockData> Blocks;
            public Dictionary<BuildingBlockKey, ArtisanOrder> ArtisanOrders;
            public Dictionary<BuildingBlockKey, BuildingResourceOutputSetting> ResourceOutput;
            public Dictionary<BuildingBlockKey, sbyte> CollectBuildingResourceType;
            public List<short> AutoWorkBlocks;
            public List<short> AutoSoldBlocks;
            public List<short> AutoCheckInResidence;
            public List<short> AutoCheckInComfortable;
            public sbyte Width;

            public void ConvertAllIndices(sbyte oldWidth, sbyte newWidth)
            {
                Location loc = DomainManager.Taiwu.GetTaiwuVillageLocation();

                Blocks = Remap(Blocks, oldWidth, newWidth, loc,
                    (k, v, nk) =>
                    {
                        v.BlockIndex = nk.BuildingBlockIndex;
                        if (v.RootBlockIndex >= 0)
                        {
                            short newRootIndex = v.RootBlockIndex.ConvertGridIndex(oldWidth, newWidth);
                            v.RootBlockIndex = newRootIndex;
                        }
                        return v;
                    });
                ArtisanOrders = Remap(ArtisanOrders, oldWidth, newWidth, loc,
                    (k, v, nk) => { v.BuildingBlockKey = nk; return v; });
                ResourceOutput = Remap(ResourceOutput, oldWidth, newWidth, loc,
                    (k, v, nk) => v);
                CollectBuildingResourceType = RemapSbyte(CollectBuildingResourceType, oldWidth, newWidth, loc);

                AutoWorkBlocks = ConvertList(AutoWorkBlocks, oldWidth, newWidth);
                AutoSoldBlocks = ConvertList(AutoSoldBlocks, oldWidth, newWidth);
                AutoCheckInResidence = ConvertList(AutoCheckInResidence, oldWidth, newWidth);
                AutoCheckInComfortable = ConvertList(AutoCheckInComfortable, oldWidth, newWidth);
                Width = newWidth;
            }

            private static Dictionary<BuildingBlockKey, T> Remap<T>(
                Dictionary<BuildingBlockKey, T> dict, sbyte oldW, sbyte newW, Location loc,
                Func<BuildingBlockKey, T, BuildingBlockKey, T> transform)
            {
                var result = new Dictionary<BuildingBlockKey, T>();
                foreach (var kvp in dict)
                {
                    short ni = kvp.Key.BuildingBlockIndex.ConvertGridIndex(oldW, newW);
                    if (ni == -1) continue;
                    var nk = new BuildingBlockKey(loc.AreaId, loc.BlockId, ni);
                    result[nk] = transform(kvp.Key, kvp.Value, nk);
                }
                return result;
            }

            private static Dictionary<BuildingBlockKey, sbyte> RemapSbyte(
                Dictionary<BuildingBlockKey, sbyte> dict, sbyte oldW, sbyte newW, Location loc)
            {
                var result = new Dictionary<BuildingBlockKey, sbyte>();
                foreach (var kvp in dict)
                {
                    short ni = kvp.Key.BuildingBlockIndex.ConvertGridIndex(oldW, newW);
                    if (ni == -1) continue;
                    var nk = new BuildingBlockKey(loc.AreaId, loc.BlockId, ni);
                    result[nk] = kvp.Value;
                }
                return result;
            }

            private static List<short> ConvertList(List<short> list, short oldW, short newW)
            {
                var result = new List<short>();
                foreach (var idx in list)
                {
                    short ni = idx.ConvertGridIndex(oldW, newW);
                    if (ni != -1) result.Add(ni);
                }
                return result;
            }

            public void LogSummary(string tag, Action<string> logInfo)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{tag}] Width={Width}, Blocks={Blocks?.Count ?? 0}, " +
                    $"Orders={ArtisanOrders?.Count ?? 0}, Output={ResourceOutput?.Count ?? 0}, " +
                    $"CollectType={CollectBuildingResourceType?.Count ?? 0}, " +
                    $"AutoWork={AutoWorkBlocks?.Count}, AutoSold={AutoSoldBlocks?.Count}, " +
                    $"Residence={AutoCheckInResidence?.Count}, Comfortable={AutoCheckInComfortable?.Count}");
                logInfo(sb.ToString());
            }
        }

        public static VillageBuildingData Collect(TaiwuDomain domain,
            Action<string> logInfo, Action<string> logWarn, Action<string> logError)
        {
            var data = new VillageBuildingData
            {
                Blocks = new Dictionary<BuildingBlockKey, BuildingBlockData>(),
                ArtisanOrders = new Dictionary<BuildingBlockKey, ArtisanOrder>(),
                ResourceOutput = new Dictionary<BuildingBlockKey, BuildingResourceOutputSetting>(),
                CollectBuildingResourceType = new Dictionary<BuildingBlockKey, sbyte>(),
                AutoWorkBlocks = new List<short>(),
                AutoSoldBlocks = new List<short>(),
                AutoCheckInResidence = new List<short>(),
                AutoCheckInComfortable = new List<short>()
            };

            try
            {
                var context = DataContextManager.GetCurrentThreadDataContext();
                Location loc = DomainManager.Taiwu.GetTaiwuVillageLocation();
                BuildingAreaData areaData = DomainManager.Building.GetElement_BuildingAreas(loc);
                int width = areaData.Width;
                int total = width * width;
                data.Width = (sbyte)width;
                logInfo($"Collect start: width={width}, total={total}");

                for (short index = 0; index < total; ++index)
                {
                    var key = new BuildingBlockKey(loc.AreaId, loc.BlockId, index);
                    try
                    {
                        if (DomainManager.Building.TryGetElement_BuildingBlocks(key, out var blockData) && blockData != null)
                            data.Blocks[key] = blockData;

                        if (DomainManager.Extra.TryGetElement_BuildingArtisanOrders((ulong)key, out var order) && order != null)
                            data.ArtisanOrders[key] = order;

                        if (DomainManager.Extra.TryGetElement_BuildingResourceOutputSettings(key.BuildingBlockIndex, out var resOut) && resOut != null)
                            data.ResourceOutput[key] = resOut;
                    }
                    catch (Exception exBlock)
                    {
                        logError($"Exception collecting block {key}: {exBlock}");
                    }
                }

                // Read internal fields via reflection
                try
                {
                    var fieldCollectType = Traverse.Create(DomainManager.Building)
                        .Field<Dictionary<BuildingBlockKey, sbyte>>("_CollectBuildingResourceType").Value;
                    if (fieldCollectType != null)
                        data.CollectBuildingResourceType = new Dictionary<BuildingBlockKey, sbyte>(fieldCollectType);
                }
                catch (Exception ex) { logError($"Failed to read _CollectBuildingResourceType: {ex}"); }

                try
                {
                    var extraTraverse = Traverse.Create(DomainManager.Extra);
                    data.AutoWorkBlocks = extraTraverse.Field<List<short>>("_autoWorkBlockIndexList").Value ?? new List<short>();
                    data.AutoSoldBlocks = extraTraverse.Field<List<short>>("_autoSoldBlockIndexList").Value ?? new List<short>();
                    data.AutoCheckInResidence = extraTraverse.Field<List<short>>("_autoCheckInResidenceList").Value ?? new List<short>();
                    data.AutoCheckInComfortable = extraTraverse.Field<List<short>>("_autoCheckInComfortableList").Value ?? new List<short>();
                }
                catch (Exception ex) { logError($"Failed to read auto lists: {ex}"); }
            }
            catch (Exception exMain) { logError($"Collect fatal error: {exMain}"); }

            data.LogSummary("Collected", logInfo);
            return data;
        }

        public static void Apply(TaiwuDomain domain, VillageBuildingData data,
            Action<string> logInfo, Action<string> logWarn, Action<string> logError,
            bool trySetLeader, int addSkillGrade, bool cleanOperationStateOnImport)
        {
            sbyte oldWidth = data.Width;
            sbyte newWidth = (sbyte)DomainManager.Building.GetElement_BuildingAreas(
                DomainManager.Taiwu.GetTaiwuVillageLocation()).Width;
            data.LogSummary("Before conversion", logInfo);
            data.ConvertAllIndices(oldWidth, newWidth);
            RepairInvalidSupportingBlocks(ref data, logInfo, logWarn);
            data.LogSummary("After conversion", logInfo);

            if (cleanOperationStateOnImport)
                CleanImportedOperationState(ref data, logInfo);

            var context = DataContextManager.GetCurrentThreadDataContext();
            Location loc = DomainManager.Taiwu.GetTaiwuVillageLocation();
            int total = newWidth * newWidth;

            // Clear all existing buildings (with null protection)
            for (short index = 0; index < total; ++index)
            {
                var key = new BuildingBlockKey(loc.AreaId, loc.BlockId, index);
                if (DomainManager.Building.TryGetElement_BuildingBlocks(key, out var existingBlock) &&
                    existingBlock != null && existingBlock.TemplateId != 0)
                    DomainManager.Building.GmCmd_RemoveBuildingImmediately(context, key);
            }
            Traverse.Create(DomainManager.Extra).Method("ClearBuildingArtisanOrders", context).GetValue();

            if (cleanOperationStateOnImport)
                ClearBuildingOperatorDictForVillage(loc, total, logInfo, logWarn);

            // Write the complete grid. The vanilla effect cache expects every cell to exist.
            // Operation-time queries for empty/supporting blocks are guarded by BackendEntry's Harmony patch.
            int writtenBlockCount = 0;
            int skippedNullBlockCount = 0;
            foreach (var (key, blockData) in data.Blocks)
            {
                if (blockData == null)
                {
                    logWarn($"Skip null imported block: {key}");
                    skippedNullBlockCount++;
                    continue;
                }

                Traverse.Create(DomainManager.Building)
                    .Method("SetElement_BuildingBlocks", key, blockData, context).GetValue();
                writtenBlockCount++;

                // Assign village leader
                BuildingBlockItem blockConfig = TryGetBuildingConfig(blockData.TemplateId, logWarn);
                if (blockConfig != null && blockConfig.NeedLeader && trySetLeader)
                {
                    int bestLeader = GetBestLeader(context, blockData, addSkillGrade, logError);
                    if (bestLeader != -1)
                    {
                        DomainManager.Building.SetShopManager(context, key, 0, bestLeader);
                        if (blockConfig.ArtisanOrderAvailable)
                        {
                            var artisanOrder = DomainManager.Extra.GetBuildingArtisanOrder(key);
                            if (artisanOrder != null && data.ArtisanOrders.TryGetValue(key, out var importedOrder))
                            {
                                DomainManager.Extra.SetArtisanOrderProductionType(context, artisanOrder, importedOrder.ItemSubType);
                                DomainManager.Extra.SetArtisanOrderStorageType(context, artisanOrder, (ItemSourceType)importedOrder.StorageType);
                            }
                        }
                    }
                }
            }
            logInfo($"Import write summary: written={writtenBlockCount}, skippedNull={skippedNullBlockCount}.");

            // Write resource output settings
            foreach (var (key, res) in data.ResourceOutput)
                DomainManager.Extra.SetBuildingResourceOutputSetting(context, key.BuildingBlockIndex, res);

            foreach (var (key, val) in data.CollectBuildingResourceType)
                DomainManager.Building.SetCollectBuildingResourceType(context, key, val);

            // Write auto work/sell/check-in lists
            DomainManager.Extra.SetAutoSoldBlockIndexList(data.AutoSoldBlocks, context);
            DomainManager.Extra.SetAutoWorkBlockIndexList(data.AutoWorkBlocks, context);
            DomainManager.Extra.SetAutoCheckInResidenceList(data.AutoCheckInResidence, context);
            DomainManager.Extra.SetAutoCheckInComfortableList(data.AutoCheckInComfortable, context);

            // Refresh village building effects
            Traverse.Create(DomainManager.Building).Method("UpdateTaiwuVillageBuildingEffect").GetValue();
            
            // PostImport validation: confirm no TemplateId=0 empty blocks in runtime.
            ValidateEmptyBlocksSafety(loc, newWidth, logInfo, logWarn);
            
            // Clean all invalid BuildingOperatorDict entries (including other villages)
            CleanInvalidOperatorDictEntries(logInfo, logWarn);
            
            logInfo("Apply complete.");
        }

        private static void CleanImportedOperationState(ref VillageBuildingData data, Action<string> logInfo)
        {
            int resetProgressCount = 0;
            if (data.Blocks != null)
            {
                foreach (var key in new List<BuildingBlockKey>(data.Blocks.Keys))
                {
                    BuildingBlockData blockData = data.Blocks[key];
                    if (blockData == null)
                        continue;

                    if (blockData.OperationProgress != 0)
                    {
                        blockData.OperationProgress = 0;
                        resetProgressCount++;
                    }

                    // Clean OperationType to prevent frontend from thinking building is still operating
                    if (blockData.OperationType != -1)
                    {
                        blockData.OperationType = -1;
                        resetProgressCount++;
                    }

                    // Clean OperationStopping
                    if (blockData.OperationStopping)
                    {
                        blockData.OperationStopping = false;
                        resetProgressCount++;
                    }

                    data.Blocks[key] = blockData;
                }
            }

            int artisanOrderCount = data.ArtisanOrders?.Count ?? 0;
            int autoWorkCount = data.AutoWorkBlocks?.Count ?? 0;
            int autoSoldCount = data.AutoSoldBlocks?.Count ?? 0;
            int residenceCount = data.AutoCheckInResidence?.Count ?? 0;
            int comfortableCount = data.AutoCheckInComfortable?.Count ?? 0;

            data.ArtisanOrders = new Dictionary<BuildingBlockKey, ArtisanOrder>();
            data.AutoWorkBlocks = new List<short>();
            data.AutoSoldBlocks = new List<short>();
            data.AutoCheckInResidence = new List<short>();
            data.AutoCheckInComfortable = new List<short>();

            logInfo($"CleanOperationStateOnImport enabled: reset OperationProgress={resetProgressCount}, " +
                $"clear ArtisanOrders={artisanOrderCount}, AutoWork={autoWorkCount}, AutoSold={autoSoldCount}, " +
                $"Residence={residenceCount}, Comfortable={comfortableCount}.");
        }

        private static void RepairInvalidSupportingBlocks(ref VillageBuildingData data,
            Action<string> logInfo, Action<string> logWarn)
        {
            if (data.Blocks == null)
                return;

            var validRootIndices = new HashSet<short>();
            foreach (var block in data.Blocks.Values)
            {
                if (block != null && block.TemplateId > 0)
                    validRootIndices.Add(block.BlockIndex);
            }

            int repairedCount = 0;
            foreach (var key in new List<BuildingBlockKey>(data.Blocks.Keys))
            {
                BuildingBlockData blockData = data.Blocks[key];
                if (blockData == null || blockData.TemplateId != -1)
                    continue;

                if (blockData.RootBlockIndex >= 0 && validRootIndices.Contains(blockData.RootBlockIndex))
                    continue;

                logWarn($"Repair invalid supporting block: Index={blockData.BlockIndex}, RootBlockIndex={blockData.RootBlockIndex}");
                blockData.TemplateId = 0;
                blockData.RootBlockIndex = -1;
                blockData.OperationType = -1;
                blockData.OperationProgress = 0;
                blockData.OperationStopping = false;
                data.Blocks[key] = blockData;
                repairedCount++;
            }

            if (repairedCount > 0)
                logInfo($"RepairInvalidSupportingBlocks: converted invalid supporting blocks to empty land, count={repairedCount}.");
        }

        private static void ValidateEmptyBlocksSafety(Location loc, sbyte width,
            Action<string> logInfo, Action<string> logWarn)
        {
            int emptyBlockCount = 0;

            for (short i = 0; i < width * width; i++)
            {
                var key = new BuildingBlockKey(loc.AreaId, loc.BlockId, i);
                var block = DomainManager.Building.GetElement_BuildingBlocks(key);

                if (block != null && block.TemplateId == 0)
                    emptyBlockCount++;
            }

            if (emptyBlockCount > 0)
                logWarn($"[PostImport Check] Found {emptyBlockCount} runtime empty blocks (TemplateId=0). These may still trigger GetBuildingOperationLeftTime.");
            else
                logInfo("[PostImport Check] No runtime empty blocks (TemplateId=0) found.");
        }

        private static void RemoveRuntimeEmptyBlocks(DataContext context, Location loc, sbyte width,
            Action<string> logInfo, Action<string> logWarn)
        {
            int removedCount = 0;
            int failedCount = 0;
            int total = width * width;

            for (short i = 0; i < total; i++)
            {
                var key = new BuildingBlockKey(loc.AreaId, loc.BlockId, i);
                BuildingBlockData block = DomainManager.Building.GetElement_BuildingBlocks(key);
                if (block == null || block.TemplateId != 0)
                    continue;

                try
                {
                    Traverse.Create(DomainManager.Building)
                        .Method("RemoveElement_BuildingBlocks", key, context).GetValue();
                    removedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logWarn($"RemoveRuntimeEmptyBlocks: failed to remove empty block index={i}: {ex.Message}");
                }
            }

            logInfo($"RemoveRuntimeEmptyBlocks: removed={removedCount}, failed={failedCount}.");
        }

        private static void CleanInvalidOperatorDictEntries(Action<string> logInfo, Action<string> logWarn)
        {
            // Clean all BuildingOperatorDict entries pointing to invalid characters
            // This prevents GetBuildingOperationLeftTime from accessing non-existent characters and causing NullReferenceException
            
            object operatorDict = TryGetBuildingOperatorDictStorage(logWarn);
            if (operatorDict == null)
                return;

            try
            {
                // Get all Keys of the dictionary
                var keysProperty = operatorDict.GetType().GetProperty("Keys");
                if (keysProperty == null)
                {
                    logWarn("CleanInvalidOperatorDictEntries: cannot access Keys property.");
                    return;
                }

                var keys = keysProperty.GetValue(operatorDict) as System.Collections.ICollection;
                if (keys == null)
                {
                    logWarn("CleanInvalidOperatorDictEntries: keys is null.");
                    return;
                }

                // Get Values property to access Dictionary<int, byte> in operator dict
                var valuesProperty = operatorDict.GetType().GetProperty("Values");
                if (valuesProperty == null)
                {
                    logWarn("CleanInvalidOperatorDictEntries: cannot access Values property.");
                    return;
                }

                var values = valuesProperty.GetValue(operatorDict) as System.Collections.ICollection;
                if (values == null)
                {
                    logWarn("CleanInvalidOperatorDictEntries: values is null.");
                    return;
                }

                // Iterate through all operator dict entries to check for invalid character references
                var keysEnumerator = keys.GetEnumerator();
                var valuesEnumerator = values.GetEnumerator();
                
                var keysToRemove = new List<object>();
                int checkedCount = 0;
                int invalidCharacterCount = 0;

                while (keysEnumerator.MoveNext() && valuesEnumerator.MoveNext())
                {
                    checkedCount++;
                    var keyObj = keysEnumerator.Current;
                    var valueObj = valuesEnumerator.Current;

                    if (keyObj is BuildingBlockKey key && valueObj != null)
                    {
                        // valueObj should be Dictionary<int, byte> type
                        var operatorsDict = valueObj as System.Collections.IDictionary;
                        if (operatorsDict != null)
                        {
                            bool hasInvalidCharacter = false;
                            
                            // Check if each operator ID is valid
                            foreach (int characterId in operatorsDict.Keys)
                            {
                                // Try to get character, null means character does not exist
                                var character = DomainManager.Character.GetElement_Objects(characterId);
                                if (character == null)
                                {
                                    logWarn($"CleanInvalidOperatorDictEntries: found invalid character ID {characterId} in building {key}");
                                    hasInvalidCharacter = true;
                                    invalidCharacterCount++;
                                }
                            }
                            
                            // If has invalid character, remove entire operator dict entry
                            if (hasInvalidCharacter)
                            {
                                keysToRemove.Add(keyObj);
                            }
                        }
                    }
                }

                // Execute removal
                int removedCount = 0;
                foreach (var keyObj in keysToRemove)
                {
                    int removeResult = TryRemoveDictionaryKey(operatorDict, keyObj, logWarn);
                    if (removeResult > 0)
                        removedCount++;
                }

                if (removedCount > 0 || invalidCharacterCount > 0)
                {
                    logInfo($"CleanInvalidOperatorDictEntries: checked {checkedCount} entries, found {invalidCharacterCount} invalid characters, removed {removedCount} operator dict entries.");
                }
            }
            catch (Exception ex)
            {
                logWarn($"CleanInvalidOperatorDictEntries: failed to clean invalid entries: {ex.Message}");
            }
        }

        private static void ClearBuildingOperatorDictForVillage(Location loc, int total,
            Action<string> logInfo, Action<string> logWarn)
        {
            object operatorDict = TryGetBuildingOperatorDictStorage(logWarn);
            if (operatorDict == null)
                return;

            // Strategy change: clean all operator dict entries pointing to current village, not limited to 0..total range
            // Because UI may request any index building, including out-of-range ones
            int removedCount = 0;
            int unsupportedCount = 0;
            
            try
            {
                // Try to get all Keys of the dictionary
                var keysProperty = operatorDict.GetType().GetProperty("Keys");
                if (keysProperty != null)
                {
                    var keys = keysProperty.GetValue(operatorDict) as System.Collections.ICollection;
                    if (keys != null)
                    {
                        var keysToRemove = new List<object>();
                        
                        foreach (var keyObj in keys)
                        {
                            // Check if key is BuildingBlockKey type
                            if (keyObj is BuildingBlockKey key)
                            {
                                // If points to current village, add to removal list
                                if (key.AreaId == loc.AreaId && key.BlockId == loc.BlockId)
                                {
                                    keysToRemove.Add(keyObj);
                                }
                            }
                        }
                        
                        // Execute removal
                        foreach (var keyObj in keysToRemove)
                        {
                            int removeResult = TryRemoveDictionaryKey(operatorDict, keyObj, logWarn);
                            if (removeResult > 0)
                                removedCount++;
                            else if (removeResult < 0)
                                unsupportedCount++;
                        }
                        
                        logInfo($"CleanOperationStateOnImport: scanned all keys, removed {removedCount} entries for village ({loc.AreaId}, {loc.BlockId}).");
                    }
                }
                else
                {
                    logWarn("CleanOperationStateOnImport: cannot access Keys property of BuildingOperatorDict.");
                    unsupportedCount = -1;
                }
            }
            catch (Exception ex)
            {
                logWarn($"CleanOperationStateOnImport: failed to clear all operator dict entries: {ex.Message}");
                // Fallback to original method
                for (short index = 0; index < total; ++index)
                {
                    var key = new BuildingBlockKey(loc.AreaId, loc.BlockId, index);
                    int removeResult = TryRemoveDictionaryKey(operatorDict, key, logWarn);
                    if (removeResult > 0)
                        removedCount++;
                    else if (removeResult < 0)
                        unsupportedCount++;
                }
            }

            if (unsupportedCount > 0)
                logWarn($"CleanOperationStateOnImport: cannot remove {unsupportedCount} BuildingOperatorDict entries because storage type is unsupported.");

            if (unsupportedCount != -1) // 不是属性访问失败
                logInfo($"CleanOperationStateOnImport: removed BuildingOperatorDict entries={removedCount}.");
        }

        private static object TryGetBuildingOperatorDictStorage(Action<string> logWarn)
        {
            try
            {
                Type buildingDomainType = DomainManager.Building.GetType();
                foreach (var field in AccessTools.GetDeclaredFields(buildingDomainType))
                {
                    if (field.Name.IndexOf("BuildingOperatorDict", StringComparison.OrdinalIgnoreCase) < 0 &&
                        field.Name.IndexOf("OperatorDict", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    object value = field.GetValue(DomainManager.Building);
                    if (value != null)
                        return value;
                }

                logWarn("CleanOperationStateOnImport: BuildingOperatorDict storage field not found.");
            }
            catch (Exception ex)
            {
                logWarn($"CleanOperationStateOnImport: failed to access BuildingOperatorDict storage: {ex}");
            }

            return null;
        }

        private static int TryRemoveDictionaryKey(object dictionary, object key, Action<string> logWarn)
        {
            try
            {
                if (dictionary is IDictionary legacyDict)
                {
                    if (!legacyDict.Contains(key))
                        return 0;

                    legacyDict.Remove(key);
                    return 1;
                }

                var removeMethod = dictionary.GetType().GetMethod("Remove", new[] { key.GetType() });
                if (removeMethod == null)
                    return -1;

                object ret = removeMethod.Invoke(dictionary, new[] { key });
                return ret is bool removed ? (removed ? 1 : 0) : 1;
            }
            catch (Exception ex)
            {
                logWarn($"CleanOperationStateOnImport: failed to remove BuildingOperatorDict key={key}: {ex.Message}");
                return -1;
            }
        }

        private static BuildingBlockItem TryGetBuildingConfig(int templateId, Action<string> logWarn)
        {
            if (templateId <= 0)
                return null;

            try
            {
                BuildingBlockItem item = BuildingBlock.Instance[templateId];
                if (item == null)
                    logWarn($"Building config missing: TemplateId={templateId}");
                return item;
            }
            catch (Exception ex)
            {
                logWarn($"Building config lookup failed: TemplateId={templateId}, Error={ex.Message}");
                return null;
            }
        }

        private static int GetBestLeader(DataContext context, BuildingBlockData blockData,
            int addSkillGrade, Action<string> logError)
        {
            try
            {
                BuildingBlockItem item = TryGetBuildingConfig(blockData.TemplateId, logError);
                if (item == null)
                    return -1;

                var available = DomainManager.Taiwu.GetAllVillagersAvailableForWork(true);
                if (available.Count == 0) return -1;

                int bestLeader = -1;
                short roleTemplateId = -1;

                if (item.RequireLifeSkillType > -1)
                {
                    available.Sort((a, b) =>
                        DomainManager.Character.GetAllLifeSkillAttainment(a)[item.RequireLifeSkillType]
                            .CompareTo(DomainManager.Character.GetAllLifeSkillAttainment(b)[item.RequireLifeSkillType]));
                    if (available.Count > 0) bestLeader = available[^1];
                    roleTemplateId = item.RequireLifeSkillType switch
                    {
                        0 or 1 or 2 or 3 or 5 => 4,
                        4 => 6,
                        6 or 7 or 10 or 11 => 1,
                        8 or 9 => 2,
                        12 or 13 => 5,
                        14 => 0,
                        15 => 3,
                        _ => -1,
                    };
                }

                if (addSkillGrade > -1 && bestLeader != -1)
                {
                    var skills = new List<LifeSkillItem>();
                    for (short i = (short)(item.RequireLifeSkillType * 9);
                         i < (short)(item.RequireLifeSkillType * 9 + addSkillGrade + 1); ++i)
                    {
                        skills.Add(new LifeSkillItem(i) { ReadingState = 31 });
                        DomainManager.Information.GainLifeSkillInformationToCharacter(
                            context, bestLeader, LifeSkill.Instance[i].Type);
                    }
                    DomainManager.Character.GmCmd_SetLearnedLifeSkills(context, bestLeader, skills);
                }

                if (item.RequireCombatSkillType > -1)
                {
                    available.Sort((a, b) =>
                        DomainManager.Character.GetAllCombatSkillAttainment(a)[item.RequireCombatSkillType]
                            .CompareTo(DomainManager.Character.GetAllCombatSkillAttainment(b)[item.RequireCombatSkillType]));
                    if (available.Count > 0)
                    {
                        bestLeader = available[^1];
                        roleTemplateId = 5;
                    }
                }

                if (bestLeader != -1 && roleTemplateId != -1)
                {
                    DomainManager.Character.GetElement_Objects(bestLeader)
                        .ForceReplaceClothing(context, (short)(85 + roleTemplateId));
                    DomainManager.Taiwu.SetVillagerRole(context, bestLeader, roleTemplateId);
                }
                return bestLeader;
            }
            catch (Exception ex) { logError($"GetBestLeader failed: {ex}"); return -1; }
        }
    }
}
