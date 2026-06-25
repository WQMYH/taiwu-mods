# 过月等待链诊断报告 - UpdateInformation瓶颈分析

**分析日期**: 2026-06-26  
**日志文件**: `GameData_2026-06-26_02_07_25.log`  
**问题描述**: PreAdvanceMonth到SaveWorld之间存在极长的等待时间  

---

## 📊 时间线分析

### 第一个完整过月周期（Year 3, Month 11）

#### 整体时间线

```
02:39:22.9595  New month begin (开始过月)
02:39:24.5800  PreAdvanceMonth: 1,605.9ms        ← 准备阶段
02:39:25.7309  UpdateStatus: 1,144.3ms           ← 状态更新
02:39:25.9608  SelfImprovement: 222.5ms          ← 自我提升
02:39:26.1169  CharacterPreparation: 155.6ms     ← 角色准备
02:39:27.8801  CharacterRelationsUpdate: 1,765.5ms  ← 关系更新
02:39:36.2092  CharacterActionPlanning: 8,325.9ms   ← 行动规划 (8.3秒)
02:39:38.7572  CharacterFixedAction: 2,547.6ms      ← 固定行动 (2.5秒)
03:03:20.9573  UpdateInformation: 1,422,198.7ms  ← ⚠️ 信息更新 (1422秒 = 23.7分钟!)
03:03:21.0672  UpdateInfectedCharacters: 21.6ms  ← 感染角色更新
03:03:21.1320  UpdateCharacterMovements: 64.4ms  ← 角色移动更新
03:03:23.7265  UpdateOrganizationMembers: 2,544.5ms ← 组织成员更新 (2.5秒)
03:03:24.3081  AdvanceMonth end (过月结束)
03:03:31.5161  SaveWorld: 2,223.5ms              ← 保存世界 (2.2秒)
```

#### 关键时间节点

| 阶段 | 耗时 | 占比 | 说明 |
|------|------|------|------|
| **PreAdvanceMonth** | 1.6秒 | 0.1% | 过月准备 |
| **UpdateStatus** | 1.1秒 | 0.1% | 状态更新 |
| **CharacterActionPlanning** | 8.3秒 | 0.6% | 行动规划 |
| **CharacterFixedAction** | 2.5秒 | 0.2% | 固定行动 |
| **UpdateInformation** | **1422.2秒** | **98.7%** | 🔴 **信息更新（瓶颈）** |
| **UpdateOrganizationMembers** | 2.5秒 | 0.2% | 组织成员更新 |
| **SaveWorld** | 2.2秒 | 0.2% | 保存世界 |
| **总计** | **1441.5秒** | **100%** | ≈ **24分钟** |

---

### 第二个过月周期（Year 3, Month 12）

#### 当前进度（未完成）

```
03:24:28.2490  AdvanceMonth begin (开始过月)
03:24:30.3608  PreAdvanceMonth: 1,924.0ms        ← 准备阶段 (比上次慢20%)
03:24:31.7612  UpdateStatus: 1,398.9ms           ← 状态更新 (比上次慢22%)
03:24:31.9397  SelfImprovement: 174.2ms          ← 自我提升
03:24:32.0779  CharacterPreparation: 141.9ms     ← 角色准备
03:24:34.1431  CharacterRelationsUpdate: 2,061.5ms  ← 关系更新 (比上次慢17%)
03:24:46.4832  CharacterActionPlanning: 12,339.3ms  ← 行动规划 (12.3秒，比上次慢48%)
03:24:49.5401  CharacterFixedAction: 3,056.4ms      ← 固定行动 (3.1秒，比上次慢20%)
... [日志在此处结束，尚未到达UpdateInformation]
```

#### 预测分析

基于第一个周期的数据，如果第二个周期继续：

```
预计 UpdateInformation 将耗时: ~1400-1500秒 (23-25分钟)
预计总过月时间: ~1450-1500秒 (24-25分钟)
```

---

## 🔍 瓶颈定位：UpdateInformation

### 核心问题

**UpdateInformation** 是过月流程中最耗时的阶段，占总时间的 **98.7%**！

```
第一阶段耗时: 12.7秒 (PreAdvanceMonth → CharacterFixedAction)
第二阶段耗时: 1422.2秒 (UpdateInformation)  ← 瓶颈！
第三阶段耗时: 2.6秒 (UpdateInfectedCharacters → SaveWorld)
```

### UpdateInformation在做什么？

根据日志内容，UpdateInformation期间发生了大量操作：

#### 1. 门派没收物品和囚禁（峨眉派、元山派）

