# 怀孕与生育事件 API 源码确认版

**最后更新**: 2026-06-24  
**来源范围**: `spied/The Scroll of Taiwu`  
**重点目录**:

- `GameData.Shared_e85ad006/GameData/Domains/Character`
- `GameData.Shared_e85ad006/GameData/Domains/World/MonthlyEvent`
- `GameData.Shared_e85ad006/GameData/Domains/World/Notification`
- `GameData.Shared_e85ad006/GameData/Domains/Information/Collection`
- `TaiwuDecompiler_da99b9b8/GameData/Domains/Character`

## 结论

怀孕与生育相关 API 分为三类，开发 MOD 时不要混用：

| 类别 | 用途 | 代表入口 |
|---|---|---|
| 角色域数据与调用 | 查询或触发角色领域逻辑 | `CharacterDomainMethod`、`CharacterDomainHelper` |
| 过月事件/通知/密闻/经历记录 | 写入显示层或记录层内容 | `MonthlyEventCollection`、`MonthlyNotificationCollection`、`SecretInformationCollection`、`LifeRecord` |
| 配置常量 | 事件、特性、记录模板 ID | `CharacterFeature`、`MonthlyEvent`、`MonthlyNotification`、`LifeRecord` |

过月事件、通知、密闻和人生经历方法主要用于追加记录，不等价于初始化或修复 `PregnantStates` 运行时字典。修复怀孕状态缺失时，必须同时考虑角色特性和角色域中的怀孕状态数据。

## 角色域数据

定义位置: `GameData.Shared_e85ad006/GameData/Domains/Character/CharacterDomainHelper.cs`

| ID | 名称 | 说明 |
|---:|---|---|
| 7 | `PregnantStates` | 角色怀孕状态集合 |
| 8 | `PregnancyLockEndDates` | 怀孕锁定结束日期集合 |

相关方法 ID:

| ID | 方法名 | 说明 |
|---:|---|---|
| 77 | `GmCmd_MakeCharacterHaveSex` | 触发角色发生性行为，可能影响怀孕状态 |
| 78 | `GmCmd_GetCharacterPregnancyLockEndDates` | 查询角色怀孕锁定结束日期 |

`PregnantStates` 是角色域数据，不是 `CharacterFeature.Pregnant` 特性本身。只给角色补 `Pregnant` 特性，不会自动证明 `PregnantStates[charId]` 已存在。

## 角色域调用

定义位置: `TaiwuDecompiler_da99b9b8/GameData/Domains/Character/CharacterDomainMethod.cs`

### 触发性行为

```csharp
CharacterDomainMethod.Call.GmCmd_MakeCharacterHaveSex(
    int selfCharId,
    int targetCharId,
    bool isRaped,
    int pregnantRemainTime
);
```

源码包装:

```csharp
GameDataBridge.AddMethodCall(
    -1, 4, 77,
    selfCharId, targetCharId, isRaped, pregnantRemainTime
);
```

这是 GM/调试倾向入口，会直接进入角色领域逻辑并可能改动关系、性行为和怀孕状态。生产 MOD 中应谨慎使用，调用前应确认双方角色存在、性别/年龄/关系/怀孕锁定状态符合预期。

### 查询怀孕锁定结束日期

同步通知式调用:

```csharp
CharacterDomainMethod.Call.GmCmd_GetCharacterPregnancyLockEndDates(
    int listenerId,
    int charId
);
```

异步调用:

```csharp
CharacterDomainMethod.AsyncCall.GmCmd_GetCharacterPregnancyLockEndDates(
    IAsyncMethodRequestHandler requestHandler,
    int charId,
    AsyncMethodCallbackDelegate callback
);
```

源码包装:

```csharp
GameDataBridge.AddMethodCall(listenerId, 4, 78, charId);

int requestId = SingletonObject.getInstance<AsyncMethodDispatcher>()
    .AsyncMethodCall(4, 78, charId, callback);
requestHandler?.RegisterAsyncMethodCall(requestId);
```

该方法用于查询怀孕锁定结束日期，不负责初始化怀孕状态。

## 过月事件

定义位置:

- `GameData.Shared_e85ad006/GameData/Domains/World/MonthlyEvent/MonthlyEventCollection.cs`
- `GameData.Shared_e85ad006/Config/MonthlyEvent.cs`

这些方法向月度事件集合追加记录，参数通过 `AppendCharacter`、`AppendLocation` 等写入记录体。

