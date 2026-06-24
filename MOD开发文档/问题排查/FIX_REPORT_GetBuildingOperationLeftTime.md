# GetBuildingOperationLeftTime NullReferenceException 修复报告

## 问题概述

导入扩宽后的太吾村蓝图后，GameData 后端崩溃：

```text
System.NullReferenceException: Object reference not set to an instance of an object.
   at GameData.Domains.Building.BuildingDomain.GetBuildingOperationLeftTime(
       DataContext context, BuildingBlockKey blockKey, SByte operationType)
```

典型发生时机：

- `ImportVillage` 后端导入完成后约 0.3-0.8 秒。
- 前端产业界面仍在刷新建筑状态。
- GameData 进程随后发送 `ErrorMessages` 和 `Disconnect`。

最终修复策略：

- 保留运行时完整建筑网格，满足原版 `UpdateTaiwuVillageBuildingEffect` 的完整格子假设。
- 修复蓝图宽度转换时 2x2 建筑从属格 `RootBlockIndex` 未同步转换的问题。
- 对原版危险方法 `BuildingDomain.GetBuildingOperationLeftTime` 增加 Harmony 防护层，让空地、从属格、非法配置、无操作者等情况安全返回 `-1`。

---

## 最终根因

这次问题不是单一原因，而是两个兼容问题叠加：

1. 宽度转换时只转换了 `BlockIndex`，没有转换 `RootBlockIndex`。
   - `TemplateId == -1` 的 2x2 从属格仍指向旧 18x18 索引。
   - 诊断结果曾出现：

```text
原始 18 宽 bin: support=54 badRoot=0
转换后 24 宽 bin: support=54 badRoot=36
```

2. 原版 `GetBuildingOperationLeftTime` 对 `TemplateId <= 0` 的 block 不安全。
   - 对“找不到 block”会安全返回 `-1`。
   - 但如果运行时存在 `TemplateId=0` 空地 block 或 `TemplateId=-1` 从属格 block，原方法仍可能继续读取配置/操作数据，触发空引用。

同时，不能简单删除空地 block。日志证明删除运行时空地后，原版效果缓存会崩：

```text
System.Collections.Generic.KeyNotFoundException
   at BuildingDomain.GetElement_BuildingBlocks(...)
   at BuildingDomain.GetBuildingBlocksAtLocation(...)
   at BuildingDomain.UpdateTaiwuVillageBuildingEffect()
```

因此最终必须同时做到：

- 运行时保留完整 `width * width` 网格。
- 对危险访问路径做防护。

---

## 诊断历程

### 1. 清理操作状态

最初假设导入蓝图携带了无效运行时状态。

处理内容：

```csharp
blockData.OperationProgress = 0;
data.ArtisanOrders.Clear();
data.AutoWorkBlocks.Clear();
data.AutoSoldBlocks.Clear();
```

结果：失败，仍然在 `GetBuildingOperationLeftTime` 崩溃。

后续又补充清理：

```csharp
blockData.OperationType = -1;
blockData.OperationStopping = false;
```

结果：仍失败。说明问题不只是操作状态字段。

### 2. 禁用前端自动重载

曾怀疑导入后自动退出/重新进入产业界面导致 UI 时序问题。

处理内容：

```csharp
private static bool TryReloadBuildingArea()
{
    Debug.LogWarning("Auto-reload temporarily disabled for debugging.");
    return false;
}
```

结果：仍失败。说明崩溃不是自动重载流程造成的。

### 3. 扩大 BuildingOperatorDict 清理

曾怀疑只清理当前村庄 `0..width*width` 范围不够。

处理内容：

```csharp
foreach (var keyObj in operatorDict.Keys)
{
    if (keyObj is BuildingBlockKey key &&
        key.AreaId == loc.AreaId &&
        key.BlockId == loc.BlockId)
    {
        keysToRemove.Add(keyObj);
    }
}
```

日志：

```text
CleanOperationStateOnImport: scanned all keys, removed 3 entries for village (11, 702).
```

结果：仍失败。说明 operator dict 残留不是唯一原因。

