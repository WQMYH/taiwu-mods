# Character 领域 API 源码确认版

**最后更新**: 2026-06-20  
**来源范围**: `Decompiler_Tools/TaiwuDecompiler_New/TaiwuDecompiler_Output`  
**重点目录**: `GameData.Shared_e85ad006/GameData/Domains/Character`

## 结论

太吾的角色运行时数据不是一个普通的 `Character` 实例类集中暴露出来。源码中与 mod 开发直接相关的 Character API 分为三层：

| 层级 | 位置 | 用途 |
|---|---|---|
| 领域调用接口 | `Assembly-CSharp_da99b9b8/GameData/Domains/Character/CharacterDomainMethod.cs` | 前端/插件侧调用角色领域方法，分 `Call` 和 `AsyncCall` |
| 共享数据结构 | `GameData.Shared_e85ad006/GameData/Domains/Character` | DTO、字段 ID、序列化结构、枚举常量 |
| 配置表 | `GameData.Shared_e85ad006/Config/Character.cs` 和 `CharacterItem.cs` | 角色模板配置，不是运行时角色实例 |

如果你要做 mod，优先查 `CharacterDomainMethod` 的方法签名，再查返回值对应的 `Display` / `Relation` / `AvatarSystem` / 技能和背包相关 DTO。

## 调用模式

`CharacterDomainMethod` 同时提供两套调用方式：

```csharp
// 发送调用，结果通过 listenerId 对应的通知机制返回
CharacterDomainMethod.Call.GetCharacterDisplayData(listenerId, charId);

// 发送调用，并在 callback 中读取 RawDataPool
CharacterDomainMethod.AsyncCall.GetCharacterDisplayData(
    requestHandler,
    charId,
    callback
);
```

常见约定：

| 名称 | 含义 |
|---|---|
| `charId` | 角色 ID |
| `listenerId` | 游戏数据监听器 ID，常见于 `Call` |
| `IAsyncMethodRequestHandler` | 异步请求发起者，常见于 `AsyncCall` |
| `AsyncMethodCallbackDelegate` | 异步回调，通常从 `RawDataPool` 反序列化结果 |
| `GmCmd_*` | 直接修改或调试倾向较强的方法，使用前要考虑存档和兼容性风险 |

不要硬编码 method id。源码已提供强类型包装方法，直接调用包装方法比自己拼 `GameDataBridge.AddMethodCall` 更稳。

## 领域数据 ID

定义位置: `GameData.Shared_e85ad006/GameData/Domains/Character/CharacterDomainHelper.cs`

| ID | 名称 | 说明 |
|---:|---|---|
| 0 | `Objects` | 角色对象集合，角色字段名由 `CharacterHelper.FieldIds` 映射 |
| 1 | `NextObjectId` | 下一个角色对象 ID |
| 2 | `DeadCharacters` | 死亡角色数据 |
| 3 | `DeadCharDeletionStates` | 死亡角色删除状态 |
| 4 | `RecentDeadCharacters` | 近期死亡角色 |
| 5 | `WaitingReincarnationChars` | 等待转世角色 |
| 6 | `Graves` | 坟墓数据，字段名映射到 `GraveHelper` |
| 7 | `PregnantStates` | 怀孕状态 |
| 8 | `PregnancyLockEndDates` | 怀孕锁定结束日期 |
| 9 | `UnguardedChars` | 未受保护角色 |
| 10 | `KidnappedChars` | 被绑架角色 |
| 11 | `Relations` | 角色关系 |
| 12 | `ActualBloodParents` | 实际血亲父母 |
| 13 | `CharacterGroups` | 角色分组 |
| 14 | `JoinGroupDates` | 加入分组日期 |
| 15 | `DebtsToTaiwu` | 对太吾债务 |
| 16 | `SoldLibrarySkillBooks` | 已售藏书技能书 |
| 17 | `UsedCombatResourceObsoletes` | 已废弃战斗资源记录 |
| 18 | `AvatarElementGrowthProgress` | 立绘部件成长进度 |
| 19 | `TargetedForAssassination` | 被刺杀目标 |
| 20 | `PrioritizedActions` | 优先行为 |
| 21 | `CrossAreaMoveInfos` | 跨地区移动信息 |
| 22 | `OngoingVengeances` | 进行中的复仇 |
| 23 | `MournedOthersChars` | 悼念他人的角色 |
| 24 | `PregeneratedCityTownGuards` | 预生成城镇守卫 |
| 25 | `PregeneratedRandomEnemies` | 预生成随机敌人 |
| 26 | `ForceRebelLocation` | 强制叛乱位置 |
| 27 | `ForceKindLocation` | 强制善意位置 |
| 28 | `AvoidDeathCharId` | 避免死亡角色 ID |
| 29 | `SubscriberOrders` | 订阅者订单 |