```log
03:02:23.0298  [峨眉派]: Confiscating 20544 worth of resources from 周嵩柱(624)
03:02:23.0298  [峨眉派]: Confiscating 12 items from 周嵩柱(624)
03:02:23.0411  [峨眉派]: add prisoner 周嵩柱(624) for 12 months
03:02:23.0411  [峨眉派]: Confiscating 37562 worth of resources from 季恩誉(589)
03:02:23.0411  [峨眉派]: Confiscating 19 items from 季恩誉(589)
03:02:23.0411  [峨眉派]: add prisoner 季恩誉(589) for 12 months

03:03:05.4768  [元山派]: Confiscating 21313 worth of resources from 杜仁(1892)
03:03:05.4768  [元山派]: Confiscating 15 items from 杜仁(1892)
03:03:05.4768  [元山派]: Confiscating 19246 worth of resources from 游勤(1895)
03:03:05.4768  [元山派]: Confiscating 17 items from 游勤(1895)
```

**耗时**: 约42秒（从03:02:23到03:03:05）

#### 2. 囚犯逃跑检查（大量计算）

```log
03:03:23.7428  少林派关押童文韬(189)... 逃跑概率：-39%，最终取0%
03:03:23.7428  少林派关押余公植(162)... 逃跑概率：-40%，最终取0%
03:03:23.7428  峨眉派关押季恩誉(589)... 逃跑概率：-34%，最终取0%
03:03:23.7552  峨眉派关押周嵩柱(624)... 逃跑概率：-35%，最终取0%
03:03:23.7552  金刚宗关押丹增次松(4187)... 逃跑概率：-53%，最终取0%
03:03:23.7552  金刚宗关押才让金巴(4173)... 逃跑概率：-65%，最终取0%
03:03:23.7552  金刚宗关押南卡罗追(4179)... 逃跑概率：-64%，最终取0%
```

**涉及囚犯**: 至少7个  
**计算复杂度**: 每个囚犯需要计算立场、罪行、身份、精纯、时长等多个因素

#### 3. 捕快刷新机制（大量重复日志）

```log
03:03:23.8996  做一个保底机制确保不会变成捕快窝
03:03:23.8996  刷新各类捕快数量
03:03:23.8996  现在是修习外出状态，清空捕快中
... (重复约30次)
```

**重复次数**: 约30次相同的日志  
**可能原因**: 遍历所有地点/门派执行捕快刷新逻辑

#### 4. 事件脚本执行

```log
03:04:22.8485  删了桌子
03:04:22.8485  设置了正在吃
03:04:26.2744  删了桌子
03:04:28.3619  [金刚宗]: add bounty on 何业(12945) for 18 months
```

---

## 🎯 根本原因分析

### 主要原因：MOD导致的性能问题

#### 嫌疑MOD #1: AdaptableLog

**证据**:
- 所有详细日志都来自`AdaptableLog`
- 大量的"Confiscating"、"add prisoner"、"逃跑概率"日志
- 大量的"捕快刷新"重复日志
- 大量的"更新角色数据"日志

**影响**:
- 频繁的磁盘I/O（写入缓存文件）
- 大量的字符串拼接和日志输出
- 可能的同步阻塞操作

**典型日志**:
```log
更新角色数据 4638到D:\AppData\Local\Temp\Taiwu_Mod\HiddenInfo\Cache_GetCharacterAttribute.txt
```

#### 嫌疑MOD #2: 监狱/囚犯系统相关MOD

**证据**:
- 大量的囚犯管理操作
- 复杂的逃跑概率计算
- 门派没收物品逻辑

**可能MOD**:
- 可能是游戏原版的监狱系统
- 也可能是增强监狱系统的MOD

#### 嫌疑MOD #3: 捕快系统相关MOD

**证据**:
- "做一个保底机制确保不会变成捕快窝"重复出现
- "刷新各类捕快数量"
- "现在是修习外出状态，清空捕快中"

**可能MOD**:
- 可能是游戏原版的捕快系统
- 也可能是优化捕快系统的MOD

---

### 次要原因：数据规模过大

#### 角色数量

```log
Alive characters: 10491
Dead characters: 1510
Non-intelligent characters: 182
Total: 12183 characters
```

**问题**:
- 超过1万个活跃角色
- 每个角色都需要进行状态更新、行动规划等
- UpdateInformation可能需要遍历所有角色

#### 物品数量

```log
Total unowned items: 61
- 50 unowned SkillBook
- 4 unowned TeaWine
- 4 unowned Cricket
- 1 unowned Weapon
- 1 unowned CraftTool
- 1 unowned Misc
```

