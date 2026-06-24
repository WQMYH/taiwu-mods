# 可靠 API 查找步骤与方法

本文总结太吾 MOD 开发中查找、确认和使用 API 的可靠流程。内容结合了建筑 API 与月度交互事件 API 的排查经验，目标是避免“凭方法名猜测”“只看截图文档调用”“绕过原版流程直接改数据”等高风险做法。

适用对象：

- 后端 Domain API，例如 `BuildingDomain`、`WorldDomain`、`TaiwuEventDomain`、`ModDomain`。
- 前端 UI API，例如 `ViewBuildingArea`、`UI_MonthNotify`、`EventModel`、`UI_EventWindow`。
- 配置数据 API，例如 `Config.MonthlyEvent`、`BuildingBlock`、`MapBlockItem`。
- MOD 前后端通信 API，例如 `CallModMethodWithParamAndRet`、`ModDisplayEvent`。
- 反射或 Harmony patch 场景。

## 一、核心原则

1. 先找真实调用链，再决定 API。
2. 先读原版实现，再看其他 MOD 用法。
3. 先做只读诊断，再做写入或 patch。
4. 区分前端 UI、后端数据、配置表和 MOD 通信，不混用入口。
5. 所有 API 都要记录参数、返回值、前置条件、副作用和失败模式。
6. 没找到真实源码实现的 API，不视为可靠 API。
7. GM 方法、私有字段、反射方法、Harmony patch 都要降级评估。

## 二、API 来源分级

查到 API 后，先标注来源。来源不同，可信度不同。

| 来源 | 可信度 | 说明 |
|---|---:|---|
| 原版源码实际调用点 | 高 | 最可靠，可确认调用时机、参数和副作用 |
| 原版 DomainMethod 包装类 | 高 | 可确认 DomainId、MethodId、前后端调用方式 |
| 原版 Helper / MethodIds / DataIds | 高 | 可确认编号和数据字段，但不等于行为实现 |
| 原版配置类 | 中高 | 可确认模板字段和枚举规则 |
| 其他 MOD 稳定用法 | 中 | 需要继续反查原版是否支持 |
| 游戏编辑器导出模板 | 中低 | 可能是生成代码模板，不一定是运行时入口 |
| 文档截图 / 手工笔记 | 中低 | 只能作为线索，不能直接调用 |
| 反射发现的私有字段 / 方法 | 低 | 必须验证签名、时机和版本兼容 |
| 猜测的方法名 | 不可信 | 未找到真实定义前不能作为 API |

示例：

- `WorldDomainMethod.Call.HandleMonthlyEvent(int offset)`：原版 UI 实际调用，可信。
- `MonthlyEventActionManager.AddTempDynamicAction<T>()`：当前只从截图看到，未找到真实实现，不可直接调用。
- `BuildingDomain.GetBuildingOperationLeftTime(...)`：原版后端方法，能 patch，但必须先确认异常前置条件。

## 三、标准查找流程

### Step 1：从目标或错误出发

先明确你要解决的是哪一类问题：

- 查询数据：只读。
- 修改数据：有副作用。
- 触发事件：可能改变流程状态。
- 刷新 UI：前端缓存和时序敏感。
- 前后端通信：需要确认 ModId、方法名和序列化格式。
- patch 原版方法：最高风险。

如果来自报错，先记录：

```text
异常类型:
最早异常时间:
前端日志 Player.log 行号:
后端日志 GameData_*.log 行号:
第一个游戏源码方法:
调用栈上层入口:
发生时机:
```

判断方向：

- `Game.Views.*`、`UI_*`、`EventModel`：优先查前端。
- `GameData.Domains.*`：优先查后端。
- `Config.*`：优先查配置表和参数。
- `DisplayEventHandler`、`ModManager`：优先查前后端通信。

### Step 2：用精确关键词定位源码

优先使用 CodeGraph：