## 常用角色字段 ID

定义位置: `GameData.Shared_e85ad006/GameData/Domains/Character/CharacterHelper.cs`

这些 ID 用于角色 `Objects` 数据的字段映射。mod 开发中通常优先使用 `CharacterDomainMethod` 包装方法；只有做底层监听、反序列化、补丁或调试时才需要直接关心字段 ID。

### 身份与基础信息

| ID | 字段 |
|---:|---|
| 0 | `Id` |
| 1 | `TemplateId` |
| 2 | `CreatingType` |
| 3 | `Gender` |
| 4 | `ActualAge` |
| 5 | `BirthMonth` |
| 6 | `Happiness` |
| 8 | `OrganizationInfo` |
| 9 | `IdealSect` |
| 15 | `XiangshuType` |
| 16 | `MonkType` |
| 37 | `FullName` |
| 38 | `MonasticTitle` |
| 39 | `Avatar` |
| 55 | `BirthLocation` |
| 56 | `Location` |
| 66 | `CurrAge` |
| 76 | `PhysiologicalAge` |
| 77 | `Fame` |
| 78 | `Morality` |
| 79 | `Attraction` |

### 特性、属性、健康

| ID | 字段 |
|---:|---|
| 17 | `FeatureIds` |
| 18 | `BaseMainAttributes` |
| 19 | `Health` |
| 20 | `BaseMaxHealth` |
| 21 | `DisorderOfQi` |
| 22 | `HaveLeftArm` |
| 23 | `HaveRightArm` |
| 24 | `HaveLeftLeg` |
| 25 | `HaveRightLeg` |
| 26 | `Injuries` |
| 40 | `PotentialFeatureIds` |
| 43 | `CurrMainAttributes` |
| 44 | `Poisoned` |
| 45 | `InjuriesRecoveryProgress` |
| 80 | `MaxMainAttributes` |
| 95 | `MaxHealth` |

### 技艺、功法、内力

| ID | 字段 |
|---:|---|
| 27 | `ExtraNeili` |
| 28 | `ConsummateLevel` |
| 29 | `LearnedLifeSkills` |
| 30 | `BaseLifeSkillQualifications` |
| 31 | `LifeSkillQualificationGrowthType` |
| 32 | `BaseCombatSkillQualifications` |
| 33 | `CombatSkillQualificationGrowthType` |
| 46 | `CurrNeili` |
| 47 | `LoopingNeigong` |
| 48 | `BaseNeiliAllocation` |
| 49 | `ExtraNeiliAllocation` |
| 50 | `BaseNeiliProportionOfFiveElements` |
| 60 | `LearnedCombatSkills` |
| 61 | `EquippedCombatSkills` |
| 62 | `CombatSkillAttainmentPanels` |
| 63 | `SkillQualificationBonuses` |
| 97 | `LifeSkillQualifications` |
| 98 | `LifeSkillAttainments` |
| 99 | `CombatSkillQualifications` |
| 100 | `CombatSkillAttainments` |
| 109 | `MaxNeili` |
| 110 | `NeiliAllocation` |
| 111 | `NeiliProportionOfFiveElements` |
| 112 | `NeiliType` |
| 116 | `MaxConsummateLevel` |
| 117 | `CombatSkillEquipment` |

### 背包、装备、物品

| ID | 字段 |
|---:|---|
| 34 | `Resources` |
| 35 | `LovingItemSubType` |
| 36 | `HatingItemSubType` |
| 52 | `LovingItemRevealed` |
| 53 | `HatingItemRevealed` |
| 57 | `Equipment` |
| 58 | `Inventory` |
| 59 | `EatingItems` |
| 104 | `MaxInventoryLoad` |
| 105 | `CurrInventoryLoad` |
| 106 | `MaxEquipmentLoad` |
| 107 | `CurrEquipmentLoad` |
| 108 | `InventoryTotalValue` |

