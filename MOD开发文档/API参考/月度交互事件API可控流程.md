# 月度交互事件 API 可控流程

本文记录太吾 MOD 开发中“过月 / 月度通知 / 月度交互事件”的可控 API。重点不是穷举所有事件内容，而是明确哪些入口安全、哪些入口只是 UI 包装、哪些规则会阻止“全部默认 / 全部无视”，以及如何从配置追踪到具体事件。

## 术语

- 月度通知：过月后展示的普通消息列表，偏展示和分类。
- 月度交互事件：过月后必须或可选择处理的事件列表，点击后会进入事件窗口或执行后端逻辑。
- 默认处理：游戏 UI 中的 `DefaultAll`，对应后端 `ProcessAllMonthlyEventsWithDefaultOption()`。
- 不可默认处理：`MonthlyEventItem.Type > 0` 的事件，UI 会禁止 `DefaultAll`。
- 事件窗口：完整事件交互 UI，由 `EventModel` / `UI_EventWindow` 驱动，选项通过 `TaiwuEventDomainMethod.Call.EventSelect(...)` 回后端。

## 总流程

过月进入月度通知阶段的主链路：

1. `BasicGameData.OnAdvancingMonthState()` 监听过月状态。
2. 当 `AdvancingMonthState == 20` 时，前端显示 `UIElement.MonthNotify`。
3. `UI_MonthNotify.OnInit()` 请求月度事件集合。
4. `WorldDomainMethod.Call.GetMonthlyEventCollection(...)` 从后端取 `MonthlyEventCollection`。
5. `UI_MonthNotify.UpdateMonthlyEventCollection(...)` 将集合转换为 `MonthlyEventRenderInfo`。
6. UI 根据 `MonthlyEventItem.Type` 决定是否允许 `DefaultAll`。
7. 玩家点击单条事件时调用 `WorldDomainMethod.Call.HandleMonthlyEvent(offset)`。
8. 所有月度交互事件处理完后调用 `WorldDomainMethod.Call.AdvanceMonth_DisplayedMonthlyNotifications(saveWorld)`。
9. 关闭月度通知时触发 `UiEvents.MonthNotifyProcessComplete`、`TaiwuEventDomainMethod.Call.TriggerListener("MonthNotifyShowed", true)` 和 `TaiwuEventDomainMethod.Call.OnNewGameMonth()`。

## 前端入口

### 过月状态打开月度通知

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/BasicGameData.cs:545`

关键逻辑：

```csharp
if (AdvancingMonthState == 20)
{
    UIManager.Instance.HideUI(UIElement.MonthNotify);
    UIElement.MonthNotify.SetOnInitArgs(EasyPool.Get<ArgumentBox>().Set("NeedSave", flag));
    UI_MonthNotify.NewMonthEventSend = false;
    UIManager.Instance.ShowUI(UIElement.MonthNotify);
    if (!flag)
        WorldDomainMethod.Call.AdvanceMonth_DisplayedMonthlyNotifications(saveWorld: false);
}
```

说明：

- `AdvancingMonthState == 20` 是月度通知 UI 出现的关键阶段。
- `NeedSave` 决定是否需要在月度通知处理完后保存世界。
- 如果无需保存，原版会立即调用 `AdvanceMonth_DisplayedMonthlyNotifications(false)`。

### UI_MonthNotify 初始化

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:264`

关键逻辑：

```csharp
WorldDomainMethod.Call.RemoveAllInvalidMonthlyEvents();
WorldDomainMethod.Call.GetMonthlyEventCollection(Element.GameDataListenerId);
```

说明：

- 打开月度通知时先清理无效月度事件。
- 然后请求 `MonthlyEventCollection`。
- `GetMonthlyEventCollection` 是有返回值的异步请求，可被前端监听。

### 接收月度事件集合

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:346`
- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:889`

关键逻辑：

```csharp
if (notification.DomainId == 1 && notification.MethodId == 5)
{
    MonthlyEventCollection item = null;
    Serializer.Deserialize(notification2.DataPool, notification.ValueOffset, ref item);
    if (BasicGameData.AdvancingMonthState != 0)
        UpdateMonthlyEventCollection(item);
}
```