```text
codegraph explore "目标方法名 相关类名 业务关键词"
codegraph node 目标类名
codegraph callers 目标方法名
```

没有 CodeGraph 时，用 `rg`：

```powershell
rg "HandleMonthlyEvent|GetMonthlyEventCollection|ProcessAllMonthlyEventsWithDefaultOption" -n
rg "TryGetElement_BuildingBlocks|SetElement_BuildingBlocks|GmCmd_RemoveBuildingImmediately" -n
```

关键词建议同时覆盖：

- 方法名。
- 类名。
- 枚举名。
- 字段名。
- UI 按钮名。
- 日志里的函数名。
- MethodId / DataId 对应名称。

### Step 3：先找原版调用点

不要只看 API 声明，必须找谁在调用它。

需要确认：

```text
调用方:
调用时机:
调用前检查:
调用后刷新:
是否在前端:
是否在后端:
是否有返回值:
失败后如何处理:
```

例子：月度全部默认处理。

原版 UI 调用点：

```csharp
if (!HasForbidDefaultEvent())
{
    WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption();
    _monthlyEventRenderInfoList.Clear();
    RefreshMonthlyEventScroll();
}
```

因此不能只知道 `ProcessAllMonthlyEventsWithDefaultOption()`，还必须记录前置条件 `!HasForbidDefaultEvent()`。

### Step 4：查 DomainMethod 包装类

对于前端调用后端的 API，必须查包装类。

记录格式：

```text
包装类:
Call 方法:
AsyncCall 方法:
DomainId:
MethodId:
是否有返回值:
推荐调用方式:
```

判断规则：

- 无返回值方法通常使用 `Call.*`。
- 有返回值方法通常可以使用 `AsyncCall.*`。
- 如果 `AsyncCall` 标记 obsolete 并抛 `NotSupportedException`，说明不能异步调用。

例子：

```csharp
WorldDomainMethod.Call.HandleMonthlyEvent(int offset)
```

是无返回值方法，`AsyncCall.HandleMonthlyEvent(...)` 被标记 obsolete，因此只能用 `Call.*`。

### Step 5：查 Helper / MethodIds / DataIds

Helper 文件用于确认编号和数据字段。

记录：

```text
Domain:
DataId:
MethodId:
FieldName2DataId:
MethodName2MethodId:
```

例子：

```text
WorldDomain.MethodIds.HandleMonthlyEvent = 4
WorldDomain.MethodIds.GetMonthlyEventCollection = 5
WorldDomain.MethodIds.ProcessAllMonthlyEventsWithDefaultOption = 7
TaiwuEventDomain.DataIds.MonthlyEventActionManager = 1
```

注意：

- `DataId` 存在不代表可以直接修改对应对象。
- `MethodId` 存在不代表该方法适合 MOD 调用。
- Helper 只能证明编号，不能证明业务安全性。

### Step 6：查数据结构和配置

所有涉及模板、类型、参数的 API，都必须查配置类。

记录：

```text
配置类:
模板 ID 字段:
类型枚举:
参数字段:
描述字段:
事件 GUID 字段:
影响流程的字段:
```

例子：月度事件配置。

```csharp
MonthlyEventItem.TemplateId
MonthlyEventItem.Type
MonthlyEventItem.Event
MonthlyEventItem.Parameters
```

其中 `Type` 决定是否允许全部默认处理：

```text
NormalEvent = 0，可默认处理
SpecialEvent = 1，禁止全部默认
LockedEvent = 2，禁止全部默认
```

例子：建筑格子数据。

```text
TemplateId > 0: 真实建筑
TemplateId == 0: 空地
TemplateId == -1: 多格建筑附属格
RootBlockIndex: 附属格指向根建筑
```

### Step 7：确认前置条件

可靠 API 文档必须写清楚前置条件。

常见前置条件：