### AI、关系、状态

| ID | 字段 |
|---:|---|
| 68 | `ExternalRelationState` |
| 69 | `KidnapperId` |
| 70 | `LeaderId` |
| 71 | `FactionId` |
| 72 | `PersonalNeeds` |
| 73 | `ActionEnergies` |
| 74 | `NpcTravelTargets` |
| 75 | `PrioritizedActionCooldowns` |
| 101 | `Personalities` |
| 102 | `HobbyChangingPeriod` |
| 103 | `FavorabilityChangingFactor` |

### 模板配置字段

`CharacterHelper.FieldIds` 后半段包含大量从 `Config.CharacterItem` 派生或关联的模板字段，例如 `Surname`、`GivenName`、`CanDefeat`、`CombatAi`、`CanMove`、`CanOpenCharacterMenu`、`CanSpeak`、`GroupId` 等。修改运行时角色时应优先找领域方法；改模板行为时再查看 `Config/CharacterItem.cs`。

## 常用领域方法

定义位置: `Assembly-CSharp_da99b9b8/GameData/Domains/Character/CharacterDomainMethod.cs`

下面列的是 mod 开发中最常遇到的包装方法。`Call` 和 `AsyncCall` 基本一一对应，`AsyncCall` 额外接收 `IAsyncMethodRequestHandler` 和 callback。

### 读取角色显示数据

```csharp
void GetCharacterDisplayData(int listenerId, int charId)
void GetCharacterDisplayDataList(int listenerId, List<int> charIdList)
void GetCharacterDisplayDataForTooltip(int listenerId, int charId)
void GetCharacterDisplayDataForMapBlock(int listenerId, int charId)
void GetCharacterDisplayDataListForRelations(int listenerId, List<int> charIds)
void GetCharacterDisplayDataListForRelationsWithRelationType(int listenerId, int currCharId, List<int> charIds)
void GetCharacterDisplayDataListForUltimateSelect(int listenerId, List<int> charIdList)
void GetCharacterLocationDisplayData(int listenerId, int charId)
void GetGroupCharDisplayDataList(int listenerId, List<int> charIdList)
```

常见返回结构：

| 方法 | 主要关注 DTO |
|---|---|
| `GetCharacterDisplayData` | `Display/CharacterDisplayData.cs` |
| `GetCharacterDisplayDataForTooltip` | `Display/CharacterDisplayDataForTooltip.cs` |
| `GetCharacterDisplayDataForMapBlock` | `Display/CharacterDisplayDataForMapBlock.cs` |
| `GetCharacterLocationDisplayData` | `Display/CharacterLocationDisplayData.cs` |
| `GetCharacterDisplayDataListForRelations*` | `Display/CharacterDisplayDataForRelations.cs`、`RelatedCharactersForRelations.cs` |

### 姓名、年龄、称号、基础状态

```csharp
void GetNameRelatedData(int listenerId, int charId)
void GetNameRelatedDataList(int listenerId, List<int> charIds)
void GetNameAndLifeRelatedData(int listenerId, int charId)
void GetNameAndLifeRelatedDataList(int listenerId, List<int> charIds)
void GenerateRandomName(int listenerId)
void GenerateRandomHanName(int listenerId, int customSurnameId, short surnameId, sbyte gender)
void GenerateRandomZangName(int listenerId, sbyte gender)
void GenerateRandomChildName(int listenerId, sbyte gender, FullName parentName)
void GetDisplayingAge(int listenerId, int charId)
void GetPhysiologicalAge(int listenerId, int charId)
void GetPhysiologicalAgeAffector(int listenerId, int charId)
void GetCharacterBirthDate(int listenerId, int charId)
void GetTitles(int listenerId, int charId)
void GetFameType(int listenerId, int charId)
void GetHealthType(int listenerId, int charId)
void GetLeftMaxHealth(int listenerId, int charId)
void GetHealthRecovery(int listenerId, int charId)
void GetCharacterAllBodyPartExists(int listenerId, int charId)
```

### 关系、好感、族谱

