# Character API 统一参考

**最后更新**: 2026-06-26  
**状态**: 当前源码确认 + 参考 MOD 实战用法合并版  
**适用范围**: 太吾绘卷 1.0.32 反编译源码、`AutoMonthlyEvent.Executor`、`spied/updated` 参考项目

本文合并并校准旧文档中的 Character 相关结论。旧文档中来自 dnSpy 或参考 MOD 的内容不全部等价于稳定公共 API；本文按可靠程度分层记录。

## 1. 可靠性分层

| 层级 | 来源 | 可靠性 | 用途 |
|---|---|---|---|
| A. 领域方法 | `CharacterDomainMethod.Call/AsyncCall` | 高 | 前端/插件跨域读取或请求角色数据 |
| B. 显示 DTO | `CharacterDisplayData` 等 `Display` 类型 | 高 | 事件窗口、UI、前端自动化读取角色信息 |
| C. 后端实例 | `DomainManager.Character.GetElement_Objects(id)` 返回的 `Character` | 中高 | 后端 Patch 或后端 Mod 直接读取/修改角色 |
| D. 私有字段/反射 | `_attraction`、`_birthMonth` 等 | 中低 | 仅适合版本锁定、Patch 型功能 |
| E. 旧推测 | 旧 dnSpy 记录但当前未复核项 | 低 | 只能作为查找线索，不能直接写入自动化逻辑 |

## 2. 源码定位

| 目标 | 路径 |
|---|---|
| 领域调用包装 | `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/GameData/Domains/Character/CharacterDomainMethod.cs` |
| 角色显示数据 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/Character/Display/CharacterDisplayData.cs` |
| 角色字段 ID | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/Character/CharacterHelper.cs` |
| 当前 Executor 关系判断 | `mods/AutoMonthlyEvent.Executor/Plugins/AutoMonthlyEvent.Executor.Frontend/RelationConditionResolver.cs` |
| 参考后端角色 API | `spied/updated/TaiwuToolbox.Backend/CharacterBackendModule.cs` |
| 参考密友/资质 API | `spied/updated/SuperGoodFriendBackend/CreateFriendPatch.cs` |

## 3. 前端/插件侧调用模式

`CharacterDomainMethod` 有两套包装：

```csharp
// Call：发起领域方法调用，通常通过 listenerId 收通知。
CharacterDomainMethod.Call.GetCharacterDisplayData(listenerId, charId);

// AsyncCall：有返回值的方法可通过 IAsyncMethodRequestHandler + callback 读取 RawDataPool。
CharacterDomainMethod.AsyncCall.GetRelationBetweenCharacters(
    requestHandler,
    charId,
    relatedCharId,
    callback
);
```

注意：

- `AsyncCall` 只适合有返回值的方法；源码中很多无返回方法的 `AsyncCall` 包装被标记为 `Obsolete(..., true)`。
- 不要手写 method id；优先调用 `CharacterDomainMethod.Call/AsyncCall` 包装。
- 回调返回 `RawDataPool` 时要按源码返回类型反序列化。

## 4. AutoMonthlyEvent 已验证角色读取流程

当前 `AutoMonthlyEvent.Executor` 前端请求类事件使用以下链路：

1. 从 `EventModel.DisplayingEventData.MainCharacter/TargetCharacter` 读取 `CharacterDisplayData`。
2. 通过 `BasicGameData.TaiwuCharId` 排除太吾，确定 NPC 请求者。
3. 调用 `CharacterDomainMethod.AsyncCall.GetRelationBetweenCharacters(handler, taiwuId, requesterId, callback)`。
4. 反序列化 `(ushort, ushort)`，取 `Item1` 作为关系位值。
5. 用 `CharacterDisplayData.FavorabilityToTaiwu` 作为好感兜底。

关键代码模式：

```csharp
CharacterDomainMethod.AsyncCall.GetRelationBetweenCharacters(
    handler,
    taiwuId,
    requester.CharacterId,
    delegate(int offset, RawDataPool pool)
    {
        (ushort, ushort) relation = default;
        Serializer.Deserialize(pool, offset, ref relation);
        ushort relationType = relation.Item1;
    });
```

已使用关系位值：

| 位值 | 含义 |
|---:|---|
| `1` | 血亲父母 |
| `2` | 血亲子女 |
| `8` | 继亲父母 |
| `16` | 继亲子女 |
| `64` | 义亲父母 |
| `128` | 义亲子女 |
| `512` | 结义 |
| `1024` | 配偶 |
| `8192` | 朋友 |

判断建议：

```csharp
bool matched = (relationTypes & allowedRelationType) != 0;
```

不要用 `relationTypes == allowedRelationType`，因为关系可能是复合位。

## 5. CharacterDisplayData 常用字段

源码位置：`.../Character/Display/CharacterDisplayData.cs`

| 字段 | 类型 | 用途 |
|---|---|---|
| `CharacterId` | `int` | 角色 ID |
| `TemplateId` | `short` | 模板 ID |
| `Gender` | `sbyte` | 性别显示数据 |
| `FullName` | `FullName` | 姓名结构 |
| `PhysiologicalAge` | `short` | 生理年龄 |
| `CurrAge` | `short` | 当前显示年龄，源码警告建议非必要时用 `PhysiologicalAge` |
| `OrgInfo` | `OrganizationInfo` | 组织信息 |
| `BehaviorType` | `sbyte` | 立场，收养弃婴已用该字段判断 |
| `FameType` | `sbyte` | 名声类型 |
| `FavorabilityToTaiwu` | `short` | 对太吾好感，前端请求判断可直接读取 |
| `IsApproveTaiwu` | `bool` | 是否认可太吾 |
| `ApproveTaiwu` | `short` | 认可值 |
| `Location` | `Location` | 当前位置 |
| `BirthDate` | `int` | 出生日期 |
| `AliveState` | `sbyte` | 生死状态 |
| `Happiness` | `sbyte` | 心情 |