### 4. 核查部署版本

中途出现过一次误判：工作区 DLL 已编译，但游戏加载目录仍是旧 DLL。

实际加载目录：

```text
A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\CopyBuildingModernized.Another\Plugins\
```

工作区输出目录：

```text
E:\Programming\Mods\Taiwu\CopyBuildingModernized.Another\Plugins\
```

必须检查 DLL 的 `mtime / size / hash`，并确认 GameData 日志里出现新版特征日志。

### 5. 诊断 RootBlockIndex

转换后的 `_width24.bin` 一度存在坏的 2x2 从属格：

```text
Width=24 Blocks=576 support=54 badRoot=36 opTypeNotMinus1=0
```

原因是转换时只更新：

```csharp
newBlock.BlockIndex = newIndex;
```

没有同步更新：

```csharp
newBlock.RootBlockIndex
```

修复后重新转换：

```text
Width=24 Blocks=576 support=54 badRoot=0
```

### 6. 尝试跳过/删除空地

曾尝试导入时跳过 `TemplateId=0` 空地：

```text
Import write summary: written=250, skippedEmpty=326, skippedNull=0.
```

但运行时仍存在空地：

```text
[PostImport Check] Found 326 runtime empty blocks (TemplateId=0).
```

随后尝试删除运行时空地，结果：

```text
RemoveRuntimeEmptyBlocks: removed=576, failed=0.
Import write summary: written=250, skippedEmpty=326, skippedNull=0.
KeyNotFoundException at UpdateTaiwuVillageBuildingEffect
```

结论：

- 删除/跳过空地不可行。
- 原版效果缓存需要完整网格。
- 必须保留完整网格，同时 patch 危险查询方法。

---

## 最终修复内容

### 1. 宽度转换同步 RootBlockIndex

离线转换器 `WidthConverter.ConvertWidth` 中，转换 `BlockIndex` 后同步转换 `RootBlockIndex`：

```csharp
BuildingBlockData newBlock = CloneBlockData(blockData);
newBlock.BlockIndex = newIndex;
if (newBlock.RootBlockIndex >= 0)
{
    short newRootIndex = ConvertGridIndex(
        newBlock.RootBlockIndex, oldWidth, targetWidth, offsetX, offsetY);
    newBlock.RootBlockIndex = newRootIndex;
}
```

运行时导入转换 `VillageBuildingData.ConvertAllIndices` 中也同步处理：

```csharp
v.BlockIndex = nk.BuildingBlockIndex;
if (v.RootBlockIndex >= 0)
{
    short newRootIndex = v.RootBlockIndex.ConvertGridIndex(oldWidth, newWidth);
    v.RootBlockIndex = newRootIndex;
}
```

### 2. 输出校验增加 2x2 从属格检查

转换后校验所有 `TemplateId == -1` 从属格：

- `RootBlockIndex` 必须在范围内。
- 根格必须存在。
- 根格 `TemplateId > 0`。

```csharp
if (block.TemplateId == -1)
{
    if (block.RootBlockIndex < 0 || block.RootBlockIndex >= expectedCells)
        return false;

    bool rootExists = data.Blocks.Values.Any(root =>
        root != null &&
        root.BlockIndex == block.RootBlockIndex &&
        root.TemplateId > 0);
    if (!rootExists)
        return false;
}
```

### 3. 导入保留完整网格

`BuildingDataCollector.Apply` 写入所有非 null block，包括空地：

```csharp
foreach (var (key, blockData) in data.Blocks)
{
    if (blockData == null)
    {
        logWarn($"Skip null imported block: {key}");
        continue;
    }

    Traverse.Create(DomainManager.Building)
        .Method("SetElement_BuildingBlocks", key, blockData, context).GetValue();
}
```

预期日志：

```text
Import write summary: written=576, skippedNull=0.
```

### 4. Harmony Prefix 防护

Patch 原版方法：

```csharp
BuildingDomain.GetBuildingOperationLeftTime(
    DataContext context, BuildingBlockKey blockKey, sbyte operationType)
```

前置检查：

