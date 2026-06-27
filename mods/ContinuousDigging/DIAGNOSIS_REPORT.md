# ContinuousDigging MOD 诊断报告

## 1. 项目概述
**目标**：实现“连续挖掘”功能，点击按钮后持续挖宝，直到满足退出条件（地格无宝、精力不足或达到最高品级），而非游戏原版的“出宝即停”。

## 2. 当前进度
- [x] **MOD 框架搭建**：已完成 `Config.lua`、Frontend 项目及构建脚本。
- [x] **自定义 UI 按钮**：成功在 `UI_Bottom` 底部栏注入“连续挖宝”按钮。
- [x] **基础逻辑触发**：点击按钮可调用 `FindTreasureOnce()` 打开单次挖宝界面。
- [ ] **核心循环逻辑**：尚未实现自动连续触发下一次挖掘的 Hook。

## 3. 核心疑难点 (Critical Blockers)

### 3.1 `UI_FindTreasure` 类型查找失败 🔴
这是目前最大的技术障碍。尽管源码确认该类存在且位于全局命名空间，但在运行时通过反射始终无法获取该类型。

*   **尝试过的方案**：
    1.  `Type.GetType("UI_FindTreasure")`：返回 null。
    2.  `Type.GetType("UI_FindTreasure, Assembly-CSharp")`：返回 null。
    3.  遍历 `AppDomain.CurrentDomain.GetAssemblies()` 并调用 `asm.GetType("UI_FindTreasure")`：返回 null。
    4.  Harmony 字符串 Patch `[HarmonyPatch("UI_FindTreasure", "AnimFinalCall")]`：抛出 `Patching exception in method null`。

*   **可能原因**：
    *   **程序集加载时机**：`UI_FindTreasure` 所在的程序集可能在 MOD 初始化时尚未完全加载到当前 AppDomain。
    *   **Unity/Mono 环境差异**：太吾绘卷使用的 Mono 版本可能对反射查找有特殊限制。
    *   **类名混淆/重命名**：虽然反编译显示为 `UI_FindTreasure`，但实际运行时的元数据名称可能存在偏差。

### 3.2 异步循环与状态同步 🟡
即使解决了类型查找问题，实现真正的连续挖掘还需要处理复杂的 UI 状态流：

*   **动画等待**：必须在 `AnimFinalCall`（动画结束回调）中触发下一次挖掘，否则会导致逻辑冲突或 UI 闪烁。
*   **退出条件检测**：
    *   **精力检查**：需要访问 `ExtraDomain` 获取当前角色精力值。
    *   **品级判定**：需要从 `TreasureFindResult` 中提取本次挖出的物品品级并与配置对比。
    *   **地格状态**：需确认地块是否还有剩余宝物。

## 4. 建议的后续突破方向

### 方案 A：延迟 Patch (Late Patching)
不要在 `Initialize()` 时立即 Patch，而是监听 `UiEvents.OnUIElementShow`。当检测到 `FindTreasure` UI 显示时，再尝试动态应用 Patch。

### 方案 B：纯 Traverse 驱动
放弃对 `UI_FindTreasure` 方法的直接 Hook，改为在 `UI_Bottom` 的 `Update` 或定时器中轮询：
1.  检测 `UI_FindTreasure` 是否处于活跃状态。
2.  利用 `Traverse` 读取其私有字段 `_series` 和 `_result`。
3.  如果动画结束且未满足退出条件，通过 `Traverse` 调用其私有方法 `DoRequestFindTreasure()`。

### 方案 C：底层 Domain Method 拦截
绕过前端 UI 逻辑，直接在 Backend 层 Hook `ExtraDomainMethod.Call.FindTreasure`。
*   **优点**：不依赖 UI 类的反射查找。
*   **缺点**：需要处理前后端通信的异步回调，实现复杂度较高。

## 5. 待完善功能清单
1.  **精力消耗监控**：实现 `_enableActionPointCheck` 逻辑。
2.  **品级上限控制**：实现 `_maxGradeLimit` 判定逻辑。
3.  **UI 反馈优化**：在连续挖掘过程中提供简单的日志或 UI 提示（如当前已挖次数）。

---
**生成时间**: 2026-06-26
**诊断人**: Lingma AI Assistant
