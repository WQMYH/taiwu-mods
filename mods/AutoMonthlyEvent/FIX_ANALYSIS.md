# AutoMonthlyEvent MOD 代码修复分析报告

**生成时间**: 2026-06-23  
**项目**: AutoMonthlyEvent - 批量月度交互自动化MOD  
**版本**: 0.1.0  

---

## 📋 执行摘要

本次代码审查发现了 **5个必须修复的阻塞性问题** 和 **6个强烈建议的改进点**。核心问题是：

1. ✅ **Config.lua 格式已修复** - 添加 `return` 关键字
2. ❌ **基类使用正确** - `TaiwuRemakePlugin` 是正确的（不是 `IModPlugin`）
3. ❌ **配置管理分散** - ConfigManager.cs 缺少新配置项支持
4. ❌ **自动处理器缺失** - 删除了旧处理器但未创建新的
5. ⚠️ **架构不完整** - 只有数据采集，没有数据处理

---

## 🔍 详细问题分析

### 问题 1: Config.lua 格式问题 ✅ 已修复

**状态**: ✅ 已修复  
**严重程度**: 🔴 高  
**影响范围**: 配置加载失败

#### 问题描述
原始 Config.lua 是一个裸表（table），在 Lua 中作为配置文件使用时需要 `return` 语句。

**修复前**:
```lua
{
    EnableAutoProcess = false,
    DiscoveryMode = true,
    ...
}
```

**修复后**:
```lua
-- AutoMonthlyEvent 配置文件
return {
    EnableAutoProcess = false,
    DiscoveryMode = true,
    ...
}
```

#### 技术说明
虽然当前的 `DiscoveryDumper.LoadConfig()` 使用正则表达式解析，不依赖 Lua 引擎执行，但：
- 标准做法是使用 `return`
- 如果未来改用 NLua 等库解析，必须有 `return`
- 提高可读性和规范性

---

### 问题 2: 基类选择 ✅ 确认正确

**状态**: ✅ 无需修改  
**严重程度**: ℹ️ 信息  
**影响范围**: 无

#### 分析结果

通过检查 `CopyBuildingModernized.Another` 项目的实际代码，确认：

```csharp
// ✅ 正确的基类
public sealed class BackendEntry : TaiwuRemakePlugin
public sealed class FrontendEntry : TaiwuRemakePlugin
```

**不是** `IModPlugin`（那是占位符或旧版本接口）。

#### TaiwuRemakePlugin 来源

- **DLL**: `TaiwuModdingLib.dll`
- **位置**: `$(TaiwuGameDir)\The Scroll of Taiwu_Data\Managed\TaiwuModdingLib.dll`
- **命名空间**: `TaiwuModdingLib.Core.Plugin`
- **方法**:
  - `Initialize()` - 插件初始化
  - `Dispose()` - 插件卸载
  - `OnModSettingUpdate()` - 配置更新回调（可选）

#### 当前代码状态

✅ `BackendEntry.cs` 已正确使用 `TaiwuRemakePlugin`  
✅ `FrontendEntry.cs` 已正确使用 `TaiwuRemakePlugin`

**结论**: 这部分代码是正确的，不需要修改。

---

### 问题 3: ConfigManager.cs 配置项缺失 ❌ 需要修复

**状态**: ❌ 待修复  
**严重程度**: 🔴 高  
**影响范围**: 配置系统不一致

#### 问题描述

`ConfigManager.cs` 只处理旧的4个配置项：
- `EnableAutoProcess`
- `ShowConfirmation`
- `SkipSpecialEvents`
- `LogVerbose`

但 `Config.lua` 中新增了3个配置项：
- `DiscoveryMode` ✨ 新增
- `DumpToJson` ✨ 新增
- `DumpDirectory` ✨ 新增