- 当前是否在过月阶段。
- 当前 UI 是否打开。
- 当前事件数据是否存在。
- 当前格子 key 是否存在。
- 配置表是否能取到模板。
- 参数列表是否和配置 `Parameters` 一致。
- 是否需要完整网格。
- 是否需要 listenerId。
- 是否需要当前显示事件的 `EventGuid`。

例子：

```csharp
EventModel.Select(optionKey)
```

前置条件：

- `DisplayingEventData != null`
- `DisplayingEventData.EventOptionInfos` 包含该 `optionKey`

否则不应调用 `TaiwuEventDomainMethod.Call.EventSelect(...)`。

### Step 8：确认副作用

API 不只是“能不能调用”，还要知道调用后会发生什么。

记录：

```text
会修改哪些数据:
会触发哪些通知:
会刷新哪些 UI:
会不会保存:
会不会进入事件窗口:
会不会阻塞流程:
会不会清理缓存:
```

例子：

```csharp
WorldDomainMethod.Call.HandleMonthlyEvent(info.Offset)
```

副作用：

- 处理指定 offset 的月度事件。
- 可能进入事件窗口。
- 可能设置 `OnHandlingMonthlyEventBlock`。
- UI 会在处理完成后重新打开月度通知。

例子：

```csharp
SetElement_BuildingBlocks(...)
```

副作用：

- 写入建筑格子数据。
- 不一定自动清理工匠订单、自动工作列表、操作人员字典。
- 写完后可能需要调用建筑效果刷新。

### Step 9：确认失败模式

记录 API 可能失败的方式。

常见失败模式：

- null 数据。
- key 不存在。
- 配置不存在。
- MethodId 对不上。
- 前端调用后端 ModId 错误。
- UI 仍持有旧缓存。
- offset 过期。
- 私有字段名变化。
- 操作状态引用旧角色或旧建筑。
- 默认处理跳过了特殊事件。

例子：

建筑导入后崩溃：

```text
GetBuildingOperationLeftTime 访问空地或非法操作状态
```

实际需要检查：

- `TemplateId <= 0`
- `OperationType` 是否非法
- `BuildingBlock.Instance[TemplateId]` 是否存在
- `BuildingOperatorDict` 是否有对应 key

例子：

月度事件全部默认：

```text
如果存在 Type > 0 的事件，原版 UI 会禁止 DefaultAll
```

绕过这个检查可能导致特殊事件被错误跳过。

## 四、API 记录标准格式

每确认一个 API，都按以下格式记录。

```text
API 名称:
完整类型:
所在文件:
源码位置:
前端/后端:
来源等级:

调用方式:
DomainId:
MethodId:
DataId:

参数:
返回值:
是否可 AsyncCall:
推荐调用:

前置条件:
副作用:
失败模式:
安全等级:

原版调用点:
相关配置:
相关 UI:
验证方法:
备注:
```

### 示例：月度单条事件处理

```text
API 名称: HandleMonthlyEvent
完整类型: GameData.Domains.World.WorldDomainMethod.Call
所在文件: GameData/Domains/World/WorldDomainMethod.cs
前端/后端: 前端调用后端
来源等级: 原版 UI 实际调用

调用方式: WorldDomainMethod.Call.HandleMonthlyEvent(info.Offset)
DomainId: 1
MethodId: 4

参数:
- offset: MonthlyEventRenderInfo.Offset

返回值: 无
是否可 AsyncCall: 否
推荐调用: Call

前置条件:
- offset 来自当前月度事件集合
- 当前处于月度通知处理阶段

副作用:
- 处理该月度事件
- 可能打开事件窗口
- 可能阻塞月度通知流程

失败模式:
- offset 过期
- 事件集合已刷新
- 当前不在过月阶段

安全等级: A
```

### 示例：建筑格子读取