```csharp
monthlyEventCollection.GetRenderInfos(_monthlyEventRenderInfoList, _monthlyEventArgumentCollection);
LifeRecordDomainMethod.Call.GetRecordRenderInfoArguments(
    Element.GameDataListenerId,
    "UI_MonthNotify_MonthlyEvent",
    new RecordArgumentsRequest(_monthlyEventArgumentCollection));
```

说明：

- `DomainId == 1` 是 World 域。
- `MethodId == 5` 是 `GetMonthlyEventCollection`。
- UI 不直接显示 raw record，而是转成 `MonthlyEventRenderInfo`。
- 文本参数需要再走 `LifeRecordDomainMethod.Call.GetRecordRenderInfoArguments(...)` 渲染。

## 后端调用入口

### WorldDomainMethod

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/GameData/Domains/World/WorldDomainMethod.cs:34`
- `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/World/WorldDomainHelper.cs:80`

| 方法 | MethodId | 用途 | 返回值 | 推荐调用 |
|---|---:|---|---|---|
| `HandleMonthlyEvent(int offset)` | 4 | 处理单条月度交互事件 | 无 | `WorldDomainMethod.Call.HandleMonthlyEvent(offset)` |
| `GetMonthlyEventCollection(int listenerId)` | 5 | 请求当前月度事件集合 | 有 | `WorldDomainMethod.Call.*` 或 `AsyncCall.*` |
| `RemoveAllInvalidMonthlyEvents()` | 6 | 清理无效月度事件 | 无 | `WorldDomainMethod.Call.RemoveAllInvalidMonthlyEvents()` |
| `ProcessAllMonthlyEventsWithDefaultOption()` | 7 | 全部默认处理可跳过事件 | 无 | `WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption()` |
| `AdvanceMonth()` | 10 | 推进过月 | 无 | 谨慎使用 |
| `AdvanceMonth_DisplayedMonthlyNotifications(bool saveWorld)` | 11 | 通知后端月度通知已展示完成 | 无 | 只在月度通知流程末尾使用 |
| `GmCmd_AddMonthlyEvent(...)` | 21 | GM 添加月度事件 | 有，返回 `bool` | 仅调试 / 测试存档 |

注意：

- 无返回值方法在 `AsyncCall` 中被标记为 obsolete 且直接抛 `NotSupportedException`。
- `HandleMonthlyEvent`、`RemoveAllInvalidMonthlyEvents`、`ProcessAllMonthlyEventsWithDefaultOption`、`AdvanceMonth_DisplayedMonthlyNotifications` 必须使用 `Call.*`。
- `GetMonthlyEventCollection` 和 `GmCmd_AddMonthlyEvent` 有返回值，可以使用 `AsyncCall.*`。

### GM 添加月度事件

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/GMFunc.cs:2672`

关键逻辑：

```csharp
WorldDomainMethod.AsyncCall.GmCmd_AddMonthlyEvent(
    null,
    templateId,
    endTemplateId,
    charId,
    targetCharId,
    callback);
```

参数：

- `templateId`：起始月度事件模板 ID。
- `endTemplateId`：结束月度事件模板 ID。
- `charId`：事件相关角色。
- `targetCharId`：目标角色。

返回：

- `bool`，成功时为 `true`。

风险：

- GM 方法依赖后端配置和代码同时存在。
- 不适合作为正式 MOD 的普通玩家入口。
- 适合调试“某个模板是否能进入月度事件队列”。

## 全部默认 / 全部无视规则

### DefaultAll 按钮

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:307`

关键逻辑：

```csharp
if ("DefaultAll" == name)
{
    btn.interactable = false;
    if (!HasForbidDefaultEvent())
    {
        WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption();
        _monthlyEventRenderInfoList.Clear();
        RefreshMonthlyEventScroll();
    }
}
```

说明：

- `DefaultAll` 不会逐个调用 `HandleMonthlyEvent(offset)`。
- 它直接调用后端的 `ProcessAllMonthlyEventsWithDefaultOption()`。
- 调用后 UI 直接清空本地 `_monthlyEventRenderInfoList` 并刷新。

### 禁止全部默认的判定

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:1036`

