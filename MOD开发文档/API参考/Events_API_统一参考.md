# Events API 统一参考

**最后更新**: 2026-06-26  
**状态**: 当前源码确认 + `AutoMonthlyEvent` 实测链路合并版  
**适用范围**: 月度事件、普通事件窗口、前端自动选择、后端月度事件集合

本文合并月度事件、事件窗口、请求事件入口、胎教/生育事件相关 API。重点是给自动过月处理器提供可靠、可追溯的接口边界。

## 1. 可靠性分层

| 层级 | 入口 | 推荐用途 | 风险 |
|---|---|---|---|
| 前端事件窗口 | `EventModel.DisplayingEventData` + `EventModel.Select(optionKey)` | 自动选择、记录、调试 | 低到中 |
| 前端额外输入 | `SetInputResult`、`SetSelectItemResult`、`SetCharacterSelectResult` | 取名、物品/人物选择 | 中，需要检查 ExtraData |
| 后端月度集合 | `MonthlyEventCollection.Add...` | 事件生成识别、后端截获 | 中，需要逐项验证调用方 |
| 事件领域方法 | `TaiwuEventDomainMethod.Call.*` | 选择事件、设置事件参数 | 中高，错误会推进事件 |
| 结果记录集合 | `SecretInformationCollection.AddAccept/Refuse...` | 验证结算记录 | 中，不是事件生成入口 |
| 配置表 | `Config.DemandInteraction` | 请求交互配置映射 | 低，但不要和月度 templateId 混用 |

## 2. 源码定位