```csharp
void GetRelatedCharactersForRelations(int listenerId, int charId)
void TryCreateRelation(int charId, int relatedCharId)
void GetGenealogy(int listenerId, int charId)
void GetFavorability(int listenerId, int charId, int relatedCharId)
void CalcFavorabilityDelta(int listenerId, int characterId, int relatedCharId, int baseDelta, short type)
void CalcItemsFavorabilityDelta(int listenerId, int characterId, int relatedCharId, Inventory items, short type)
void GetRelationBetweenCharacters(int listenerId, int charId, int relatedCharId)
void TryAddAndApplyOneWayRelation(int charId, int relatedCharId, ushort relationType)
void TryRemoveOneWayRelation(int charId, int relatedCharId, ushort relationType)
void GmCmd_AddRelation(int charId, int relatedCharId, ushort addingType)
void GmCmd_RemoveRelation(int charId, int relatedCharId, ushort removeType)
void GmCmd_ChangeFavorability(int selfCharId, int relatedCharId, short delta)
```

### 特性、声望、组织

```csharp
void GetFeatureMedalValue(int listenerId, int charId, sbyte medalType)
void GetFeatureMedalValueList(int listenerId, List<int> charIdList, sbyte medalType)
void GmCmd_AddFeature(int charId, short templateId)
void GmCmd_SetFeatures(int charId, List<short> features)
void GmCmd_RemoveFeature(int charId, short templateId)
void GmCmd_RecordFameAction(int charId, short fameActionId)
void GmCmd_RecordFameAction(int charId, short fameActionId, int targetCharId)
void GmCmd_ClearFameActionRecords(int charId)
void GmCmd_ForceChangeOrganization(int charId, sbyte orgTemplateId)
void GmCmd_ForceChangeGrade(int listenerId, int charId, sbyte grade, bool principal)
void GmCmd_ForceChangeOrganizationByName(int listenerId, int charId, string settlementName)
```

### 技艺、功法、内力、战斗配置

```csharp
void GetCharacterAttributeDisplayData(int listenerId, int charId)
void GetCharacterLifeSkillAttainmentList(int listenerId, List<int> charIdList, sbyte lifeSkillType)
void GetCombatSkillAttainment(int listenerId, int charId, sbyte type)
void GetAllCombatSkillAttainment(int listenerId, int charId)
void GetLifeSkillAttainment(int listenerId, int charId, sbyte type)
void GetAllLifeSkillAttainment(int listenerId, int charId)
void LearnCombatSkill(int charId, short skillTemplateId)
void LearnCombatSkill(int charId, short skillTemplateId, ushort readingState)
void LearnLifeSkill(int charId, short skillTemplateId)
void LearnLifeSkill(int charId, short skillTemplateId, byte readingState)
void GmCmd_SetLearnedLifeSkills(int charId, List<LifeSkillItem> learnedLifeSkills)
void GmCmd_ForgetCombatSkill(int charId, short skillTemplateId)
void GmCmd_RevokeCombatSkill(int charId, List<short> skillTemplateIdList)
void GmCmd_SetCurrNeili(int charId, int value)
void GmCmd_SetCharBaseNeiliProportionOfFiveElements(int charId, NeiliProportionOfFiveElements fiveElements)
void AllocateNeili(int charId, byte neiliAllocationType)
void DeallocateNeili(int charId, byte neiliAllocationType)
void AutoAllocateNeili(int charId)
void SetNeiliAllocationLock(int charId, bool isLocked)
void GetChangeOfQiDisorder(int listenerId, int charId)
void GmCmd_ChangeCharDisorderOfQi(int charId, int delta)
void SetCombatSkillSlot(int charId, sbyte equipType, int index, short skillTemplateId)
void UnequipAllCombatSkills(int charId)
void AutoEquipCombatSkills(int charId)
void AutoEquipCombatSkills(int charId, short combatConfigTemplateId)
void AddEquippedCombatSkill(int charId, short skillTemplateId)
void RemoveEquippedCombatSkill(int charId, short skillTemplateId)
```

### 物品、背包、装备、资源