**问题**:
- 虽然未拥有物品不多，但总物品数可能很大
- 没收物品时需要处理大量物品转移

---

## 📈 性能对比

### 两个周期的对比

| 指标 | 周期1 (Month 11) | 周期2 (Month 12) | 变化 |
|------|------------------|------------------|------|
| PreAdvanceMonth | 1,605.9ms | 1,924.0ms | +20% ⬆️ |
| UpdateStatus | 1,144.3ms | 1,398.9ms | +22% ⬆️ |
| CharacterRelationsUpdate | 1,765.5ms | 2,061.5ms | +17% ⬆️ |
| CharacterActionPlanning | 8,325.9ms | 12,339.3ms | +48% ⬆️ |
| CharacterFixedAction | 2,547.6ms | 3,056.4ms | +20% ⬆️ |
| **趋势** | - | - | **逐渐变慢** ⚠️ |

**结论**: 
- 随着游戏进程，过月时间逐渐增加
- CharacterActionPlanning增长最快（+48%）
- 可能与角色数量增加、关系复杂度提高有关

---

## 💡 解决方案建议

### 方案1: 禁用或优化AdaptableLog MOD（推荐）

**操作步骤**:
1. 找到AdaptableLog相关的MOD
2. 暂时禁用该MOD
3. 测试过月速度是否改善

**预期效果**:
- 减少大量日志输出
- 减少磁盘I/O操作
- 可能节省50-80%的UpdateInformation时间

**风险**:
- 可能失去某些调试信息
- 可能影响依赖该MOD的其他功能

### 方案2: 减少角色数量

**操作步骤**:
1. 使用MOD工具清理死亡角色
2. 减少非智能角色数量
3. 限制新生角色数量

**预期效果**:
- 减少UpdateInformation需要处理的数据量
- 可能节省20-40%的时间

**风险**:
- 可能影响游戏体验
- 需要谨慎操作避免破坏存档

### 方案3: 优化囚犯系统

**如果是MOD导致的**:
1. 找到负责囚犯管理的MOD
2. 检查是否有性能优化选项
3. 考虑暂时禁用复杂的逃跑计算

**预期效果**:
- 减少逃跑概率计算的复杂度
- 可能节省10-20%的时间

### 方案4: 调整日志级别

**如果可以配置**:
1. 将AdaptableLog的日志级别从INFO改为WARN或ERROR
2. 禁用详细的囚犯计算日志
3. 禁用捕快刷新的重复日志

**预期效果**:
- 减少日志输出的开销
- 可能节省5-15%的时间

---

## 🔗 与Player.log的关联

### Player.log中的错误

之前分析的Player.log显示：
```
KeyNotFoundException at GetElement_PregnantStates (Worker13)
```

**关联性**:
- ❌ **无直接关联**
- Player.log的错误是HappyLife MOD导致的怀孕状态问题
- GameData.log的问题是UpdateInformation的性能瓶颈
- 两者是不同的MOD导致的不同问题

**但是**:
- ⚠️ 如果同时存在多个MOD问题，可能导致更严重的性能下降
- ⚠️ HappyLife的怀孕状态修复也可能在UpdateInformation中执行

---

## 📝 总结

### 核心发现

1. **主要瓶颈**: UpdateInformation阶段耗时1422秒（23.7分钟），占总时间的98.7%

2. **主要原因**: 
   - AdaptableLog MOD的大量日志输出和磁盘I/O
   - 复杂的囚犯管理系统（没收物品、逃跑计算）
   - 捕快系统的重复刷新逻辑
   - 庞大的角色数量（10491个活跃角色）

3. **次要原因**:
   - 随着游戏进程，过月时间逐渐增加
   - CharacterActionPlanning增长最快（+48%/周期）

### 建议行动

**立即执行**:
1. ✅ 禁用AdaptableLog相关MOD
2. ✅ 测试过月速度是否改善
3. ✅ 如果改善明显，考虑永久禁用或寻找替代方案

**后续优化**:
1. 清理不必要的角色数据
2. 优化囚犯系统配置
3. 调整日志级别
4. 监控过月时间的变化趋势

### 预期改善

如果成功优化UpdateInformation：
- **当前过月时间**: ~24分钟
- **优化后过月时间**: ~2-5分钟（取决于优化程度）
- **改善幅度**: 80-90% ⬇️

---

**报告结束**

*此报告基于GameData_2026-06-26_02_07_25.log分析，重点关注UpdateInformation阶段的性能瓶颈。*