关键逻辑：

```csharp
foreach (MonthlyEventRenderInfo info in _monthlyEventRenderInfoList)
{
    MonthlyEventItem item = MonthlyEvent.Instance.GetItem(info.RecordType);
    if ((int)item.Type > 0)
        return true;
}
return false;
```

规则：

- `MonthlyEventItem.Type == NormalEvent`：可被 `DefaultAll` 默认处理。
- `MonthlyEventItem.Type == SpecialEvent`：禁止 `DefaultAll`。
- `MonthlyEventItem.Type == LockedEvent`：禁止 `DefaultAll`。
- 只要列表里存在一个 `Type > 0` 的事件，整个 `DefaultAll` 按钮不可用。

### 不可跳过标记

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:550`

关键逻辑：

```csharp
CannotSkipMark.SetActive((int)item.Type == 1);
```

说明：

- UI 明确显示不可跳过标记的是 `SpecialEvent`。
- `LockedEvent` 同样会让 `DefaultAll` 禁用，因为 `Type > 0`，但该标记逻辑只检查 `Type == 1`。

### 排序规则

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:934`

关键逻辑：

```csharp
if (item.Type != item2.Type)
    return item2.Type - item.Type;
if (left.RecordType != right.RecordType)
    return right.RecordType.CompareTo(left.RecordType);
return right.Offset.CompareTo(left.Offset);
```

排序优先级：

1. `Type` 高的排前面。
2. `RecordType` 高的排前面。
3. `Offset` 高的排前面。

## 单条月度事件处理

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:550`

关键逻辑：

```csharp
MonthlyEventRenderInfo info = _monthlyEventRenderInfoList[index];
WorldDomainMethod.Call.HandleMonthlyEvent(info.Offset);
```

安全规则：

- 只能传 `MonthlyEventRenderInfo.Offset`。
- 不要自己构造 offset。
- 不要在前端绕过 UI 直接解析 raw data 后调用其他事件方法。
- 点击前 UI 会 `QuickHide()`，避免月度通知窗口和事件窗口冲突。

### 处理完成回调

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:571`

关键逻辑：

```csharp
if (!HandlingMonthlyEventBlock)
{
    if (UIManager.Instance.IsFocusElement(UIElement.MainMenu))
    {
        QuickHide();
        GEvent.Remove(UiEvents.OnHandlingMonthlyEventBlockChange, OnFinishHandlingMonthlyEvent);
    }
    else
    {
        Element.SetOnInitArgs(EasyPool.Get<ArgumentBox>().Set("NeedSave", true));
        UIManager.Instance.ShowUI(Element);
    }
}
```

说明：

- 后端处理事件期间会通过 `OnHandlingMonthlyEventBlock` 阻塞继续流程。
- 当阻塞解除后，UI 重新打开 `MonthNotify`，继续处理剩余事件。

## 月度通知结束

### UI 关闭条件

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:237`

关键逻辑：

```csharp
if (_monthlyEventRenderInfoList == null || _monthlyEventRenderInfoList.Count <= 0)
{
    GEvent.OnEvent(UiEvents.MonthNotifyProcessComplete);
    TaiwuEventDomainMethod.Call.TriggerListener("MonthNotifyShowed", value: true);
    if (!NewMonthEventSend)
    {
        TaiwuEventDomainMethod.Call.OnNewGameMonth();
        NewMonthEventSend = true;
    }
    base.QuickHide();
}
```

说明：

- `MonthNotifyProcessComplete` 是前端事件信号，不是后端推进月份的主入口。
- 事件列表为空时才允许真正关闭。
- 关闭时会触发 `MonthNotifyShowed` listener。
- 每次月度通知只发送一次 `OnNewGameMonth()`。

### 后端完成通知

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UI_MonthNotify.cs:934`