这导致：
1. **配置逻辑分散** - `DiscoveryDumper` 自己解析配置，`ConfigManager` 也解析配置
2. **重复代码** - 两处都有正则解析逻辑
3. **维护困难** - 修改配置需要同步两个地方
4. **ConfigManager 变得无用** - 实际上没有被使用

#### 当前代码对比

**DiscoveryDumper.LoadConfig()** (第72-96行):
```csharp
public static void LoadConfig()
{
    string content = File.ReadAllText(ConfigFilePath);
    _discoveryMode = ReadBool(content, "DiscoveryMode", true);
    _dumpToJson = ReadBool(content, "DumpToJson", true);
    _logVerbose = ReadBool(content, "LogVerbose", false);
    _dumpDirectoryName = ReadString(content, "DumpDirectory", "Dump_out");
}
```

**ConfigManager.ParseConfig()** (第80-126行):
```csharp
private void ParseConfig(string content)
{
    // 只解析旧的4个配置项
    var match = Regex.Match(content, @"EnableAutoProcess\s*=\s*(true|false)");
    // ... 其他旧配置项
}
```

#### 修复方案

**方案 A: 统一使用 ConfigManager（推荐）**

1. 扩展 `ConfigManager` 添加新配置项：
```csharp
public class ConfigManager
{
    // 原有配置项
    public bool EnableAutoProcess { get; set; } = true;
    public bool ShowConfirmation { get; set; } = false;
    public bool SkipSpecialEvents { get; set; } = true;
    public bool LogVerbose { get; set; } = false;
    
    // 新增配置项
    public bool DiscoveryMode { get; set; } = true;
    public bool DumpToJson { get; set; } = true;
    public string DumpDirectory { get; set; } = "Dump_out";
}
```

2. 在 `ParseConfig()` 中添加解析逻辑：
```csharp
// 解析 DiscoveryMode
if (content.Contains("DiscoveryMode"))
{
    var match = Regex.Match(content, @"DiscoveryMode\s*=\s*(true|false)");
    if (match.Success)
        DiscoveryMode = match.Groups[1].Value == "true";
}

// 解析 DumpToJson
if (content.Contains("DumpToJson"))
{
    var match = Regex.Match(content, @"DumpToJson\s*=\s*(true|false)");
    if (match.Success)
        DumpToJson = match.Groups[1].Value == "true";
}

// 解析 DumpDirectory
if (content.Contains("DumpDirectory"))
{
    var match = Regex.Match(content, @"DumpDirectory\s*=\s*""([^""]*)""");
    if (match.Success)
        DumpDirectory = match.Groups[1].Value;
}
```

3. 在 `GenerateConfigContent()` 中输出新配置项：
```csharp
return $@"{{
    -- 启用自动处理月度事件
    EnableAutoProcess = {EnableAutoProcess.ToString().ToLower()},
    
    -- 只读发现模式
    DiscoveryMode = {DiscoveryMode.ToString().ToLower()},
    
    -- 导出到JSON
    DumpToJson = {DumpToJson.ToString().ToLower()},
    
    -- 输出目录
    DumpDirectory = ""{DumpDirectory}"",
    
    -- ... 其他配置项
}}
";
```

4. 修改 `DiscoveryDumper` 使用 `ConfigManager`：
```csharp
public static void LoadConfig()
{
    // 直接使用 ConfigManager
    var config = ConfigManager.Instance;
    _discoveryMode = config.DiscoveryMode;
    _dumpToJson = config.DumpToJson;
    _logVerbose = config.LogVerbose;
    _dumpDirectoryName = config.DumpDirectory;
}
```

**方案 B: 移除 ConfigManager（简化）**

如果 `ConfigManager` 只在 Backend 中使用，而 Backend 目前没有实际功能，可以考虑：
- 暂时移除 `ConfigManager.cs`
- 所有配置由 `DiscoveryDumper` 统一管理
- 后续实现自动处理器时再重新设计配置系统

**推荐**: 方案 A，保持架构完整性。

---