| 配置 ID | 配置名 | Collection 方法 | 参数 |
|---:|---|---|---|
| 11 | `GiveBirthToCricketTaiwu` | `AddGiveBirthToCricketTaiwu` | `int charId` |
| 12 | `GiveBirthToCricketWife` | `AddGiveBirthToCricketWife` | `int charId` |
| 13 | `PrenatalEducationTaiwu` | `AddPrenatalEducationTaiwu` | `int charId` |
| 14 | `AbortionTaiwu` | `AddAbortionTaiwu` | `int charId, Location location` |
| 158 | `PregnancyWithLover` | `AddPregnancyWithLover` | `int charId, int charId1` |
| 291 | `Pregnant` | `AddPregnant` | `int charId, Location location` |

示例:

```csharp
monthlyEvents.AddPregnancyWithLover(motherCharId, loverCharId);
monthlyEvents.AddPregnant(motherCharId, location);
```

这些 API 用于追加过月事件显示，不应当被当作 `PregnantStates` 初始化 API。

## 月度通知

定义位置:

- `GameData.Shared_e85ad006/GameData/Domains/World/Notification/MonthlyNotificationCollection.cs`
- `GameData.Shared_e85ad006/Config/MonthlyNotification.cs`

| 配置 ID | 配置名 | Collection 方法 | 参数 |
|---:|---|---|---|
| 40 | `MotherGiveBirthToBoy` | `AddMotherGiveBirthToBoy` | `int charId, Location location` |
| 41 | `MotherGiveBirthToGirl` | `AddMotherGiveBirthToGirl` | `int charId, Location location` |
| 42 | `FatherGetBoy` | `AddFatherGetBoy` | `int charId` |
| 43 | `FatherGetGirl` | `AddFatherGetGirl` | `int charId` |
| 44 | `GiveBirthToCricket` | `AddGiveBirthToCricket` | `int charId` |
| 45 | `MotherLoseFetus` | `AddMotherLoseFetus` | `int charId, Location location` |

月度通知用于给前端展示本月发生的结果。它记录“已经发生的生产/流产/得子”事件，不负责生成孩子或维护怀孕状态。

## 人生经历

定义位置: `GameData.Shared_e85ad006/Config/LifeRecord.cs`

| ID | 配置名 | 说明 |
|---:|---|---|
| 7 | `GiveBirthToCricket` | 生下促织相关经历 |
| 8 | `GiveBirthToBoy` | 生下男孩经历 |
| 9 | `GiveBirthToGirl` | 生下女孩经历 |
| 545 | `PregnancyWithWife` | 与妻子怀孕经历 |
| 546 | `PregnancyWithHusband` | 与丈夫怀孕经历 |
| 607 | `TaiwuReincarnationPregnancy` | 太吾转世怀孕经历 |

人生经历配置是记录模板。它本身不表示角色当前处于怀孕状态。

## 密闻

定义位置:

- `GameData.Shared_e85ad006/GameData/Domains/Information/Collection/SecretInformationCollection.cs`
- `GameData.Shared_e85ad006/Config/SecretInformation.cs`

| 配置 ID | 配置名 | Collection 方法 | 参数 |
|---:|---|---|---|
| 21 | `GiveBirthToChild` | `AddGiveBirthToChild` | `int charId, int charId1` |
| 22 | `GiveBirthToChild2` | `AddGiveBirthToChild2` | `int charId, int charId1, int charId2` |
| 108 | `GiveBirthToChildFatherUnknown` | `AddGiveBirthToChildFatherUnknown` | `int charId, int charId1, int charId2` |

这些方法会返回新增密闻记录 ID:

```csharp
int infoId = secretInformation.AddGiveBirthToChild(motherCharId, childCharId);
```

密闻用于情报和传播系统，不是生育流程的核心状态源。

## 配置常量