```text
API 名称: TryGetElement_BuildingBlocks
完整类型: GameData.Domains.Building.BuildingDomain
前端/后端: 后端
来源等级: 原版领域数据访问

参数:
- BuildingBlockKey

返回值:
- bool
- out BuildingBlockData

推荐调用:
- 读取时优先使用 TryGetElement_BuildingBlocks

前置条件:
- key 的 AreaId / BlockId / BuildingBlockIndex 合法

副作用:
- 无

失败模式:
- key 不存在
- blockData 为 null

安全等级: A
```

## 五、安全等级定义

| 等级 | 含义 | 使用建议 |
|---|---|---|
| A | 原版公开或包装 API，有明确调用点和前置条件 | 可用于正式功能 |
| B | 原版内部流程 API，有明确调用点但时机敏感 | 可用，但必须复刻原版前置检查 |
| C | GM、调试、私有反射或依赖当前版本结构 | 只用于诊断或受控功能 |
| D | 直接改内部集合、伪造 offset、吞异常、绕过流程 | 不建议作为长期方案 |
| X | 未找到真实实现，只有截图或猜测 | 禁止调用 |

示例：

```text
WorldDomainMethod.Call.GetMonthlyEventCollection: A
WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption: B
WorldDomainMethod.AsyncCall.GmCmd_AddMonthlyEvent: C
反射 MonthlyEventCollection.AddXxx: D
MonthlyEventActionsManager.AddTempDynamicAction: X，当前未找到真实实现
```

## 六、前端 API 查找方法

前端 API 重点看 UI 生命周期、事件监听、数据通知和缓存。

查找顺序：

1. 找 UI 类，例如 `UI_MonthNotify`、`ViewBuildingArea`。
2. 找 `OnInit`、`OnEnable`、`OnDisable`、`QuickHide`。
3. 找按钮回调，例如 `OnClick`、`ClearAndAddListener`。
4. 找 `OnNotifyGameData`。
5. 找 `GEvent.Add` / `GEvent.OnEvent`。
6. 找 `UIManager.Instance.ShowUI/HideUI`。
7. 找前端到后端的 `DomainMethod.Call.*`。

前端记录重点：

```text
UI 何时打开:
UI 初始化参数:
监听哪些通知:
点击按钮调用什么:
关闭 UI 前检查什么:
是否持有缓存:
是否需要延迟刷新:
```

建筑 UI 示例：

- `ViewBuildingArea.CreateBlock` 报 `Queue empty` 时，源头是前端对象池不足。
- 不能先改后端建筑数据。
- 应查 `ViewBuildingArea.Awake` 是否被扩建 MOD patch，以及对象池数量是否匹配实际宽度。

月度 UI 示例：

- `UI_MonthNotify.DefaultAll` 会先调用 `HasForbidDefaultEvent()`。
- 如果存在 `MonthlyEventItem.Type > 0`，全部默认按钮不可用。

## 七、后端 API 查找方法

后端 API 重点看 Domain、数据结构、副作用和通知。

查找顺序：

1. 找 `DomainMethod` 包装类。
2. 找 `DomainHelper.MethodIds`。
3. 找真实 `Domain` 实现或反编译逻辑。
4. 找数据类。
5. 找配置类。
6. 找原版 GM 函数是否调用。
7. 找其他 MOD 是否稳定使用。

后端记录重点：

```text
是否有返回值:
是否需要 DataContext:
是否会写存档:
是否会发 DisplayEvent:
是否会修改私有集合:
是否会触发领域通知:
是否需要保存:
```

建筑后端示例：

- 导入建筑不能只写 `BuildingBlockData`。
- 还要处理工匠订单、资源输出、自动工作列表、操作人员字典、建筑效果刷新。
- 宽度转换必须同步 `BlockIndex` 和 `RootBlockIndex`。

月度后端示例：

- `AdvanceMonth_DisplayedMonthlyNotifications(saveWorld)` 是月度通知展示完成后的推进 API。
- 不能在还有未处理特殊事件时调用。

## 八、配置 API 查找方法

配置类决定很多流程规则。