| 目标 | 路径 |
|---|---|
| 前端事件模型 | `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/EventModel.cs` |
| 事件领域方法 | `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/GameData/Domains/TaiwuEvent/TaiwuEventDomainMethod.cs` |
| 事件显示数据 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/TaiwuEvent/DisplayEvent/TaiwuEventDisplayData.cs` |
| 事件选项数据 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/TaiwuEvent/DisplayEvent/EventOptionInfo.cs` |
| 事件额外数据 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/TaiwuEvent/DisplayEvent/TaiwuEventDisplayExtraData.cs` |
| 月度事件集合 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/World/MonthlyEvent/MonthlyEventCollection.cs` |
| 请求交互配置 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/Config/DemandInteraction.cs` |
| 请求结果记录 | `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/Information/Collection/SecretInformationCollection.cs` |
| 当前自动执行器 | `mods/AutoMonthlyEvent.Executor/Plugins/AutoMonthlyEvent.Executor.Frontend` |

## 3. 前端事件窗口主链路

当前最稳定自动化路径：

```csharp
EventModel eventModel = SingletonObject.getInstance<EventModel>();
TaiwuEventDisplayData data = eventModel.DisplayingEventData;
eventModel.Select(optionKey);
```

源码确认：

```csharp
public void Select(string optionKey)
{
    if (DisplayingEventData != null
        && DisplayingEventData.EventOptionInfos.Exists(e => e.OptionKey == optionKey))
    {
        string eventGuid = DisplayingEventData.EventGuid;
        TaiwuEventDomainMethod.Call.EventSelect(eventGuid, optionKey);
        GEvent.OnEvent(UiEvents.EventWindowSelectOption, argumentBox);
    }
}
```

自动点击前必须检查：

- `DisplayingEventData != null`
- `EventGuid` 非空
- `EventOptionInfos` 包含目标 `OptionKey`
- 目标选项 `OptionState == 0`
- 当前窗口签名未变化
- 没有未处理的 `ExtraData` 输入/选择器，除非该功能明确支持

## 4. TaiwuEventDisplayData

源码字段：

| 字段 | 类型 | 用途 |
|---|---|---|
| `EventGuid` | `string` | 当前事件窗口 GUID |
| `MainCharacter` | `CharacterDisplayData` | 左/主角色 |
| `TargetCharacter` | `CharacterDisplayData` | 右/目标角色 |
| `ExtraData` | `TaiwuEventDisplayExtraData` | 物品/人物/输入等额外操作 |
| `EventContent` | `string` | 当前窗口文本 |
| `NameDecodeDataList` | `List<TaiwuEventCharacterNameDecodeData>` | 姓名解码数据 |
| `ExtraFormatLanguageKeys` | `List<string>` | 额外格式化语言键 |
| `EventTexture` | `string` | 事件图 |
| `MaskControlCode` | `sbyte` | 遮罩控制 |
| `MaskTweenTime` | `ushort` | 遮罩时间 |
| `EscOptionIndex` | `sbyte` | ESC 选项索引 |
| `EventOptionInfos` | `List<EventOptionInfo>` | 当前可见选项 |

推荐事件签名：

```text
eventGuid + optionKeys + mainCharacterId + targetCharacterId + stableHash(eventContent)
```

宽松签名可用于玩家自定义对话复用：

```text
eventGuid + optionKeys
```

宽松签名存在多个不同目标选项时必须视为冲突并跳过。

## 5. EventOptionInfo

源码字段：

| 字段 | 类型 | 用途 |
|---|---|---|
| `OptionType` | `sbyte` | 选项类型 |
| `OptionKey` | `string` | 提交给 `EventSelect` 的 key |
| `OptionContent` | `string` | 选项文本 |
| `OptionAvailableConditions` | `List<OptionAvailableInfo>` | 可用条件 |
| `OptionAvailableConditionInfos` | `List<OptionAvailableConditionInfo>` | 可用条件展示 |
| `OptionConsumeInfos` | `List<OptionConsumeInfo>` | 消耗信息 |
| `OptionState` | `sbyte` | 选项状态；当前自动化只点击 `0` |
| `Behavior` | `sbyte` | 行为/立场显示语义，可辅助判断 |
| `ExtraFormatLanguageKeys` | `List<string>` | 额外格式化语言键 |

当前 Executor 规则：

- `OptionState == 0` 是唯一可点击状态。
- `OptionContent` 可作为语义兜底，但不能单独决定高风险事件。
- 对玩家自定义跳过，可不要求白名单，但仍要检查目标选项存在且可用。

## 6. TaiwuEventDisplayExtraData

关键字段：

| 字段 | 用途 |
|---|---|
| `SelectItemData` | 事件内物品选择 |
| `SelectCharacterData` | 事件内人物选择 |
| `SelectReadingBookCountData` | 读书数量选择 |
| `SelectNeigongLoopingCountData` | 内功周天数量选择 |
| `SelectFuyuFaithCountData` | 福缘/信仰数量选择 |
| `SelectFameData` | 名声选择 |
| `InputRequestData` | 输入框，例如新生儿取名 |
| `ActorData/LeftActorData` | 事件角色动作数据 |
| `SelectOneAvatarRelatedDataList` | 头像/形象选择 |

安全规则：

- “所有单选项跳过”必须在这些额外操作为空时才执行。
- 取名类事件可以支持 `InputRequestData`，但要先确认 `InputDataType` 和目标事件。
- 物品/人物选择必须走专门处理，不应被普通 `Select(optionKey)` 代替。

## 7. 事件领域方法

常用源码确认签名：

```csharp
TaiwuEventDomainMethod.Call.EventSelect(string eventGuid, string optionKey)
TaiwuEventDomainMethod.Call.EventSelect(string eventGuid, string optionKey, bool isContinue)
TaiwuEventDomainMethod.Call.EventSelectContinue()

