# Config.lua 开发规范

**最后更新**: 2026-06-24  
**适用版本**: 太吾绘卷 1.0.x  
**参考来源**: 成功 MOD 实例分析（居所仓库扩展、背包仓储数量最大加等）

---

## 📋 目录

1. [基本结构](#基本结构)
2. [必填字段](#必填字段)
3. [可选字段](#可选字段)
4. [DefaultSettings 配置项](#defaultsettings-配置项)
5. [支持的 SettingType](#支持的-settingtype)
6. [完整示例](#完整示例)
7. [常见错误](#常见错误)
8. [最佳实践](#最佳实践)

---

## 🏗️ 基本结构

Config.lua 必须使用 `return` 关键字返回一个 Lua 表：

```lua
return {
    -- MOD 元信息
    Title = "MOD名称",
    Author = "作者名",
    Version = "1.0.0.0",
    
    -- 插件配置
    BackendPlugins = {
        [1] = "YourMod.Backend.dll",
    },
    FrontendPlugins = {
        [1] = "YourMod.Frontend.dll",
    },
    
    -- 默认设置
    DefaultSettings = {
        -- 配置项...
    },
    
    -- 其他配置
    ChangeConfig = false,
    HasArchive = false,
}
```

---

## ✅ 必填字段

### 1. Title（标题）

**类型**: String  
**说明**: MOD 的显示名称

```lua
Title = "批量月度交互自动化"
```

### 2. Author（作者）

**类型**: String  
**说明**: MOD 作者名称

```lua
Author = "WQMYH"
```

### 3. Version（版本）

**类型**: String  
**格式**: `主版本.次版本.修订版.构建号`

```lua
Version = "0.1.0.0"
```

### 4. Description（描述）

**类型**: String  
**说明**: MOD 功能描述，支持 `\v` 换行

```lua
Description = "自动检测并批量化处理太吾绘卷中的月度交互事件。\v\v支持发现模式导出事件ID和选项。"
```

### 5. GameVersion（游戏版本）

**类型**: String  
**说明**: 兼容的游戏版本号

```lua
GameVersion = "1.0.24"
```

### 6. BackendPlugins（后端插件）

**类型**: Table  
**说明**: 后端 DLL 文件列表

```lua
BackendPlugins = {
    [1] = "AutoMonthlyEvent.Backend.dll",
}
```

### 7. FrontendPlugins（前端插件）

**类型**: Table  
**说明**: 前端 DLL 文件列表

```lua
FrontendPlugins = {
    [1] = "AutoMonthlyEvent.Frontend.dll",
}
```

---

## 🔧 可选字段

### Source（来源）

**类型**: Integer  
**值**: 
- `0` - 本地 MOD
- `1` - 创意工坊 MOD

```lua
Source = 0  -- 本地 MOD
```

### Visibility（可见性）

**类型**: Integer  
**值**:
- `0` - 创意工坊公开
- `2` - 本地 MOD

```lua
Visibility = 2  -- 本地 MOD
```

### TagList（标签列表）

**类型**: Table  
**常用标签**:
- `"Modifications"` - 修改类
- `"Quality of Life"` - 生活质量
- `"Optimizations"` - 优化类
- `"New Content"` - 新内容

```lua
TagList = {
    [1] = "Modifications",
    [2] = "Quality of Life",
}
```

### ChangeConfig（允许修改配置）

**类型**: Boolean  
**说明**: 是否允许在游戏中修改配置

```lua
ChangeConfig = false  -- 不允许运行时修改
```

### HasArchive（有存档数据）

**类型**: Boolean  
**说明**: MOD 是否有存档相关数据

```lua
HasArchive = false  -- 无存档数据
```

### NeedRestartWhenSettingChanged（配置更改需重启）

**类型**: Boolean  
**说明**: 配置更改后是否需要重启游戏

```lua
NeedRestartWhenSettingChanged = false  -- 不需要重启
```

### Cover / WorkshopCover（封面图片）

**类型**: String 或 nil  
**说明**: MOD 封面图片文件名

```lua
Cover = "Cover.png"           -- 本地图片
WorkshopCover = "Cover.png"   -- 创意工坊封面
-- 或
Cover = nil                   -- 无封面
```

### SettingGroups（设置分组）

**类型**: Table  
**说明**: 为配置项定义分组名称

```lua
SettingGroups = {
    [1] = "数值设置",
    [2] = "高级选项",
}
```

### UpdateLogList（更新日志）

**类型**: Table  
**说明**: 创意工坊 MOD 的更新历史记录

```lua
UpdateLogList = {
    [1] = {
        Timestamp = 1781688338,
        LogList = {
            [1] = "初始版本发布",
        },
    },
}
```

### FileId（创意工坊 ID）

**类型**: Integer  
**说明**: 创意工坊 MOD 的文件 ID

```lua
FileId = 3746477341  -- 仅创意工坊 MOD 需要
```

---

## ⚙️ DefaultSettings 配置项

### 基本结构

```lua
DefaultSettings = {
    [1] = {
        SettingType = "Toggle",      -- 设置类型（必填）
        Key = "EnableFeature",       -- 配置键名（必填）
        DisplayName = "启用功能",     -- 显示名称（必填）
        Description = "功能描述",     -- 详细描述（必填）
        DefaultValue = true,         -- 默认值（必填）
        GroupName = "基础设置",       -- 分组名称（可选）
        
        -- Slider 特有字段
        MinValue = 0,                -- 最小值
        MaxValue = 100,              -- 最大值
        StepSize = 1,                -- 步长
        
        -- InputField 特有字段
        Placeholder = "请输入...",    -- 占位符文本
    },
}
```

### 必填字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `SettingType` | String | 设置类型（见下文） |
| `Key` | String | 配置键名，代码中通过此键读取值 |
| `DisplayName` | String | UI 中显示的 name |
| `Description` | String | 鼠标悬停时显示的描述 |
| `DefaultValue` | Any | 默认值，类型取决于 SettingType |

### 可选字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `GroupName` | String | 设置分组名称 |

---

## 🎯 支持的 SettingType

根据实际 MOD 分析，太吾绘卷支持以下设置类型：

### 1. Toggle（开关）

**用途**: 布尔值开关  
**DefaultValue**: `true` 或 `false`

```lua
[1] = {
    SettingType = "Toggle",
    Key = "EnableAutoProcess",
    DisplayName = "启用自动处理",
    Description = "开启后自动处理符合条件的月度事件。",
    DefaultValue = false,
},
```

### 2. Slider（滑块）

**用途**: 数值选择  
**必需额外字段**: `MinValue`, `MaxValue`, `StepSize`  
**DefaultValue**: Number

```lua
[2] = {
    SettingType = "Slider",
    Key = "inventoryCount",
    DisplayName = "背包扩容数",
    Description = "人物背包可负重数量增加……",
    MinValue = 0,
    MaxValue = 30000,
    StepSize = 10,
    DefaultValue = 30000,
},
```

**参数说明**:
- `MinValue`: 最小值
- `MaxValue`: 最大值
- `StepSize`: 步进值（0 表示连续滑动）

### 3. InputField（输入框）

**用途**: 文本输入（数字、字符串等）  
**DefaultValue**: String

```lua
[3] = {
    SettingType = "InputField",
    Key = "ResidenceMultiplier",
    DisplayName = "居所倍率",
    Description = "使用倍率模式时，居所每级容纳人数 = 原版数值 × 此倍率。",
    GroupName = "数值设置",
    DefaultValue = "100000",
},
```

**注意**: 
- InputField 返回的是字符串
- 代码中需要自行转换为数字或其他类型
- 可以输入逗号分隔的多个值（如 `"70,140,210"`）

---

## ❌ 不支持的 SettingType

以下类型**不被支持**，使用会导致 `Invalid Mod Setting Type` 错误：

- ❌ `TextField` - **不存在**
- ❌ `Dropdown` - 未在实际 MOD 中发现
- ❌ `ColorPicker` - 未在实际 MOD 中发现
- ❌ `Button` - 未在实际 MOD 中发现

---

## 📝 完整示例

### 示例 1: 简单开关型 MOD

```lua
return {
    Title = "批量月度交互自动化",
    Author = "WQMYH",
    Version = "0.1.0.0",
    Description = "自动检测并批量化处理太吾绘卷中的月度交互事件。",
    GameVersion = "1.0.24",
    
    FrontendPlugins = {
        [1] = "AutoMonthlyEvent.Frontend.dll",
    },
    BackendPlugins = {
        [1] = "AutoMonthlyEvent.Backend.dll",
    },
    
    TagList = {
        [1] = "Modifications",
        [2] = "Quality of Life",
    },
    
    Visibility = 2,
    
    DefaultSettings = {
        [1] = {
            SettingType = "Toggle",
            Key = "EnableAutoProcess",
            DisplayName = "启用自动处理",
            Description = "开启后自动处理符合条件的月度事件。",
            DefaultValue = false,
        },
        [2] = {
            SettingType = "Toggle",
            Key = "DiscoveryMode",
            DisplayName = "发现模式",
            Description = "只读模式，导出事件ID和选项到JSON文件。",
            DefaultValue = true,
        },
        [3] = {
            SettingType = "Toggle",
            Key = "LogVerbose",
            DisplayName = "详细日志",
            Description = "启用详细的日志输出。",
            DefaultValue = false,
        },
    },
    
    ChangeConfig = false,
    HasArchive = false,
    NeedRestartWhenSettingChanged = false,
    Cover = "Cover.png",
    WorkshopCover = "Cover.png",
}
```

### 示例 2: 数值配置型 MOD

```lua
return {
    Title = "居所仓库扩展",
    Author = "吴丁秋",
    Version = "1.0.1.0",
    Description = "提高「居所」可容纳人数、「仓库」重量上限。",
    GameVersion = "1.0.10.0",
    
    BackendPlugins = {
        [1] = "TaiwuBuildingCapacity.dll",
    },
    FrontendPlugins = {
        [1] = "TaiwuBuildingCapacity.dll",
    },
    
    DefaultSettings = {
        [1] = {
            SettingType = "Toggle",
            Key = "UseFixedValues",
            DisplayName = "使用固定数值",
            Description = "关闭时使用倍率；开启时使用固定数值。",
            GroupName = "数值设置",
            DefaultValue = false,
        },
        [2] = {
            SettingType = "InputField",
            Key = "ResidenceMultiplier",
            DisplayName = "居所倍率",
            Description = "使用倍率模式时，居所每级容纳人数 = 原版数值 × 此倍率。",
            GroupName = "数值设置",
            DefaultValue = "100000",
        },
        [3] = {
            SettingType = "Slider",
            Key = "WarehouseCapacity",
            DisplayName = "仓库容量",
            Description = "仓库可容纳的重量上限。",
            GroupName = "数值设置",
            MinValue = 100,
            MaxValue = 1000000,
            StepSize = 100,
            DefaultValue = 500000,
        },
    },
    
    SettingGroups = {
        [1] = "数值设置",
    },
    
    ChangeConfig = true,
    HasArchive = false,
    NeedRestartWhenSettingChanged = true,
    Source = 1,
    FileId = 3746477341,
    TagList = {
        [1] = "Modifications",
    },
    Visibility = 0,
    Cover = nil,
    WorkshopCover = nil,
}
```

---

## ⚠️ 常见错误

### 错误 1: 使用不支持的 SettingType

**错误代码**:
```lua
[4] = {
    SettingType = "TextField",  -- ❌ 不支持！
    Key = "DumpDirectory",
    DisplayName = "输出目录",
    DefaultValue = "Dump_out",
},
```

**错误信息**:
```
Invalid Mod Setting Type
[加载Mod配置失败]: 加载 Mod 配置文件失败, 已跳过.
```

**修复**:
```lua
-- 方案 1: 移除该配置项，在代码中使用硬编码
-- 方案 2: 改用 InputField
[4] = {
    SettingType = "InputField",  -- ✅ 正确
    Key = "DumpDirectory",
    DisplayName = "输出目录",
    Description = "相对于游戏根目录的输出文件夹路径。",
    DefaultValue = "Dump_out",
},
```

### 错误 2: 缺少必填字段

**错误代码**:
```lua
[1] = {
    SettingType = "Toggle",
    Key = "EnableFeature",
    -- 缺少 DisplayName, Description, DefaultValue
},
```

**修复**: 补全所有必填字段

### 错误 3: Slider 缺少必需参数

**错误代码**:
```lua
[2] = {
    SettingType = "Slider",
    Key = "Value",
    DisplayName = "数值",
    DefaultValue = 50,
    -- 缺少 MinValue, MaxValue, StepSize
},
```

**修复**:
```lua
[2] = {
    SettingType = "Slider",
    Key = "Value",
    DisplayName = "数值",
    MinValue = 0,      -- ✅ 添加
    MaxValue = 100,    -- ✅ 添加
    StepSize = 1,      -- ✅ 添加
    DefaultValue = 50,
},
```

### 错误 4: 作者名称不一致

**问题**: 项目目录和游戏目录的 Config.lua 作者名不同

**症状**: 日志显示 `Author = "WQHH"` 但实际是 `"WQMYH"`

**原因**: 游戏读取的是旧版本的 Config.lua

**修复**: 重新部署 Config.lua 到游戏目录

---

## 💡 最佳实践

### 1. 配置项命名规范

**Key 命名**:
- 使用 PascalCase（大驼峰）
- 清晰表达含义
- 避免缩写

```lua
-- ✅ 推荐
Key = "EnableAutoProcess"
Key = "DiscoveryMode"
Key = "ResidenceMultiplier"

-- ❌ 不推荐
Key = "enable"
Key = "mode"
Key = "mult"
```

### 2. 描述文本规范

**DisplayName**:
- 简洁明了
- 不超过 10 个字符
- 使用中文

**Description**:
- 详细说明功能
- 说明影响范围
- 可以使用 `\v` 换行

```lua
DisplayName = "启用自动处理",
Description = "开启后自动处理符合条件的月度事件。\v发现阶段请保持关闭。",
```

### 3. 默认值选择

**原则**:
- Toggle: 默认为 `false`（保守策略）
- Slider: 默认为中间值或安全值
- InputField: 默认为空字符串或合理默认值

```lua
-- ✅ 推荐：保守默认值
DefaultValue = false   -- 功能默认关闭
DefaultValue = "100"   -- 合理的默认倍率

-- ❌ 不推荐：激进默认值
DefaultValue = true    -- 可能导致意外行为
DefaultValue = "999999" -- 极端值
```

### 4. 分组管理

当配置项超过 5 个时，建议使用 `GroupName` 和 `SettingGroups` 进行分组：

```lua
DefaultSettings = {
    [1] = {
        SettingType = "Toggle",
        Key = "EnableFeature1",
        DisplayName = "功能1",
        GroupName = "基础设置",  -- 分组
        DefaultValue = false,
    },
    [2] = {
        SettingType = "Slider",
        Key = "Value1",
        DisplayName = "数值1",
        GroupName = "高级设置",  -- 另一分组
        MinValue = 0,
        MaxValue = 100,
        StepSize = 1,
        DefaultValue = 50,
    },
},

SettingGroups = {
    [1] = "基础设置",
    [2] = "高级设置",
},
```

### 5. 配置更改策略

**ChangeConfig = false**:
- 配置只能在游戏外修改
- 适合需要重启才能生效的配置

**ChangeConfig = true**:
- 配置可以在游戏中实时修改
- 需要在代码中监听配置变化

**NeedRestartWhenSettingChanged**:
- `true`: 配置更改后提示重启
- `false`: 配置即时生效

---

## 🔍 代码中读取配置

### Backend 中读取配置

```csharp
using GameData.Domains.Mod;

// 读取 Toggle 配置
bool enableAutoProcess = ModManager.GetModSetting<bool>("EnableAutoProcess");

// 读取 Slider 配置
int inventoryCount = ModManager.GetModSetting<int>("inventoryCount");

// 读取 InputField 配置（字符串）
string dumpDirectory = ModManager.GetModSetting<string>("DumpDirectory");

// 将字符串转换为数字
if (int.TryParse(dumpDirectory, out int value))
{
    // 使用 value
}
```

### 监听配置变化

```csharp
public override void OnModSettingUpdate()
{
    // 配置更改时调用
    bool newValue = ModManager.GetModSetting<bool>("EnableFeature");
    
    // 重新初始化或应用新配置
    InitializeWithNewSettings(newValue);
}
```

---

## 📚 参考资料

### 成功 MOD 示例

1. **居所仓库扩展** (Workshop ID: 3746477341)
   - 展示了 InputField 的使用
   - 包含配置分组
   - 支持倍率和固定数值两种模式

2. **背包仓储数量最大加** (Workshop ID: 2872084063)
   - 展示了 Slider 的使用
   - 简洁的配置结构
   - 适合新手参考

3. **CopyBuildingModernized** (本地 MOD)
   - 展示了 Toggle 和 Slider 的组合
   - 包含详细的中文描述
   - 良好的分组管理

---

## 📝 检查清单

在提交 Config.lua 之前，请确认：

- [ ] 使用 `return { ... }` 格式
- [ ] 包含所有必填字段（Title, Author, Version, Description, GameVersion）
- [ ] BackendPlugins 和 FrontendPlugins 正确配置
- [ ] 所有 SettingType 都是支持的类型（Toggle/Slider/InputField）
- [ ] Slider 包含 MinValue, MaxValue, StepSize
- [ ] 所有配置项都有 DisplayName 和 Description
- [ ] DefaultValue 类型与 SettingType 匹配
- [ ] 没有使用 TextField 等不支持的类型
- [ ] 已部署到游戏目录
- [ ] 测试 MOD 能正常加载

---

**文档版本**: 1.0  
**维护者**: Taiwu MODs 开发社区  
**最后更新**: 2026-06-24