查找顺序：

1. 找 `Config.XxxItem`。
2. 找字段定义。
3. 找枚举类型。
4. 找 `Config.Xxx` 的静态项。
5. 找使用字段的 UI 或 Domain。

记录格式：

```text
配置项:
模板 ID:
核心字段:
枚举:
字段影响:
使用位置:
```

月度事件示例：

```text
MonthlyEventItem.Type 控制是否允许 DefaultAll。
MonthlyEventItem.Event 指向 TaiwuEvent GUID。
MonthlyEventItem.Parameters 控制 raw record 参数读取顺序。
```

建筑配置示例：

```text
BuildingBlockItem.OperationTotalProgress 控制 OperationType 的合法范围。
BuildingBlockItem.NeedLeader 控制是否需要设置建筑负责人。
MapBlockItem.BuildingAreaWidth 可作为扩建宽度来源之一。
```

## 九、MOD 前后端通信 API 查找方法

常见通道：

| 通道 | 方向 | 用途 |
|---|---|---|
| `DomainManager.Mod.AddModMethod` | 前端到后端 | 前端调用 MOD 后端方法 |
| `CallModMethodWithParamAndRet` | 前端到后端 | 带参数和返回值调用 |
| `DomainManager.Mod.AddModDisplayEvent` | 后端到前端 | 后端通知前端 |
| `ModManager.RegisterModDisplayEventHandler` | 前端注册 | 接收后端显示事件 |

查找重点：

```text
ModId 是否一致:
方法名是否一致:
参数 key 是否一致:
返回值 key 是否一致:
是否需要 SerializableModData:
是否需要 RawDataPool 反序列化:
```

示例：

```csharp
DomainManager.Mod.AddModDisplayEvent(modId, customData);
ModManager.RegisterModDisplayEventHandler(modId, handler);
```

适合：

- 过月时后端通知 MOD 前端显示自定义 UI。
- 不适合伪装成原版月度事件。

## 十、反射与 Harmony Patch 方法

只有在公开 API 不够用时才使用。

使用前检查：

```text
目标类型是否存在:
目标方法是否存在:
签名是否匹配:
字段是否存在:
是否前端/后端程序集一致:
失败时是否降级:
是否记录日志:
```

Patch 原则：

- prefix 只拦截明确危险输入。
- finalizer 只兜底，不能长期吞掉所有异常。
- 不改变无关逻辑。
- 不在 patch 中做复杂业务写入。
- Patch 必须有日志，确认是否安装成功。

建筑修复示例：

```text
GetBuildingOperationLeftTime 对 TemplateId <= 0、非法 OperationType、缺失配置等情况返回 -1。
```

这属于防御性 patch，而不是替代建筑数据修复。

## 十一、验证流程

### 只读验证

第一次确认 API 时，先写只读日志。

记录：

```text
当前阶段:
当前 UI:
DomainId / MethodId:
关键数据数量:
关键模板 ID:
关键枚举值:
参数列表:
```

建筑示例：

```text
width
blocks count
TemplateId 分布
RootBlockIndex 是否有效
operator dict 数量
```

月度示例：

```text
AdvancingMonthState
MonthlyEventRenderInfo.Count
每条事件 Offset / RecordType / Type / Event GUID
是否存在 Type > 0
```

### 最小写入验证

写入或触发前：

1. 使用测试存档。
2. 只处理一个对象或一条事件。
3. 写入前后输出摘要。
4. 不同时修改多条链路。
5. 保留回滚文件。

### 日志验证

验证时同时看：

- `Player.log`
- `GameData_*.log`
- MOD 自己的日志

判断：

- 原错误是否消失。
- 是否出现新异常。
- 前后端日志时间是否对齐。
- 运行的 DLL 是否是刚编译版本。

## 十二、常见反例

### 反例 1：只看方法名就调用

错误：

```text
看到 AddTempDynamicAction<T>() 就直接写代码调用。
```