TaiwuEventDomainMethod.Call.SetListenerEventActionIntArg(string actionName, string key, int value)
TaiwuEventDomainMethod.Call.SetListenerEventActionStringArg(string actionName, string key, string value)
TaiwuEventDomainMethod.Call.SetCharacterSelectResult(string key, int charId, bool callComplete)
TaiwuEventDomainMethod.Call.SetCharacterMultSelectResult(string key, List<int> charIds, bool callComplete)
TaiwuEventDomainMethod.Call.GetValidInteractionEventOptions(int listenerId, int targetCharId)
```

`EventModel.SetInputResult(string inputResult)` 已确认会：

- `InputDataType == 1`：尝试按数字范围写入 `SetListenerEventActionIntArg("InputActionComplete", key, value)`。
- `InputDataType == 3`：先调用 `ProfessionModel.HandleSetGiveNameResult(...)`，再写入 `SetListenerEventActionStringArg("InputActionComplete", key, inputResult)`。

因此新生儿取名自动化应优先复用 `EventModel.SetInputResult`，不要直接拼底层参数。

## 8. 月度事件集合：生育/成长段

源码模式：

```csharp
int beginOffset = BeginAddingRecord(templateId);
AppendCharacter(...);
AppendLocation(...);
AppendItemKey(...);
AppendInteger(...);
EndAddingRecord(beginOffset);
```

已确认 templateId：

| templateId | 方法 | 参数 |
|---:|---|---|
| 11 | `AddGiveBirthToCricketTaiwu` | `charId` |
| 12 | `AddGiveBirthToCricketWife` | `charId` |
| 13 | `AddPrenatalEducationTaiwu` | `charId` |
| 14 | `AddAbortionTaiwu` | `charId, location` |
| 15 | `AddLoseFetusWife` | `charId, location` |
| 16 | `AddMotherFetusBothDieTaiwu` | `charId, location` |
| 17 | `AddMotherFetusBothDieWife` | `charId, location` |
| 18 | `AddDystociaLoseFetusTaiwu` | `charId, location` |
| 19 | `AddDystociaLoseFetusWife` | `charId, location` |
| 20 | `AddHaveChildBoyTaiwu` | `charId, location, childCharId` |
| 21 | `AddHaveChildGirlTaiwu` | `charId, location, childCharId` |
| 22 | `AddHaveChildBoyWife` | `charId, location, childCharId` |
| 23 | `AddHaveChildGirlWife` | `charId, location, childCharId` |
| 24-31 | `AddDystocia...HaveChild...` | `charId, location, childCharId` |
| 32 | `AddAbandonedBabyInVilliage` | `charId` |
| 33 | `AddChildZhuazhou` | `charId` |
| 34 | `AddTeachChild` | `charId` |

当前 Executor 对应关系：

- 胎教：前端按 GUID 选择 1/2/3，再可选退出结果。
- 弃婴收养：前端根据 `CharacterDisplayData.BehaviorType` 判断。
- 喜得贵子/千金：前端按事件 GUID 选择姓氏/亲自取名，再用 `SetInputResult` 填名。

## 9. 月度请求事件 66-90

请求系列方向按当前用户确认模型记录为：`NPC -> 太吾`。`charId` 是请求者，`charId1` 是被请求者，通常应为太吾。

| templateId | 方法 | 参数追加顺序 | 当前策略 |
|---:|---|---|---|
| 66 | `AddRequestHealOuterInjuryByItem` | `charId, location, charId1, itemKey, bodyPartType` | 可处理 |
| 67 | `AddRequestHealOuterInjuryByResource` | `charId, location, charId1, value` | 可处理 |
| 68 | `AddRequestHealInnerInjuryByItem` | `charId, location, charId1, itemKey, bodyPartType` | 可处理 |
| 69 | `AddRequestHealInnerInjuryByResource` | `charId, location, charId1, value` | 可处理 |
| 70 | `AddRequestHealPoisonByItem` | `charId, location, charId1, itemKey, poisonType` | 可处理 |
| 71 | `AddRequestHealPoisonByResource` | `charId, location, charId1, value, poisonType` | 可处理 |
| 72 | `AddRequestHealth` | `charId, location, charId1, itemKey` | 可处理 |
| 73 | `AddRequestHealDisorderOfQi` | `charId, location, charId1, itemKey` | 可处理 |
| 74 | `AddRequestNeili` | `charId, location, charId1, itemKey` | 可处理 |
| 75 | `AddRequestKillWug` | `charId, location, charId1, itemKey, itemKey1` | 可处理 |
| 76 | `AddRequestFood` | `charId, location, charId1, itemKey, value` | 可处理 |
| 77 | `AddRequestTeaWine` | `charId, location, charId1, itemKey` | 可处理 |
| 78 | `AddRequestResource` | `charId, location, charId1, value, resourceType` | 可处理 |
| 79 | `AddRequestItem` | `charId, location, charId1, itemKey, value` | 可处理 |
| 80 | `AddRequestRepairItem` | `charId, location, charId1, itemKey, itemKey1, value, resourceType` | 可处理 |
| 81 | `AddRequestAddPoisonToItem` | `charId, location, charId1, itemKey, itemKey1` | 可处理 |
| 82 | `AddRequestInstructionOnLifeSkill` | `charId, location, charId1, itemType, itemTemplateId, value` | 可处理 |
| 83 | `AddRequestInstructionOnCombatSkill` | `charId, location, charId1, itemType, itemTemplateId, value, value1, value2` | 可处理 |
| 84 | `AddRequestInstructionOnReadingLifeSkill` | `charId, location, charId1, itemKey, value` | 可处理 |
| 85 | `AddRequestInstructionOnReadingCombatSkill` | `charId, location, charId1, itemKey, value, value1` | 可处理 |
| 86 | `AddRequestInstructionOnBreakout` | `charId, location, charId1, combatSkillTemplateId` | 可处理 |
| 87 | `AddRequestPlayCombat` | `charId, location, charId1` | 默认不处理 |
| 88 | `AddRequestNormalCombat` | `charId, location, charId1` | 默认不处理 |
| 89 | `AddRequestLifeSkillBattle` | `charId, location, charId1` | 默认不处理 |
| 90 | `AddRequestCricketBattle` | `charId, location, charId1` | 默认不处理 |

自动处理建议：

- 66-86 可按关系/好感/配置筛选。
- 87-90 属于胜负/风险类，必须另建规则：实力、技艺类型、收益损失、是否允许失败。

## 10. DemandInteraction 配置表

`DemandInteraction` 是请求交互配置，不是月度事件 templateId。

已确认：

| DemandInteractionId | 名称 |
|---:|---|
| 0 | `RequestHealOuterInjuryByItem` |
| 1 | `RequestHealInnerInjuryByItem` |
| 2 | `RequestHealPoisonByItem` |
| 3 | `RequestHealth` |
| 4 | `RequestHealDisorderOfQi` |
| 5 | `RequestNeili` |
| 6 | `RequestKillWug` |
| 7 | `RequestFood` |
| 8 | `RequestTeaWine` |
| 9 | `RequestResource` |
| 10 | `RequestItem` |
| 11 | `RequestRepairItem` |
| 12 | `RequestAddPoisonToItem` |
| 13 | `RequestInstructionOnReadingLifeSkill` |
| 14 | `RequestInstructionOnBreakout` |
| 15 | `RequestInstructionOnReadingCombatSkill` |

字段：

- `TemplateId`
- `Name`
- `HeadEvent`
- `AgreeSelect`
- `AfterAgree`

不要把 `DemandInteractionId 0-15` 和 `MonthlyEvent templateId 66-86` 混用。

## 11. SecretInformationCollection 结果记录

该层记录接受/拒绝结果，不是请求生成入口。

已确认方法组：

```csharp
AddAcceptRequestHealInjury(...)
AddAcceptRequestDetoxPoison(...)
AddAcceptRequestIncreaseHealth(...)
AddAcceptRequestRestoreDisorderOfQi(...)
AddAcceptRequestIncreaseNeili(...)
AddAcceptRequestKillWug(...)
AddAcceptRequestFood(...)
AddAcceptRequestTeaWine(...)
AddAcceptRequestResource(...)
AddAcceptRequestItem(...)
AddAcceptRequestInstructionOnReading(...)
AddAcceptRequestInstructionOnBreakout(...)
AddAcceptRequestRepairItem(...)
AddAcceptRequestAddPoisonToItem(...)
AddAcceptRequestInstructionOnLifeSkill(...)
AddAcceptRequestInstructionOnCombatSkill(...)