适用场景：

- 前端事件窗口判断 NPC、关系、好感、婴孩立场时优先使用。
- 这是显示 DTO，不应直接当作完整角色实体修改。

## 6. 常用 CharacterDomainMethod

以下签名在当前源码中确认存在。

### 显示数据

```csharp
GetCharacterDisplayData(int listenerId, int charId)
GetCharacterDisplayData(IAsyncMethodRequestHandler requestHandler, int charId, AsyncMethodCallbackDelegate callback)
```

### 关系与好感

```csharp
GetFavorability(int listenerId, int charId, int relatedCharId)
GetFavorability(IAsyncMethodRequestHandler requestHandler, int charId, int relatedCharId, AsyncMethodCallbackDelegate callback)

GetRelationBetweenCharacters(int listenerId, int charId, int relatedCharId)
GetRelationBetweenCharacters(IAsyncMethodRequestHandler requestHandler, int charId, int relatedCharId, AsyncMethodCallbackDelegate callback)
```

后端参考项目也直接使用：

```csharp
DomainManager.Character.GetFavorability(charId, relatedCharId);
DomainManager.Character.GetRelationBetweenCharacters(charId, relatedCharId);
```

### 姓名与生育/取名

```csharp
GenerateRandomChildName(int listenerId, sbyte gender, FullName parentName)
GenerateRandomChildName(IAsyncMethodRequestHandler requestHandler, sbyte gender, FullName parentName, AsyncMethodCallbackDelegate callback)
```

后端参考项目常见用法：

```csharp
character.GetFullName().GetName(character.GetGender(), DomainManager.World.GetCustomTexts());
DomainManager.Character.GenerateRandomHanName(context, customSurnameId, surnameId, character.GetGender());
```

### 年龄

```csharp
GetPhysiologicalAge(int listenerId, int charId)
GetPhysiologicalAge(IAsyncMethodRequestHandler requestHandler, int charId, AsyncMethodCallbackDelegate callback)
```

后端参考项目直接使用：

```csharp
DomainManager.Character.GetElement_Objects(charId).GetPhysiologicalAge();
```

### 物品/资源转移

```csharp
TransferResourcesWithDebt(int srcCharId, int destCharId, ResourceInts resources, bool checkFavorability)
TransferInventoryItemWithDebt(int srcCharId, int destCharId, ItemKey itemKey, int amount, bool checkFavorability)
```

注意：源码中对应 `AsyncCall` 被标记为不可用；需要用 `Call` 或后端 helper。

### 特性 GM 方法

```csharp
GmCmd_AddFeature(int charId, short templateId)
GmCmd_SetFeatures(int charId, List<short> features)
GmCmd_RemoveFeature(int charId, short templateId)
```

这些会改变角色状态，默认视为高风险。前端自动化不应直接调用。

## 7. 后端实例 API：参考 MOD 已使用项

参考来源：`spied/updated/TaiwuToolbox.Backend`、`SuperGoodFriendBackend`、`FreeStartingPointsMod.Backend`。

| API | 用途 | 可靠性 |
|---|---|---|
| `DomainManager.Character.GetElement_Objects(charId)` | 取后端 `Character` 实体 | 中高 |
| `character.GetId()` | 角色 ID | 中高 |
| `character.GetGender()` | 性别 | 中高 |
| `character.GetFullName()` | 姓名结构 | 中高 |
| `character.GetAttraction()` | 魅力 | 中高，参考项目 patch 过 |
| `character.GetPhysiologicalAge()` | 生理年龄 | 中高 |
| `character.GetBaseCombatSkillQualifications()` | 武学资质 | 中高 |
| `character.SetBaseCombatSkillQualifications(ref value, context)` | 写武学资质 | 中高，高风险修改 |
| `character.GetBaseLifeSkillQualifications()` | 技艺资质 | 中高 |
| `character.SetBaseLifeSkillQualifications(ref value, context)` | 写技艺资质 | 中高，高风险修改 |
| `DomainManager.Character.GetFavorability(a, b)` | 后端查好感 | 中高 |
| `DomainManager.Character.GetRelationBetweenCharacters(a, b)` | 后端查关系 | 中高 |

## 8. 反射/私有字段规则

历史参考项目使用过 `GetValue/SetValue` 访问私有字段，例如 `_birthMonth`。这种做法版本风险高：

```csharp
character.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
```

使用条件：

- 只在后端 Patch 中使用。
- 每个游戏版本都重新验证字段名。
- 必须有异常保护和回退路径。
- 不应写入通用 API 文档的“稳定接口”段。

## 9. 推荐查找流程

1. 先查 `CharacterDomainMethod.cs` 是否有包装方法。
2. 如果是事件窗口或前端逻辑，查 `CharacterDisplayData` 是否已经包含所需字段。
3. 如果是后端修改，查参考 MOD 是否通过 `DomainManager.Character.GetElement_Objects` 使用过同名实例方法。
4. 如果只能反射私有字段，标为“版本锁定高风险”，不要放进默认自动化逻辑。
5. 每次实装前在日志中记录：`charId / relationType / favorability / field source / decision`。

## 10. 当前待补充

- `Character` 实体类完整源码未在本轮展开；现有后端实例 API 主要来自参考 MOD 调用点。
- 未来资质、坏特性、父母是否为太吾村民等筛选条件还需要继续追踪对应稳定 API。
- 新生儿取名目前 Executor 前端用 `EventModel.SetInputResult`，未直接调用后端姓名 API。
