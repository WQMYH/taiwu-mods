# 项目结构说明

## 目录结构

```
AutoMonthlyEvent/
├── Plugins/                              # 插件目录
│   ├── AutoMonthlyEvent.Backend/        # 后端插件（核心逻辑）
│   │   ├── AutoMonthlyEvent.Backend.csproj  # 项目文件
│   │   ├── BackendEntry.cs              # 插件入口点
│   │   ├── AutoMonthlyEventProcessor.cs # 核心处理器（Harmony Patch）
│   │   └── ConfigManager.cs             # 配置管理器
│   │
│   └── AutoMonthlyEvent.Frontend/       # 前端插件（UI相关，可选）
│       ├── AutoMonthlyEvent.Frontend.csproj  # 项目文件
│       └── FrontendEntry.cs             # 前端入口点
│
├── Config.lua                           # MOD配置文件
├── README.md                            # 完整使用文档
├── QUICKSTART.md                        # 快速开始指南
├── PROJECT_STRUCTURE.md                 # 项目结构说明（本文件）
├── .gitignore                          # Git忽略文件
├── build.bat                           # 编译脚本
└── deploy.bat                          # 部署脚本
```

## 核心文件说明

### 1. BackendEntry.cs
**作用**：后端插件的入口点
- 实现 `IModPlugin` 接口
- 在 `OnLoad()` 中初始化配置和Harmony
- 在 `OnUnload()` 中清理资源

**关键代码**：
```csharp
[Plugin("AutoMonthlyEvent.Backend", "0.1.0", "Auto Monthly Event Processor")]
public class BackendEntry : IModPlugin
{
    public void OnLoad()
    {
        ConfigManager.Instance.LoadConfig();
        _harmony = new Harmony("com.auto.monthlyevent.backend");
        _harmony.PatchAll(typeof(BackendEntry).Assembly);
    }
}
```

### 2. AutoMonthlyEventProcessor.cs
**作用**：核心业务逻辑，实现自动处理功能
- 使用Harmony Patch拦截 `UI_MonthNotify.OnInit()`
- 监听月度事件集合的返回
- 检查事件类型，决定是否自动处理
- 调用 `WorldDomainMethod.Call.ProcessAllMonthlyEventsWithDefaultOption()`

**关键方法**：
- `OnInit_Postfix()` - Patch月度通知UI初始化
- `OnNotifyGameData_Postfix()` - Patch游戏数据通知
- `ProcessMonthlyEvents()` - 处理月度事件
- `HasForbiddenEvents()` - 检查是否有禁止自动处理的事件

### 3. ConfigManager.cs
**作用**：管理MOD配置
- 加载和保存Config.lua
- 解析Lua配置为C#对象
- 提供默认配置值

**配置项**：
- `EnableAutoProcess` - 启用/禁用自动处理
- `ShowConfirmation` - 显示确认对话框（预留）
- `SkipSpecialEvents` - 跳过特殊事件
- `LogVerbose` - 详细日志

### 4. FrontendEntry.cs
**作用**：前端插件入口（可选功能）
- 可以注册MOD显示事件处理器
- 用于显示自定义UI提示
- 当前版本为基础实现

### 5. Config.lua
**作用**：用户可编辑的配置文件
- Lua格式的键值对
- 游戏启动时由ConfigManager读取
- 修改后需重启游戏生效

## 技术架构

### Harmony Patch机制

```
原版流程:
UI_MonthNotify.OnInit() 
  → 请求月度事件集合
  → 显示UI等待玩家操作

MOD增强流程:
UI_MonthNotify.OnInit() 
  → [Harmony Postfix介入]
  → 检查配置是否启用
  → 注册监听器
  
UI_MonthNotify.OnNotifyGameData()
  → [Harmony Postfix介入]
  → 接收月度事件集合
  → 检查事件类型
  → 如果安全则自动处理
  → 否则保持原行为
```

### 数据流

```
1. 过月触发
   ↓
2. BasicGameData.AdvancingMonthState = 20
   ↓
3. UI_MonthNotify.OnInit() 被调用
   ↓
4. Harmony Postfix 执行
   ↓
5. 请求月度事件集合
   ↓
6. WorldDomainMethod.Call.GetMonthlyEventCollection()
   ↓
7. 后端返回 MonthlyEventCollection
   ↓
8. UI_MonthNotify.OnNotifyGameData() 接收
   ↓
9. Harmony Postfix 检查事件类型
   ↓
10a. 全部NormalEvent → ProcessAllMonthlyEventsWithDefaultOption()
10b. 有SpecialEvent → 跳过，保持UI打开
```

## 依赖关系

### 程序集引用

**Backend需要**：
- Assembly-CSharp.dll - 游戏主程序集
- GameData.Shared.dll - 游戏数据结构
- 0Harmony.dll - Harmony库
- UnityEngine.dll - Unity引擎
- UnityEngine.CoreModule.dll - Unity核心模块

**Frontend需要**：
- Assembly-CSharp.dll
- GameData.Shared.dll
- UnityEngine.dll
- UnityEngine.UI.dll
- Unity.TextMeshPro.dll

### NuGet包

当前项目不使用NuGet包，所有依赖来自游戏本身。

## 编译流程

1. **dotnet build** 读取 .csproj 文件
2. 解析引用的DLL路径（通过 `$(TaiwuGameDir)` 变量）
3. 编译C#代码为 netstandard2.1 程序集
4. 输出到 `bin/Release/netstandard2.1/` 目录

## 部署流程

1. **build.bat** 执行编译
2. **deploy.bat** 复制文件：
   - Config.lua → 游戏目录/Mods/AutoMonthlyEvent/
   - *.dll → 游戏目录/Mods/AutoMonthlyEvent/Plugins/
3. 游戏启动时扫描Mods目录
4. 加载找到的MOD插件

## 扩展开发

### 添加新功能

1. 在Backend中添加新的Patch类
2. 使用 `[HarmonyPatch]` 和 `[HarmonyPostfix]` 特性
3. 在BackendEntry中确保 `PatchAll()` 会扫描到新类

### 添加配置项

1. 在ConfigManager中添加属性
2. 在ParseConfig()中添加解析逻辑
3. 在GenerateConfigContent()中添加输出
4. 更新Config.lua模板

### 添加UI功能

1. 在Frontend中创建UI脚本
2. 使用Unity API创建界面元素
3. 通过ModManager注册显示事件
4. 从Backend发送事件到Frontend

## 调试技巧

### 查看日志

```
Player.log - 游戏主日志，包含MOD加载信息
GameData_*.log - 游戏数据日志，包含API调用
```

### 开启详细日志

编辑Config.lua：
```lua
LogVerbose = true
```

### 断点调试

1. 在Visual Studio中附加到游戏进程
2. 设置断点在Patch方法中
3. 触发对应游戏行为

## 性能考虑

- Harmony Patch只在方法调用时增加微小开销
- 配置只在加载时读取一次
- 事件类型检查是O(n)复杂度，n为事件数量（通常很小）
- 不会创建额外的线程或协程

## 安全边界

MOD严格遵守以下原则：
1. 不修改游戏内存中的数据结构
2. 只调用公开的游戏API
3. 不进行任何破坏性操作
4. 异常时回退到原版行为
5. 允许用户完全禁用功能