关键逻辑：

```csharp
if (_monthlyEventRenderInfoList.Count <= 0 && _needSave)
{
    WorldDomainMethod.Call.AdvanceMonth_DisplayedMonthlyNotifications(saveWorld: true);
    _needSave = false;
}
```

说明：

- 这是月度通知流程完成后的后端推进入口。
- `saveWorld: true` 会进入保存世界流程。
- `saveWorld: false` 用于不保存或 GM 禁用自动保存路径。

## 事件窗口交互 API

### 事件选项

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/EventModel.cs:100`
- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/GameData/Domains/TaiwuEvent/TaiwuEventDomainMethod.cs:74`

关键逻辑：

```csharp
if (DisplayingEventData != null &&
    DisplayingEventData.EventOptionInfos.Exists(e => e.OptionKey == optionKey))
{
    TaiwuEventDomainMethod.Call.EventSelect(DisplayingEventData.EventGuid, optionKey);
}
```

安全规则：

- 前端必须先确认当前 `DisplayingEventData` 存在。
- 只能选择当前事件提供的 `OptionKey`。
- 不要猜测 option key。
- 可监听 `UiEvents.EventWindowSelectOption` 做前端联动。

### 显示事件数据

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/EventModel.cs:763`
- `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/TaiwuEvent/TaiwuEventDomainHelper.cs:51`

关键点：

- `TaiwuEventDomainHelper.DataIds.DisplayingEventData = 21`
- 前端通过通知反序列化 `TaiwuEventDisplayData`。
- 如果 `EventGuid` 为空，前端会把 `DisplayingEventData` 置空。

## TaiwuEventDomain 相关方法

来源：

- `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/TaiwuEvent/TaiwuEventDomainHelper.cs:68`
- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/GameData/Domains/TaiwuEvent/TaiwuEventDomainMethod.cs:69`

重点方法：

| 方法 | MethodId | 用途 | 安全等级 |
|---|---:|---|---|
| `TriggerListener(string key, bool value)` | 2 | 触发事件监听器 | B |
| `StartHandleEventDuringAdvance()` | 7 | 过月中开始处理事件 | C，需确认时机 |
| `SetEventInProcessing(string eventGuid)` | 9 | 设置处理中事件 | C |
| `EventSelect(string eventGuid, string optionKey)` | 10 | 选择事件选项 | A，必须来自当前显示事件 |
| `GetEventDisplayData(int listenerId)` | 11 | 请求事件显示数据 | B |
| `GmCmd_SaveMonthlyActionManager()` | 12 | 保存月度 Action Manager | D，仅 GM/诊断 |
| `OnCharacterClicked(int charId)` | 13 | 事件中点击角色 | B |
| `OnNewGameMonth()` | 19 | 新月事件触发 | C，原版关闭月度通知时调用 |

说明：

- `MonthlyEventActionManager` 在 `DataIds` 中存在，ID 为 `1`。
- 当前源码树未找到 `AddTempDynamicAction`、`AddWrappedConfigAction`、`HandleMonthlyActions` 的可直接调用实现。
- 不应仅凭文档截图直接调用这些方法；需要找到真实类型、程序集和签名后再使用。

## 月度事件配置

### MonthlyEventItem

来源：

- `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/Config/MonthlyEventItem.cs:8`

字段：

| 字段 | 含义 |
|---|---|
| `TemplateId` | 月度事件模板 ID |
| `Name` | 本地化名称 |
| `Type` | 事件类型，决定是否允许默认处理 |
| `Event` | 对应 TaiwuEvent GUID |
| `Icon` | 图标 |
| `Desc` | 描述模板 |
| `Parameters` | 渲染参数类型 |
| `MergeableParameters` | 可合并参数 |
| `Score` | 排序/权重相关分值 |
| `Node` | 节点标记 |

### EMonthlyEventType

来源：

- `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/EMonthlyEventType.cs:1`

枚举：

```csharp
Invalid = -1
NormalEvent = 0
SpecialEvent = 1
LockedEvent = 2
Count = 3
```