### 问题 4: 自动处理器缺失 ❌ 需要创建

**状态**: ❌ 待实现  
**严重程度**: 🔴 高  
**影响范围**: MOD 核心功能缺失

#### 问题描述

您删除了原来的 `AutoMonthlyEventProcessor.cs`（基于 Type 字段判断的版本），但没有创建新的处理器。

**当前状态**:
- ✅ 有数据采集功能（`DiscoveryDumper.cs`）
- ❌ 没有数据处理功能
- ❌ 无法根据采集到的 ID 进行自动处理

#### 需要实现的功能

新的处理器应该：

1. **读取白名单/黑名单配置**
   - 从导出的 JSON 文件中提取事件 ID
   - 或者从单独的配置文件读取

2. **智能判断是否自动处理**
   - 检查当前事件是否在白名单中
   - 检查是否有特殊事件需要跳过
   - 基于规则引擎判断

3. **执行自动处理**
   - 调用 `WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption()`
   - 记录日志
   - 防止重入

#### 建议的实现方案

**文件**: `Plugins/AutoMonthlyEvent.Backend/AutoMonthlyEventProcessor.cs`

**核心逻辑**:
```csharp
[HarmonyPatch]
public class AutoMonthlyEventProcessor
{
    private static bool _isProcessing = false;
    private static HashSet<int> _whitelistRecordTypes = new();
    private static HashSet<string> _blacklistEventGuids = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_MonthNotify), nameof(UI_MonthNotify.OnInit))]
    public static void OnInit_Postfix(UI_MonthNotify __instance, ArgumentBox argsBox)
    {
        if (!ConfigManager.Instance.EnableAutoProcess)
            return;
        
        if (_isProcessing)
            return;

        // 检查过月状态
        var basicGameData = SingletonObject.getInstance<BasicGameData>();
        if (basicGameData?.AdvancingMonthState != 20)
            return;

        if (basicGameData.SavingWorld)
            return;

        _isProcessing = true;
        RegisterListener(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UI_MonthNotify), nameof(UI_MonthNotify.OnNotifyGameData))]
    public static void OnNotifyGameData_Postfix(
        UI_MonthNotify __instance, 
        List<NotificationWrapper> notifications)
    {
        foreach (var wrapper in notifications)
        {
            var notification = wrapper.Notification;
            
            // 检查是否是月度事件集合返回
            if (notification.Type == 1 && 
                notification.DomainId == 1 && 
                notification.MethodId == 5)
            {
                MonthlyEventCollection? eventCollection = null;
                Serializer.Deserialize(
                    wrapper.DataPool, 
                    notification.ValueOffset, 
                    ref eventCollection);

                if (eventCollection != null)
                {
                    ProcessMonthlyEvents(__instance, eventCollection);
                }
            }
        }
    }

    private static void ProcessMonthlyEvents(
        UI_MonthNotify monthNotify, 
        MonthlyEventCollection eventCollection)
    {
        try
        {
            // 加载白名单（首次或配置变更时）
            LoadWhitelistIfNeeded();

            // 检查所有事件是否都在白名单中
            bool allAllowed = true;
            foreach (var eventInfo in eventCollection.Events)
            {
                if (!_whitelistRecordTypes.Contains(eventInfo.RecordType))
                {
                    allAllowed = false;
                    AdaptableLog.Info(
                        $"[AutoMonthlyEvent] Event {eventInfo.RecordType} not in whitelist, skipping.");
                    break;
                }
            }

            if (allAllowed && eventCollection.Events.Count > 0)
            {
                AdaptableLog.Info(
                    $"[AutoMonthlyEvent] Auto-processing {eventCollection.Events.Count} events.");
                
                WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption();
            }
            else
            {
                AdapableLog.Info(
                    "[AutoMonthlyEvent] Some events not allowed, skipping auto process.");
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static void LoadWhitelistIfNeeded()
    {
        // 从 JSON 文件或配置加载白名单
        // 实现略...
    }
}
```