| 类型 | 常量 | ID | 说明 |
|---|---|---:|---|
| `CharacterFeature` | `Pregnant` | 197 | 角色怀孕特性 |
| `MonthlyEvent` | `GiveBirthToCricketTaiwu` | 11 | 太吾生促织月度事件 |
| `MonthlyEvent` | `GiveBirthToCricketWife` | 12 | 配偶生促织月度事件 |
| `MonthlyEvent` | `PrenatalEducationTaiwu` | 13 | 太吾胎教月度事件 |
| `MonthlyEvent` | `AbortionTaiwu` | 14 | 太吾流产月度事件 |
| `MonthlyEvent` | `PregnancyWithLover` | 158 | 与爱侣怀孕月度事件 |
| `MonthlyEvent` | `Pregnant` | 291 | 怀孕月度事件 |
| `MonthlyNotification` | `MotherGiveBirthToBoy` | 40 | 母亲生男孩通知 |
| `MonthlyNotification` | `MotherGiveBirthToGirl` | 41 | 母亲生女孩通知 |
| `MonthlyNotification` | `GiveBirthToCricket` | 44 | 生促织通知 |
| `MonthlyNotification` | `MotherLoseFetus` | 45 | 母亲流产通知 |
| `LifeRecord` | `GiveBirthToBoy` | 8 | 生男孩经历 |
| `LifeRecord` | `GiveBirthToGirl` | 9 | 生女孩经历 |
| `LifeRecord` | `PregnancyWithWife` | 545 | 与妻子怀孕经历 |
| `LifeRecord` | `PregnancyWithHusband` | 546 | 与丈夫怀孕经历 |
| `LifeRecord` | `TaiwuReincarnationPregnancy` | 607 | 太吾转世怀孕经历 |
| `SecretInformation` | `GiveBirthToChild` | 21 | 生子密闻 |
| `SecretInformation` | `GiveBirthToChild2` | 22 | 生子密闻变体 |
| `SecretInformation` | `GiveBirthToChildFatherUnknown` | 108 | 父亲未知生子密闻 |

## 开发注意事项

### 不要把事件记录当作状态初始化

以下调用只追加显示或记录数据：

```csharp
monthlyEvents.AddPregnant(charId, location);
monthlyNotifications.AddMotherGiveBirthToBoy(charId, location);
secretInformation.AddGiveBirthToChild(motherCharId, childCharId);
```

它们不会保证 `CharacterDomain.PregnantStates` 中存在 `charId` 对应的怀孕状态。

### `CharacterFeature.Pregnant` 与 `PregnantStates` 不是同一个东西

怀孕相关逻辑至少涉及两层数据：

| 数据 | 含义 | 风险 |
|---|---|---|
| `CharacterFeature.Pregnant` | 角色身上的怀孕特性，配置 ID 为 197 | 只补特性可能仍缺状态数据 |
| `PregnantStates` | 角色域怀孕状态集合，数据 ID 为 7 | 缺 key 时访问 `GetElement_PregnantStates(charId)` 可能抛 `KeyNotFoundException` |

如果修复逻辑写成：

```csharp
if (_pregnantStates.ContainsKey(charId) && !hasPregnantFeature)
{
    AddPregnantFeature(charId);
}
```

那么不在 `_pregnantStates` 字典中的角色会被跳过，怀孕状态不会初始化。后续过月流程仍可能处理这些角色，并在访问 `_pregnantStates[charId]` 时崩溃。

### 直接修改怀孕状态要保守

没有完整确认后端 `CharacterDomain` 内部实现时，不建议手工拼写或伪造 `PregnantState`。更稳妥的方向是：

1. 优先使用游戏已有领域方法触发完整流程。
2. 需要补丁修复时，同时校验 `PregnantStates`、`PregnancyLockEndDates` 和 `CharacterFeature.Pregnant` 的一致性。
3. 对 `GetElement_PregnantStates` 等危险访问点加前置检查或 Finalizer 兜底，但不要简单返回 `null` 后继续让原流程访问空对象。
4. 每次修改后用 `Player.log` 与 `GameData_*.log` 对齐验证过月流程是否还出现 `KeyNotFoundException`。

## 源码索引

| 内容 | 源码路径 |
|---|---|
| 角色域数据和方法 ID | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/Character/CharacterDomainHelper.cs` |
| 角色域调用包装 | `spied/The Scroll of Taiwu/TaiwuDecompiler_da99b9b8/GameData/Domains/Character/CharacterDomainMethod.cs` |
| 过月事件集合 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/World/MonthlyEvent/MonthlyEventCollection.cs` |
| 过月事件配置 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/Config/MonthlyEvent.cs` |
| 月度通知集合 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/World/Notification/MonthlyNotificationCollection.cs` |
| 月度通知配置 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/Config/MonthlyNotification.cs` |
| 人生经历配置 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/Config/LifeRecord.cs` |
| 密闻集合 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/Information/Collection/SecretInformationCollection.cs` |
| 密闻配置 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/Config/SecretInformation.cs` |
| 怀孕特性配置 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/Config/CharacterFeature.cs` |