控制规则：

- `NormalEvent`：允许被 `DefaultAll` 默认处理。
- `SpecialEvent`：禁止 `DefaultAll`，UI 显示 `CannotSkipMark`。
- `LockedEvent`：禁止 `DefaultAll`，但 `CannotSkipMark` 逻辑未直接检查它。

## MonthlyEventCollection

来源：

- `spied/The Scroll of Taiwu/GameData.Shared_e85ad006/GameData/Domains/World/MonthlyEvent/MonthlyEventCollection.cs:12`

核心读取方法：

| 方法 | 用途 |
|---|---|
| `GetRenderInfos(List<MonthlyEventRenderInfo>, ArgumentCollection)` | 将 raw records 转成 UI 可渲染列表 |
| `GetRenderInfo(int offset, ArgumentCollection)` | 读取单条记录并生成渲染信息 |
| `GetRecordType(int offset)` | 获取记录模板 ID |
| `FillEventArgBox(int offset, IVariantCollection<string>)` | 将 offset 对应记录参数填入事件参数盒 |

核心添加方法：

- 文件中存在大量 `AddXxx(...)` 方法，每个方法负责按对应 `MonthlyEventItem.Parameters` 写入参数。
- 这些方法大多是后端领域逻辑内部使用，不建议前端或普通 MOD 直接反射调用。
- 调试添加事件优先使用 `GmCmd_AddMonthlyEvent(...)`。

### 事件参数追踪

`MonthlyEventCollection.GetRenderInfo()` 会：

1. 从 raw data 中读出 `recordType`。
2. 取 `Config.MonthlyEvent.Instance[recordType]`。
3. 遍历 `MonthlyEventItem.Parameters`。
4. 按 `ParameterType.Parse(...)` 从 raw record 中读取参数。
5. 生成 `MonthlyEventRenderInfo`。

因此追踪一个事件时，应按这个顺序：

1. 找 `MonthlyEventItem.TemplateId`。
2. 查 `MonthlyEventItem.Event`，确认对应 TaiwuEvent GUID。
3. 查 `MonthlyEventItem.Type`，判断是否可默认处理。
4. 查 `MonthlyEventItem.Parameters`，确认需要哪些参数。
5. 在 `MonthlyEventCollection` 中搜索对应 `Add...` 方法，确认参数写入顺序。

## MOD 可控使用建议

### 只观察月度通知完成

前端可监听：

```csharp
GEvent.Add(UiEvents.MonthNotifyProcessComplete, handler);
```

适合：

- 月度通知窗口关闭后刷新 MOD UI。
- 不修改游戏月度事件队列。

不适合：

- 替代后端过月完成逻辑。
- 主动推进月份。

### 处理单条事件

只在已有 `MonthlyEventRenderInfo` 时调用：

```csharp
WorldDomainMethod.Call.HandleMonthlyEvent(info.Offset);
```

不要：

- 自己构造 offset。
- 缓存旧 offset 到下个月使用。
- 在 `AdvancingMonthState == 0` 时处理旧集合。

### 全部默认处理

调用前必须复刻原版检查：

```csharp
bool forbid = monthlyEventRenderInfos.Any(info =>
    MonthlyEvent.Instance.GetItem(info.RecordType).Type > 0);
if (!forbid)
    WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption();
```

不要绕过 `Type > 0` 检查，否则可能跳过特殊事件或锁定事件。

### 完成月度通知

只有当月度事件列表为空时调用：

```csharp
WorldDomainMethod.Call.AdvanceMonth_DisplayedMonthlyNotifications(saveWorld);
```

不要：

- 在还有 `SpecialEvent` / `LockedEvent` 时调用。
- 在非过月阶段调用。
- 在 UI 仍在处理 `OnHandlingMonthlyEventBlock` 时调用。

### MOD 后端通知前端

如果只是想在过月相关逻辑里让前端弹出 MOD 自定义 UI，不要塞进原版月度事件系统。优先使用：

后端：

```csharp
DomainManager.Mod.AddModDisplayEvent(modId, customData);
```