```csharp
void TransferResourcesWithDebt(int srcCharId, int destCharId, ResourceInts resources, bool checkFavorability)
void TransferInventoryItemWithDebt(int srcCharId, int destCharId, ItemKey itemKey, int amount, bool checkFavorability)
void TransferInventoryItemListWithDebt(int srcCharId, int destCharId, List<ItemKey> keyList, bool checkFavorability)
void TransferInventoryItemFromAToB(int charA, int charB, ItemKey itemKey, int amount)
void CreateInventoryItem(int charId, sbyte itemType, short templateId, int amount)
void GetInventoryItems(int listenerId, int charId, short itemSubType)
void GetInventoryItemsByItemType(int listenerId, int charId, sbyte itemType)
void GetAllInventoryItems(int listenerId, int charId)
void GetAllInventoryItemsExcludeValueZero(int listenerId, int charId)
void GetInventoryItemAmount(int listenerId, int charId, sbyte itemType, short templateId)
void GetInventoryItemDisplayData(int listenerId, int charId, ItemKey itemKey)
void InventoryContainsItem(int listenerId, int charId, ItemKey itemKey)
void GetAllEquipmentItems(int listenerId, int charId)
void GetInventoryEquipment(int listenerId, int charId)
void ChangeEquipment(int charId, sbyte srcSlot, sbyte destSlot, ItemKey srcItemKey)
void AutoEquipItems(int charId)
void AddEatingItem(int charId, ItemKey itemKey)
void AddEatingItem(int charId, ItemKey itemKey, List<sbyte> targetBodyParts)
void SimulateEatingEffect(int listenerId, int charId, ItemKey itemKey, int amount)
```

### 绑架、死亡、转世、特殊状态

```csharp
void AddKidnappedCharacter(int charId, int kidnappedCharId, ItemKey ropeItemKey)
void TransferKidnappedCharacters(int targetKidnapperId, int sourceKidnapperId, KidnappedCharacterList kidnappedCharsToTransfer)
void ChangeKidnappedCharacterRope(int kidnapperId, int kidnappedCharId, ItemKey newRopeKey)
void GetKidnapMaxSlotCount(int listenerId, int charId)
void TransferKidnappedCharacter(int targetKidnapperId, int sourceKidnapperId, KidnappedCharacter kidnappedCharData)
void RemoveKidnappedCharacter(int kidnappedCharId, int kidnapperId, bool isEscaped)
void GetSomeoneKidnapCharacters(int listenerId, int charId)
void GetKidnappedCharacterDisplayData(int listenerId, int charId)
void TryGetDeadCharacter(int listenerId, int charId)
void GmCmd_Die(int charId)
void GmCmd_GetAliveCharByPreexistenceChar(int listenerId, int preexistenceCharId)
void GmCmd_LogCharacterSamsaraInfo()
void GetCharacterSamsaraData(int listenerId, int charId)
```

### 排序和筛选

```csharp
void GetFilteredCharacterCounts(int listenerId)
void ClearCharacterSortFilter()
void UpdateSortFilterSettings(int listenerId, CharacterSortFilterSettings sortFilterSettings)
void InitializeCharacterSortFilter(int listenerId, CharacterSortFilterSettings sortFilterSettings)
void FindNameInCurrentSortFilter(int listenerId, string name)
void GetMaxSortingTypeCharIds(int listenerId, List<int> sortingTypes, sbyte filterSubId)
void SortCharacterListByMaxCombatSkill(int listenerId, List<int> managerList)
```

## 关键 DTO

### CharacterDisplayData

定义位置: `GameData.Shared_e85ad006/GameData/Domains/Character/Display/CharacterDisplayData.cs`

这是 UI 和多数 mod 读取角色信息时最常用的显示 DTO。