#### 白名单管理策略

**方案 A: 手动编辑白名单文件**
```json
// whitelist.json
{
  "allowedRecordTypes": [1001, 1002, 1003],
  "blockedEventGuids": ["story_important_001"]
}
```

**方案 B: 基于规则自动生成**
```csharp
// 规则示例
- Type == NormalEvent AND Score < 10 → 加入白名单
- EventGuid 包含 "story_" → 加入黑名单
- Parameters 包含特定角色 → 加入黑名单
```

**方案 C: 混合模式（推荐）**
- 默认保守策略（Type 检查）
- 白名单覆盖（用户确认安全的事件）
- 黑名单排除（用户确认重要的事件）

---

### 问题 5: 架构不完整 ⚠️ 需要补充

**状态**: ⚠️ 部分完成  
**严重程度**: 🟡 中  
**影响范围**: MOD 功能不完整

#### 当前架构

```
AutoMonthlyEvent/
├── Plugins/
│   ├── AutoMonthlyEvent.Backend/
│   │   ├── BackendEntry.cs          ✅ 入口（正确）
│   │   ├── ConfigManager.cs         ⚠️ 配置项不全
│   │   └── AutoMonthlyEventProcessor.cs  ❌ 缺失
│   └── AutoMonthlyEvent.Frontend/
│       ├── FrontendEntry.cs         ✅ 入口（正确）
│       └── DiscoveryDumper.cs       ✅ 数据采集
└── Config.lua                       ✅ 已修复
```

#### 完整架构应该是

```
第一阶段：数据采集（已完成 90%）
├── DiscoveryDumper.cs
│   ├── ExportStaticCatalogs()      ✅ 导出静态目录
│   ├── DumpMonthlyEventCollection() ✅ 捕获运行时事件
│   └── DumpEventOptions()          ✅ 捕获事件选项
└── 输出文件
    ├── monthly_events_catalog.json
    ├── interaction_options_catalog.json
    ├── runtime_monthly_events.jsonl
    └── runtime_event_options.jsonl

第二阶段：数据分析（需要工具或手动）
├── 分析导出的 JSON 文件
├── 标记可安全自动处理的事件
└── 生成白名单配置

第三阶段：自动处理（缺失，需要实现）
├── AutoMonthlyEventProcessor.cs（新建）
│   ├── LoadWhitelist()             加载白名单
│   ├── CheckEventAllowed()         检查事件是否允许
│   └── ProcessEvents()             执行自动处理
└── 配置管理
    └── ConfigManager.cs（完善）
```

---

## 🛠️ 修复优先级和建议

### P0 - 立即修复（阻塞编译或运行）

1. ✅ **Config.lua 格式** - 已修复
2. ❌ **创建 AutoMonthlyEventProcessor.cs** - 核心功能缺失
   - 预计工作量: 2-3 小时
   - 风险: 低
   - 依赖: 需要先确定白名单策略

### P1 - 强烈建议（影响可维护性）

3. ❌ **完善 ConfigManager.cs**
   - 添加新配置项支持
   - 统一配置解析逻辑
   - 预计工作量: 1 小时
   - 风险: 低

4. ❌ **移除重复的配置解析代码**
   - 让 `DiscoveryDumper` 使用 `ConfigManager`
   - 预计工作量: 30 分钟
   - 风险: 低

### P2 - 建议改进（提升用户体验）

5. ⚠️ **添加游戏版本信息到导出文件**
   ```json
   {
     "gameVersion": "2024.XX",
     "modVersion": "0.1.0",
     "generatedAt": "..."
   }
   ```

6. ⚠️ **完善错误处理和日志分级**
   - 区分 Info/Warning/Error
   - 添加更多边界检查

7. ⚠️ **添加数据统计功能**
   - 记录发现了多少事件
   - 记录处理了多少事件
   - 生成统计报告