```csharp
if (!__instance.TryGetElement_BuildingBlocks(blockKey, out BuildingBlockData blockData) ||
    blockData == null || blockData.TemplateId <= 0)
{
    __result = -1;
    return false;
}

BuildingBlockItem blockConfig;
try
{
    blockConfig = BuildingBlock.Instance[blockData.TemplateId];
}
catch
{
    __result = -1;
    return false;
}

if (blockConfig?.OperationTotalProgress == null ||
    operationType < 0 ||
    operationType >= blockConfig.OperationTotalProgress.Length)
{
    __result = -1;
    return false;
}

if (!__instance.TryGetElement_BuildingOperatorDict(blockKey, out _))
{
    __result = -1;
    return false;
}

return true;
```

### 5. Harmony Finalizer 兜底

即使原方法内部仍有未预见异常，也返回 `-1`，避免 GameData 进程崩溃：

```csharp
private static Exception GetBuildingOperationLeftTimeFinalizer(
    Exception __exception,
    BuildingBlockKey blockKey,
    sbyte operationType,
    ref int __result)
{
    if (__exception == null)
        return null;

    Logger.Warn(__exception,
        "Suppressed GetBuildingOperationLeftTime exception. Key={0}, OperationType={1}",
        blockKey, operationType);

    __result = -1;
    return null;
}
```

### 6. 启动时安装补丁

```csharp
private void InstallHarmonyPatches()
{
    _harmony = new Harmony(GetGuid() + ".backend");

    var target = AccessTools.Method(typeof(BuildingDomain), "GetBuildingOperationLeftTime",
        new[] { typeof(DataContext), typeof(BuildingBlockKey), typeof(sbyte) });

    if (target == null)
    {
        Logger.Warn("GetBuildingOperationLeftTime target not found.");
        return;
    }

    _harmony.Patch(target,
        prefix: new HarmonyMethod(typeof(BackendEntry), nameof(GetBuildingOperationLeftTimePrefix)),
        finalizer: new HarmonyMethod(typeof(BackendEntry), nameof(GetBuildingOperationLeftTimeFinalizer)));

    Logger.Info("Patched GetBuildingOperationLeftTime.");
}
```

---

## 验证标准

### 启动日志

必须出现：

```text
[CopyBuildingModernized] Patched GetBuildingOperationLeftTime.
[CopyBuildingModernized] Backend initialized.
```

### 转换日志

必须出现：

```text
[WidthConverter] [校验] ✅ 校验通过！
```

同时建议用诊断工具确认：

```text
Width=24 Blocks=576 support=54 badRoot=0 opTypeNotMinus1=0
```

### 导入日志

必须出现：

```text
[CM] Import write summary: written=576, skippedNull=0.
[CM] Apply complete.
[CopyBuildingModernized] Imported from ...
```

不应出现：

```text
NullReferenceException at GetBuildingOperationLeftTime
KeyNotFoundException at UpdateTaiwuVillageBuildingEffect
GameData module is about to exit
```

如果出现：

```text
Suppressed GetBuildingOperationLeftTime exception
```

说明防护层兜住了异常，但仍应继续分析是否还有可修复的数据源。

---

## 完整错误检查流程

这套流程适用于后续排查其他导入、宽度转换、运行时刷新类错误。

### Step 1: 确认运行的是目标版本

检查内容：

- 工作区 DLL 与游戏 Mod 目录 DLL 是否一致。
- 文件时间、大小、hash 是否一致。
- GameData 日志是否出现新版本特征日志。

常用对照目录：

```text
工作区:
E:\Programming\Mods\Taiwu\CopyBuildingModernized.Another\Plugins\

游戏实际加载:
A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\CopyBuildingModernized.Another\Plugins\
```

没有先确认版本，后续日志分析可能全部失真。

### Step 2: 对齐 Player.log 和 GameData 日志

`Player.log` 用来确认前端表现和最终错误栈。

`GameData_*.log` 用来确认后端真实执行流程：

- 方法是否被调用。
- 哪一步完成。
- 哪一步异常。
- mod 自己的日志是否出现。