| 字段 | 类型 |
|---|---|
| `CharacterId` | `int` |
| `TemplateId` | `short` |
| `CreatingType` | `byte` |
| `Gender` | `sbyte` |
| `FullName` | `FullName` |
| `MonkType` | `byte` |
| `MonasticTitle` | `MonasticTitle` |
| `AvatarRelatedData` | `AvatarRelatedData` |
| `PhysiologicalAge` | `short` |
| `CurrAge` | `short` |
| `OrgInfo` | `OrganizationInfo` |
| `BehaviorType` | `sbyte` |
| `FameType` | `sbyte` |
| `FavorabilityToTaiwu` | `short` |
| `IsApproveTaiwu` | `bool` |
| `ApproveTaiwu` | `short` |
| `InfluencePower` | `short` |
| `Contribution` | `int` |
| `ContributionPerMonth` | `int` |
| `TitleIds` | `List<short>` |
| `CompletelyInfected` | `bool` |
| `ValidKidnapSlotCount` | `byte` |
| `AliveState` | `sbyte` |
| `Location` | `Location` |
| `BirthDate` | `int` |
| `ExternalRelationState` | `byte` |
| `LegendaryBookOwnerState` | `sbyte` |
| `CustomDisplayNameId` | `int` |
| `SettlementTreasuryGuardInfo` | `byte` |
| `BountyPunishmentSeverity` | `sbyte` |
| `BountyOrgTemplate` | `sbyte` |
| `CanNotSpeak` | `bool` |
| `IsFollowedByTaiwu` | `bool` |
| `NickNameId` | `int` |
| `ExtraNameTextTemplateId` | `int` |
| `IdealSect` | `sbyte` |
| `CurrOrgTemplate` | `sbyte` |
| `DarkAshProtector` | `uint` |
| `DarkAshCounter` | `DarkAshCounter` |
| `OrganizationMemberPotentialSuccessor` | `CharacterDisplayData` |
| `Happiness` | `sbyte` |

### 其他高频结构

| 文件 | 用途 |
|---|---|
| `Display/NameRelatedData.cs` | 姓名相关返回数据 |
| `Display/NameAndLifeRelatedData.cs` | 姓名 + 生死状态 |
| `Display/CharacterAttributeDisplayData.cs` | 角色属性展示数据 |
| `Display/CharacterDisplayDataForTooltip.cs` | 鼠标提示展示数据 |
| `Display/CharacterDisplayDataForMapBlock.cs` | 地图格角色展示数据 |
| `Display/CharacterDisplayDataForRelations.cs` | 关系界面展示数据 |
| `Relation/RelatedCharacter.cs` | 单个关系条目 |
| `Relation/RelatedCharacters.cs` | 关系集合 |
| `Relation/RelationKey.cs` | 关系键 |
| `CharacterList.cs` | `List<int>` 包装，序列化角色 ID 列表 |
| `CharacterSet.cs` | `HashSet<int>` 包装，序列化角色 ID 集合 |
| `KidnappedCharacter.cs` | 被绑架角色条目 |
| `KidnappedCharacterList.cs` | 被绑架角色集合 |
| `FullName.cs` | 姓名结构 |
| `AvatarSystem/AvatarData.cs` | 头像/立绘完整数据 |
| `Display/AvatarRelatedData.cs` | 头像相关展示数据 |

## 使用建议

1. 只读显示信息时，优先使用 `GetCharacterDisplayData*`，避免直接监听底层 `Objects` 字段。
2. 需要修改角色数据时，优先找现成的 `CharacterDomainMethod.Call` / `AsyncCall` 包装方法，例如 `GmCmd_AddFeature`、`GmCmd_SetCurrNeili`、`LearnCombatSkill`。
3. `GmCmd_*` 方法能直接改变游戏状态，适合后端逻辑或调试功能，前端 UI 侧不要随意调用。
4. `CharacterHelper.FieldIds` 是底层字段映射，不等于稳定的公共 API。游戏更新后字段 ID 可能变化，mod 应尽量通过领域方法和 DTO 交互。
5. 旧文档中提到的“Character 实例方法”需要用 dnSpy 或实际项目编译验证；本文件只记录当前反编译源码和 codegraph 能确认的内容。

## 查找路径速记

| 目标 | 路径 |
|---|---|
| 领域方法签名 | `Assembly-CSharp_da99b9b8/GameData/Domains/Character/CharacterDomainMethod.cs` |
| MethodId / DataId | `GameData.Shared_e85ad006/GameData/Domains/Character/CharacterDomainHelper.cs` |
| 角色字段 ID | `GameData.Shared_e85ad006/GameData/Domains/Character/CharacterHelper.cs` |
| 显示 DTO | `GameData.Shared_e85ad006/GameData/Domains/Character/Display` |
| 关系 DTO | `GameData.Shared_e85ad006/GameData/Domains/Character/Relation` |
| 头像 DTO | `GameData.Shared_e85ad006/GameData/Domains/Character/AvatarSystem` |
| 模板配置 | `GameData.Shared_e85ad006/Config/Character.cs`、`CharacterItem.cs` |
