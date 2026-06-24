# AutoMonthlyEvent - 自动月度事件处理MOD

## 功能说明

这是一个太吾绘卷MOD，用于在过月时自动批量处理所有可默认处理的月度交互事件，减少玩家手动操作负担。

### 主要特性

- ✅ **智能检测**：自动识别过月时的月度交互事件
- ✅ **安全处理**：只自动处理NormalEvent类型的事件
- ✅ **保护机制**：跳过SpecialEvent和LockedEvent，确保不会错过重要剧情
- ✅ **灵活配置**：提供多个配置选项自定义行为
- ✅ **详细日志**：支持详细日志输出，方便调试

## 安装方法

1. 编译项目生成DLL文件
2. 将以下文件复制到游戏Mods目录：
   ```
   太吾绘卷根目录/Mods/AutoMonthlyEvent/
   ├── Plugins/
   │   ├── AutoMonthlyEvent.Backend.dll
   │   └── AutoMonthlyEvent.Frontend.dll
   └── Config.lua
   ```
3. 启动游戏，MOD会自动加载

## 配置说明

编辑 `Config.lua` 文件来自定义MOD行为：

```lua
{
    -- 启用自动处理月度事件（默认：true）
    EnableAutoProcess = true,
    
    -- 显示确认对话框（默认：false，暂未实现）
    ShowConfirmation = false,
    
    -- 跳过特殊事件（默认：true）
    -- SpecialEvent和LockedEvent不会被自动处理
    SkipSpecialEvents = true,
    
    -- 详细日志输出（默认：false）
    -- 开启后会在Player.log中输出详细信息
    LogVerbose = false
}
```

### 配置项详解

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| EnableAutoProcess | boolean | true | 是否启用自动处理功能 |
| ShowConfirmation | boolean | false | 处理前是否显示确认对话框（预留） |
| SkipSpecialEvents | boolean | true | 是否跳过特殊事件（强烈建议保持true） |
| LogVerbose | boolean | false | 是否输出详细日志 |

## 工作原理

### 事件类型分类

太吾绘卷的月度事件分为三种类型：

1. **NormalEvent (Type=0)**：普通事件，可以安全地自动处理
   - 例如：普通的人际关系变化、资源增减等
   
2. **SpecialEvent (Type=1)**：特殊事件，需要玩家手动处理
   - 例如：重要剧情事件、关键决策点
   
3. **LockedEvent (Type=2)**：锁定事件，必须玩家手动处理
   - 例如：某些强制性的剧情推进

### 自动处理流程

```
过月 → AdvancingMonthState == 20
     ↓
显示月度通知UI
     ↓
获取月度事件集合
     ↓
检查事件类型
     ↓
┌─────────────────────┐
│ 是否有SpecialEvent  │
│ 或LockedEvent？     │
└─────────────────────┘
     ↓            ↓
   是(有)       否(无)
     ↓            ↓
  跳过自动    调用ProcessAllMonthlyEventsWithDefaultOption()
  处理        批量处理所有事件
     ↓            ↓
  玩家手动    自动完成
  处理        关闭UI
```

### 安全检查

MOD会执行以下安全检查：

1. ✅ 只在 `AdvancingMonthState == 20` 时执行
2. ✅ 不在保存世界时执行
3. ✅ 检查所有事件的Type字段
4. ✅ 发现任何Type > 0的事件就跳过自动处理
5. ✅ 异常时回退到原版行为

## 使用场景

### 适合使用

- 快速过月，不想逐个点击"默认"按钮
- 重复游玩时，已经熟悉常规事件
- 专注于特定玩法，希望减少琐事操作

### 不适合使用

- 第一次游玩，想仔细阅读每个事件
- 想要体验完整剧情和随机事件
- 喜欢手动控制每个决策

## 兼容性

### 与其他MOD兼容

- ✅ 不修改原版数据结构
- ✅ 只调用公开API
- ✅ 可以随时禁用功能
- ⚠️ 如果其他MOD也Patch了UI_MonthNotify，可能存在冲突

### 游戏版本

- 适用于太吾绘卷正式版
- 基于游戏版本：需要测试确认

## 故障排除

### MOD不生效

1. 检查Config.lua中 `EnableAutoProcess` 是否为true
2. 查看Player.log是否有错误信息
3. 确认DLL文件已正确放置到Mods目录
4. 检查游戏是否成功加载MOD

### 自动处理没有执行

可能原因：
- 当前月份存在SpecialEvent或LockedEvent
- 游戏正在保存世界
- AdvancingMonthState不是20

解决方法：
- 开启LogVerbose查看详细日志
- 检查Player.log中的MOD输出

### 游戏崩溃或异常

1. 立即禁用MOD（设置EnableAutoProcess=false）
2. 查看Player.log和GameData_*.log
3. 报告问题时附上日志文件

## 开发说明

### 项目结构

```
AutoMonthlyEvent/
├── Plugins/
│   ├── AutoMonthlyEvent.Backend/      # 后端逻辑
│   │   ├── BackendEntry.cs            # 插件入口
│   │   ├── AutoMonthlyEventProcessor.cs # 核心处理器
│   │   └── ConfigManager.cs           # 配置管理
│   └── AutoMonthlyEvent.Frontend/     # 前端（可选）
│       └── FrontendEntry.cs           # 前端入口
├── Config.lua                         # 配置文件
└── README.md                          # 说明文档
```

### 编译方法

```bash
# 在项目根目录执行
dotnet build AutoMonthlyEvent.sln
```

### 关键技术

- **Harmony Patch**：用于拦截和增强原版方法
- **WorldDomainMethod API**：调用游戏后端API处理事件
- **MonthlyEvent.Instance**：查询事件配置信息

## 已知限制

1. ❌ 暂不支持确认对话框（ShowConfirmation配置项预留）
2. ❌ 暂不支持选择性处理特定类型的事件
3. ⚠️ 无法处理需要玩家输入的事件窗口

## 未来计划

- [ ] 添加确认对话框UI
- [ ] 支持白名单/黑名单机制
- [ ] 添加统计功能（记录自动处理次数）
- [ ] 支持更多自定义规则

## 反馈与支持

如遇到问题或有改进建议，请：
1. 查看Player.log获取错误信息
2. 开启LogVerbose收集详细日志
3. 报告问题时提供：
   - 游戏版本
   - MOD版本
   - Player.log相关片段
   - 问题描述和复现步骤

## 许可证

本项目遵循MIT许可证。

## 致谢

感谢太吾绘卷MOD社区提供的工具和文档支持。

---

**警告**：使用MOD可能导致存档损坏或游戏异常，请定期备份存档！