AddRefuseRequest...
```

用途：

- 可用于确认“接受/拒绝”后端结算是否被记录。
- 不建议作为自动处理入口。

## 12. 当前 Executor 推荐策略

### 稳定默认层

- 事件窗口出现后读 `TaiwuEventDisplayData`。
- 分类命中后检查 `EventOptionInfo.OptionState == 0`。
- 请求类按关系/好感筛选。
- 结果类只处理明确白名单结果。
- 玩家自定义跳过允许宽松匹配，但目标选项不可用时阻止。

### 高风险可选层

- `EnableAnySingleOptionContinue`：所有单选项跳过。
- 生育/取名：只对已知 GUID + 输入类型生效。
- 后端截获：只作为实验项，不默认启用。

### 不处理层

- 奇遇、主线、剧情事件。
- 切磋、挑战、较艺、比武、促织战斗。
- 存在未支持 `ExtraData` 的事件。
- 事件签名变化或目标选项不可用。

## 13. 当前待补充

- 胎教后端选项效果入口仍未完全定位，当前前端处理更可靠。
- 87-90 胜负类请求需要实力与风险 API 后再设计。
- `optionKey` 稳定性应继续通过 Discovery 日志验证。
- 普通 normal event 可以做玩家自定义跳过，但默认不应全局自动处理。