8. ⚠️ **提供白名单管理工具**
   - 简单的命令行工具
   - 或者图形界面（远期目标）

---

## 📊 修复工作量估算

| 任务 | 工作量 | 优先级 | 风险 |
|------|--------|--------|------|
| 创建 AutoMonthlyEventProcessor.cs | 2-3 小时 | P0 | 低 |
| 完善 ConfigManager.cs | 1 小时 | P1 | 低 |
| 统一配置解析逻辑 | 30 分钟 | P1 | 低 |
| 添加版本信息 | 15 分钟 | P2 | 极低 |
| 完善日志系统 | 30 分钟 | P2 | 低 |
| 添加数据统计 | 1 小时 | P2 | 低 |
| **总计** | **5-6 小时** | - | - |

---

## 🎯 推荐的实施顺序

### 第一步：实现核心功能（P0）

1. 创建 `AutoMonthlyEventProcessor.cs`
   - 实现基本的 Harmony Patch
   - 实现基于白名单的判断逻辑
   - 先使用硬编码的白名单测试

2. 测试基本流程
   - 编译项目
   - 部署到游戏
   - 验证是否能正常拦截和处理

### 第二步：完善配置系统（P1）

3. 扩展 `ConfigManager.cs`
   - 添加新配置项
   - 统一配置解析

4. 重构 `DiscoveryDumper.LoadConfig()`
   - 使用 `ConfigManager` 而非自己解析

### 第三步：优化和改进（P2）

5. 添加版本信息和统计
6. 完善日志和错误处理
7. 编写使用文档

---

## 💡 关键技术决策

### 决策 1: 白名单 vs 规则引擎

**选项 A: 纯白名单**
- ✅ 简单直接
- ✅ 完全可控
- ❌ 需要手动维护
- ❌ 新事件需要手动添加

**选项 B: 规则引擎**
- ✅ 自动化程度高
- ✅ 适应性强
- ❌ 实现复杂
- ❌ 可能误判

**选项 C: 混合模式（推荐）**
- ✅ 兼顾安全性和便利性
- ✅ 用户可以精细控制
- ⚠️ 需要设计良好的配置格式

**建议**: 采用混合模式，默认保守，允许用户自定义。

### 决策 2: 配置格式

**选项 A: 继续用 Lua**
- ✅ 与现有配置一致
- ✅ 玩家熟悉
- ❌ 不适合复杂结构（如列表）

**选项 B: 使用 JSON**
- ✅ 适合结构化数据
- ✅ 易于程序处理
- ❌ 玩家需要学习新格式

**选项 C: 混合（推荐）**
- `Config.lua` - 简单开关配置
- `whitelist.json` - 事件 ID 列表
- `blacklist.json` - 排除列表

**建议**: 采用混合方案，Lua 用于开关，JSON 用于列表。

---

## 📝 总结

### 当前进度

- ✅ **数据采集模块**: 完成度 90%
- ❌ **数据处理模块**: 完成度 0%
- ⚠️ **配置系统**: 完成度 50%

### 主要问题

1. ✅ Config.lua 格式 - 已修复
2. ✅ 基类选择 - 确认正确（TaiwuRemakePlugin）
3. ❌ 配置管理分散 - 需要统一
4. ❌ 自动处理器缺失 - 需要创建
5. ⚠️ 架构不完整 - 需要补充

### 下一步行动

1. **立即**: 创建 `AutoMonthlyEventProcessor.cs`
2. **随后**: 完善 `ConfigManager.cs`
3. **最后**: 优化和改进

### 预期成果

完成修复后，MOD 将具备：
- ✅ 动态事件 ID 采集能力
- ✅ 基于白名单的智能处理能力
- ✅ 灵活的配置系统
- ✅ 完整的文档和工具

---

**文档版本**: 1.0  
**最后更新**: 2026-06-23  
**作者**: AI Assistant  
**审核状态**: 待用户确认