正确：

```text
先找到真实类型、程序集、方法签名、原版调用点。
找不到实现则标记为 X 级，不调用。
```

### 反例 2：绕过 UI 前置检查

错误：

```text
直接调用 ProcessAllMonthlyEventsWithDefaultOption()。
```

正确：

```text
先检查是否存在 MonthlyEventItem.Type > 0。
如果存在，不能全部默认处理。
```

### 反例 3：直接删除空地块

错误：

```text
看到 TemplateId == 0 触发查询异常，就删除空地块。
```

正确：

```text
保持完整网格，修复操作状态和危险查询防护。
原版 UI / 效果缓存可能依赖完整网格。
```

### 反例 4：伪造 offset

错误：

```text
自己计算 MonthlyEvent offset 后调用 HandleMonthlyEvent。
```

正确：

```text
offset 必须来自当前 MonthlyEventRenderInfo.Offset。
```

### 反例 5：只刷新当前 UI

错误：

```text
后端数据整体替换后，只刷新当前面板。
```

正确：

```text
确认 UI 是否持有旧缓存。
必要时退出重进或走原版完整刷新流程。
```

## 十三、API 文档编写模板

新增 API 文档建议使用以下结构：

```markdown
# API 名称

## 用途

## 来源
- 文件:
- 行号:
- 原版调用点:

## 调用方式
```csharp
// 示例
```

## 参数

## 返回值

## 前置条件

## 副作用

## 失败模式

## 安全等级

## 验证步骤

## 相关 API

## 注意事项
```

## 十四、完整查找清单

每次查找新 API，按以下清单执行：

1. 明确目标：查询、写入、触发、刷新、通信、patch。
2. 收集日志或需求上下文。
3. 判断前端还是后端。
4. 搜索原版调用点。
5. 搜索 DomainMethod 包装类。
6. 搜索 Helper 的 MethodId / DataId。
7. 搜索数据结构。
8. 搜索配置类和枚举。
9. 搜索其他 MOD 用法。
10. 记录前置条件。
11. 记录副作用。
12. 记录失败模式。
13. 给出安全等级。
14. 做只读验证。
15. 做最小写入或最小触发验证。
16. 写入 API 文档。
17. 后续实测后补充日志结论。

## 十五、关键词索引

### 建筑 API

```text
BuildingDomain
BuildingAreaData
BuildingBlockData
BuildingBlockKey
TryGetElement_BuildingBlocks
GetElement_BuildingBlocks
SetElement_BuildingBlocks
GmCmd_RemoveBuildingImmediately
UpdateTaiwuVillageBuildingEffect
GetBuildingOperationLeftTime
BuildingOperatorDict
RootBlockIndex
TemplateId
```

### 月度交互 API

```text
UI_MonthNotify
WorldDomainMethod
GetMonthlyEventCollection
HandleMonthlyEvent
ProcessAllMonthlyEventsWithDefaultOption
AdvanceMonth_DisplayedMonthlyNotifications
MonthlyEventCollection
MonthlyEventRenderInfo
MonthlyEventItem
EMonthlyEventType
HasForbidDefaultEvent
DefaultAll
CannotSkipMark
EventModel
EventSelect
```

### MOD 通信 API

```text
DomainManager.Mod.AddModMethod
CallModMethodWithParamAndRet
SerializableModData
DomainManager.Mod.AddModDisplayEvent
ModManager.RegisterModDisplayEventHandler
DisplayEventHandler
RawDataPool
Serializer.Deserialize
```

## 十六、结论

可靠 API 不是“能编译的方法名”，而是已经确认以下信息的调用点：

- 真实源码存在。
- 原版有调用路径。
- 参数来源明确。
- 前置条件明确。
- 副作用明确。
- 失败模式可预期。
- 有只读或最小触发验证。

未满足这些条件的 API，只能作为线索，不能直接进入正式 MOD 逻辑。
