# 连续挖掘 (ContinuousDigging)

## 功能说明

修改太吾绘卷的挖宝逻辑，实现**连续挖掘**功能：

- ✅ 点击游戏原有的"挖宝"按钮即可触发连续挖掘
- ✅ 自动持续挖掘直到满足退出条件
- ✅ 无需添加新的 UI 按钮，完全复用游戏原有界面
- ✅ 通过 MOD 设置界面配置参数

### 核心原理

游戏原本有 `_series` 连续挖宝模式，但设计为：**只有失败时才继续，成功就停止**。本 MOD 通过 Hook `UI_FindTreasure.AnimFinalCall` 方法，**即使挖宝成功也继续挖掘**，直到满足退出条件。

## 退出条件

连续挖掘会在以下任一条件满足时停止：

1. **达到最大挖掘次数** - 默认50次（可配置）
2. **本月剩余天数不足3天** - 与原版一致
3. ⚠️ **地格没有宝物** - TODO: 需要后续实现
4. ⚠️ **精力不足** - TODO: 需要后续实现
5. ⚠️ **达到最高品级限制** - TODO: 需要后续实现

## 配置说明

在游戏中通过 **MOD 设置界面** 调整以下参数：

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| 启用连续挖掘 | 开关 | ✅ 开启 | 控制是否启用连续挖掘功能 |
| 最高品级限制 | 整数 | 0 | 0=不限制，1-9=挖掘到此品级及以上时停止（数值越小品质越高） |
| 每次挖掘消耗精力 | 整数 | 10 | 每次挖掘消耗的精力值（1-100） |
| 启用精力检查 | 开关 | ✅ 开启 | 是否在精力不足时停止挖掘 |
| 最大连续挖掘次数 | 整数 | 50 | 单次连续挖掘的最大次数（1-100） |

### 品级说明

太吾绘卷的物品品级范围是 **0-9**：
- **0** = 最高品级（神器）
- **9** = 最低品级

配置示例：
- `最高品级限制 = 0`：不限制品级，一直挖到地格清空或精力耗尽
- `最高品级限制 = 3`：挖到3品及以上物品时停止（即只挖0、1、2品）
- `最高品级限制 = 9`：几乎不限制，只有挖不到物品时才停止

## 安装方法

### 1. 编译 MOD

```bash
cd mods/ContinuousDigging
build.bat
```

### 2. 部署到游戏

```bash
deploy.bat
```

或手动复制：
- `Config.lua` → `游戏目录/Mods/ContinuousDigging/Config.lua`
- `ContinuousDigging.Backend.dll` → `游戏目录/Mods/ContinuousDigging/Plugins/`
- `ContinuousDigging.Frontend.dll` → `游戏目录/Mods/ContinuousDigging/Plugins/`

### 3. 启用 MOD

1. 启动太吾绘卷
2. 在主菜单进入 **MOD 管理**
3. 找到 **连续挖掘** 并启用
4. 重启游戏

## 使用方法

1. 在游戏中找到有宝物的地块
2. 点击游戏原有的 **"挖宝"** 按钮
3. MOD 会自动进行连续挖掘
4. 查看日志确认挖掘结果

## 技术实现

### 核心原理

- **Frontend**: Hook `UI_FindTreasure.DoRequestFindTreasure` 方法，拦截原版单次挖宝逻辑
- **Backend**: 注册自定义方法（MethodId: 6200），实现连续挖掘循环
- **通信**: 通过 `AsynchMethodDispatcher` 进行前后端异步调用

### 关键 API

```csharp
// Backend 注册自定义方法
DomainManager.Extra.AddMethodCallHandler(6200, MyContinuousFindTreasures);

// Frontend 调用 Backend
SingletonObject.getInstance<AsynchMethodDispatcher>()
    .AsynchMethodCall<int>(19, 6200, charId, callback);

// 精力操作
DomainManager.Extra.GetActionPointCurrMonth();
DomainManager.Extra.ChangeActionPoint(context, delta);

// 地块物品获取
MapBlockData block = DomainManager.Map.GetBlock(location);
block.Items // 地块物品字典
```

### 日志输出

MOD 会在控制台输出详细日志：
```
[ContinuousDigging] 开始连续挖掘，地块物品数: 5, 福缘: 75
[ContinuousDigging] 第1次挖掘获得: ItemKey(xxx) x 10
[ContinuousDigging] 第2次挖掘获得: ItemKey(xxx) x 5
[ContinuousDigging] 精力不足 (8 < 10)，停止挖掘
[ContinuousDigging] 连续挖掘完成，共挖掘2次，获得2件物品
```

## 注意事项

⚠️ **重要提示**：

1. **月份限制**：需要本月剩余天数 >= 3 才能挖掘（与原版一致）
2. **性能考虑**：最大挖掘次数默认为50，避免一次性处理过多物品导致卡顿
3. **兼容性**：与其他修改挖宝逻辑的 MOD 可能冲突
4. **存档安全**：建议在测试前备份存档

## 故障排查

### 问题：点击挖宝按钮没有反应

**解决方案**：
1. 检查 MOD 是否已启用
2. 查看日志是否有错误信息
3. 确认本月剩余天数 >= 3
4. 确认当前地块有宝物

### 问题：连续挖掘只执行了一次

**解决方案**：
1. 检查是否启用了精力检查
2. 确认精力值足够（至少要有配置的消耗值）
3. 查看日志确认停止原因

### 问题：编译失败

**解决方案**：
1. 确认游戏路径正确（`A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu`）
2. 如果游戏安装在其他位置，修改 `.csproj` 文件中的 `TaiwuDir` 变量
3. 确保安装了 .NET SDK

## 开发者信息

- **MOD 名称**: ContinuousDigging
- **版本**: 1.0.0
- **作者**: MOD Developer
- **基于**: Aron_FindTreasurePatch 源码分析 + TaiwuToolbox 实现模式

## 许可证

本项目仅供学习和研究使用。