前端：

```csharp
ModManager.RegisterModDisplayEventHandler(modId, handler);
```

来源：

- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/DisplayEventHandler.cs:622`
- `spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/ModManager.cs:1718`

## 安全等级

| API | 等级 | 理由 |
|---|---|---|
| `WorldDomainMethod.Call.GetMonthlyEventCollection` | A | 原版 UI 入口 |
| `WorldDomainMethod.Call.HandleMonthlyEvent` | A | 原版单条处理入口 |
| `WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption` | B | 原版入口，但必须先检查 `Type > 0` |
| `WorldDomainMethod.Call.AdvanceMonth_DisplayedMonthlyNotifications` | B | 原版完成入口，但时机敏感 |
| `WorldDomainMethod.Call.RemoveAllInvalidMonthlyEvents` | B | 原版 UI 初始化会调用 |
| `WorldDomainMethod.AsyncCall.GmCmd_AddMonthlyEvent` | C | GM/调试用途 |
| `TaiwuEventDomainMethod.Call.EventSelect` | A | 原版事件窗口选项入口 |
| `TaiwuEventDomainMethod.Call.OnNewGameMonth` | C | 原版关闭月度通知时调用，不建议随意触发 |
| `TaiwuEventDomainMethod.Call.GmCmd_SaveMonthlyActionManager` | D | GM/诊断用途 |
| 反射调用 `MonthlyEventCollection.Add...` | D | 参数和时机复杂，容易破坏队列 |
| 直接修改 `MonthlyEventActionManager` 数据 | D | 当前未确认真实实现和不变量 |

## 排查模板

遇到月度交互问题时按以下顺序：

1. 看 `Player.log` 是否在 `UI_MonthNotify`、`EventModel`、`UI_EventWindow` 报错。
2. 看 `GameData_*.log` 是否在 `WorldDomain`、`TaiwuEventDomain` 报错。
3. 确认当前 `AdvancingMonthState`。
4. 确认是否已经调用 `GetMonthlyEventCollection`。
5. 记录 `MonthlyEventRenderInfo.Count`。
6. 对每条事件记录 `Offset`、`RecordType`、`MonthlyEventItem.Type`、`Event GUID`。
7. 如果 `DefaultAll` 不可用，检查是否存在 `Type > 0`。
8. 单条处理只使用 `HandleMonthlyEvent(info.Offset)`。
9. 事件窗口选择只使用当前 `DisplayingEventData.EventOptionInfos` 中存在的 `OptionKey`。
10. 所有事件处理完后再调用 `AdvanceMonth_DisplayedMonthlyNotifications(saveWorld)`。
11. 如果只是 MOD 自定义提示，优先用 `ModDisplayEvent`，不要伪造原版月度事件。

## 关键源码索引

- `BasicGameData.OnAdvancingMonthState`：过月状态 20 打开月度通知。
- `UI_MonthNotify.OnInit`：清理无效事件并请求月度事件集合。
- `UI_MonthNotify.OnNotifyGameData`：接收 `MonthlyEventCollection`。
- `UI_MonthNotify.OnClick`：`DefaultAll` 和关闭按钮逻辑。
- `UI_MonthNotify.OnMonthlyEventItemRender`：单条事件点击处理。
- `UI_MonthNotify.HasForbidDefaultEvent`：全部默认禁止规则。
- `UI_MonthNotify.RefreshMonthlyEventScroll`：排序、按钮状态和完成推进。
- `WorldDomainMethod`：前端到后端月度方法包装。
- `WorldDomainHelper.MethodIds`：World 域方法 ID。
- `MonthlyEventCollection`：月度事件 raw record 集合。
- `MonthlyEventItem` / `EMonthlyEventType`：事件配置和类型规则。
- `EventModel.Select`：完整事件窗口选项选择。
- `TaiwuEventDomainMethod`：TaiwuEvent 域方法包装。
- `ModManager.RegisterModDisplayEventHandler` / `DomainManager.Mod.AddModDisplayEvent`：MOD 后端到前端通道。