按时间线整理：

```text
ConvertVillageWidth
WidthConverter 校验
ImportVillage
Before conversion
After conversion
CleanOperationStateOnImport
Import write summary
Apply complete
Frontend callback
Crash / no crash
```

### Step 3: 区分异常类型

常见分支：

```text
NullReferenceException at GetBuildingOperationLeftTime
```

优先检查危险访问路径和 Harmony 防护。

```text
KeyNotFoundException at UpdateTaiwuVillageBuildingEffect
```

优先检查运行时网格是否缺格。

```text
Import failed
```

去 GameData 日志找后端捕获的真实异常，不要只看 Player.log。

### Step 4: 校验蓝图文件结构

至少检查：

- `Width` 合法。
- `Blocks.Count == Width * Width`。
- 没有 null block。
- key index 与 `block.BlockIndex` 一致。
- `TemplateId == -1` 的从属格根索引合法。
- `badRoot == 0`。
- `OperationType == -1`。

推荐输出摘要：

```text
Width=24 Blocks=576 support=54 badRoot=0 empty=326 opTypeNotMinus1=0
```

### Step 5: 校验运行时写入行为

确认导入是否符合原版期望：

- 完整网格场景必须 `written == Width * Width`。
- 不要删除原版后续流程需要的 key。
- 如果跳过某类 block，要确认原版后续流程是否能承受缺 key。

本案例结论：

```text
完整网格是必须的。
删除空地会导致 UpdateTaiwuVillageBuildingEffect KeyNotFoundException。
```

### Step 6: 反编译崩溃方法

对崩溃方法做最小反编译或 IL 分析，确认：

- 哪些输入路径安全返回。
- 哪些输入路径会空引用。
- 是否有数组越界、字典索引、配置缺失、角色缺失。

不要只靠猜测改数据。

### Step 7: 选择修复策略

优先级：

1. 修复确定的数据结构错误。
2. 保持原版流程要求的结构不变。
3. 对原版危险访问点加防护。
4. 只在无法安全兼容时才删除或跳过数据。

本案例中：

- `RootBlockIndex` 是确定的数据结构错误，必须修。
- 完整网格是原版流程要求，必须保留。
- `GetBuildingOperationLeftTime` 是危险访问点，必须 patch。

### Step 8: 验证修复

验证必须覆盖：

- 启动时补丁安装成功。
- 转换文件结构正确。
- 导入日志完整。
- 无 GameData 进程断开。
- 前端界面能继续操作。

建议每次测试保留：

- 当前 `Player.log` 片段。
- 最新 `GameData_*.log` 片段。
- 转换后 bin 的结构摘要。
- DLL hash 或 mtime。

---

## 后续维护建议

### 1. 保留诊断日志，但降低噪音

建议保留关键 Info：

```text
Patched GetBuildingOperationLeftTime.
Import write summary...
Apply complete.
```

高频拦截日志建议用 Debug 或计数采样，避免刷屏。

### 2. 可扩展防护其他建筑方法

类似风险可能存在于：

- 获取建筑产出进度的方法。
- 获取建筑资源产出的方法。
- 获取商铺/工坊/采集收益的方法。

处理流程同样是：

1. 反编译目标方法。
2. 找出危险访问点。
3. Prefix 先校验。
4. Finalizer 兜底。

### 3. 不建议默认加入过多配置开关

Harmony 防护属于兼容安全层，默认开启更合适。

只有当防护可能改变玩法结果时，再考虑加入配置项。

---

## 关键经验

1. 先确认加载版本，再分析日志。
2. 双日志对齐比单看 Player.log 更可靠。
3. 数据结构错误要修，原版危险访问也要防。
4. 不要轻易删除原版流程依赖的数据。
5. 宽度转换必须同步所有索引字段，不只是 `BlockIndex`。
6. Harmony Prefix + Finalizer 是处理原版空引用兼容问题的有效模式。

最终有效方案不是“删除空地”，而是：

```text
完整网格 + 正确索引 + 操作状态清理 + 危险方法防护
```

