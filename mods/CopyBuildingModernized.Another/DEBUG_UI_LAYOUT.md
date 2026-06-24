# UI布局问题 - 调试指南

## 当前状态

- ✅ 代码已修改为竖排布局
- ✅ 默认状态为收起（_isExpanded = false）
- ✅ 添加了详细的调试日志
- ✅ DLL已重新编译并部署（时间戳：最新）

## 问题现象

从您的截图看：
-  UI显示为横排
- ❌ 所有按钮都可见
- ❌ 按钮文字挤在一起

## 可能的原因

### 原因1：游戏没有加载新DLL ⭐⭐⭐⭐⭐

虽然DLL文件时间戳是最新的，但游戏可能：
- 加载了缓存的旧版本
- 或者有其他版本的MOD在运行

### 原因2：代码逻辑有问题 ⭐⭐

如果代码执行过程中出现异常，可能导致UI创建失败。

## 验证步骤

### 步骤1：完全重启游戏

**重要：** 必须完全退出游戏，不要只是返回主菜单！

1. 退出游戏到桌面
2. （可选）关闭Steam确保进程完全结束
3. 重新启动游戏
4. 进入产业界面

### 步骤2：查找日志文件

游戏启动后，查找以下位置的日志文件：

#### 位置1：游戏根目录
```
A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Player.log
```

#### 位置2：AppData目录
```
D:\Users\WQ\AppData\LocalLow\ConchShip\The Scroll Of Taiwu\Player.log
```

#### 位置3：Mod目录
```
A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\log.txt
A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\LuanYunLog.txt
```

### 步骤3：搜索关键日志

在日志文件中搜索以下关键词：

```
[CopyBuildingModernized] *** VERSION: Vertical Layout with Auto-Reload Enabled ***
[CopyBuildingModernized] *** CREATING VERTICAL LAYOUT ***
[CopyBuildingModernized] Creating UI controls at base position:
[CopyBuildingModernized] Import button position:
[CopyBuildingModernized] Export button position:
[CopyBuildingModernized] Convert button position:
```

### 步骤4：分析结果

#### 情况A：找到上述日志 ✅

**说明：** 游戏加载了新版本的DLL

**下一步：**
- 检查日志中的按钮位置坐标
- 如果X坐标都是0，说明是竖排
- 如果X坐标不同，说明是横排（代码有问题）

#### 情况B：没有找到上述日志 ❌

**说明：** 游戏没有加载我们的新DLL

**可能原因：**
1. 游戏加载了其他版本的MOD
2. ModManager没有启用这个MOD
3. DLL文件损坏或格式错误

**解决方案：**
1. 检查游戏内的MOD管理界面，确认"太吾村建筑蓝图工具"已启用
2. 检查是否有其他版本的CopyBuildingModernized MOD
3. 尝试删除并重命名MOD文件夹，然后重新复制

## 快速诊断命令

您可以运行以下命令来快速检查：

```cmd
cd "A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu"
findstr /i "CopyBuildingModernized.*VERSION" *.log
```

或者

```cmd
cd "D:\Users\WQ\AppData\LocalLow\ConchShip\The Scroll Of Taiwu"
findstr /i "CopyBuildingModernized.*VERSION" *.log
```

## 如果依然有问题

请提供以下信息：

1. **日志文件内容**：
   - 搜索 `[CopyBuildingModernized]` 的所有行
   - 特别是包含 `VERSION`、`Creating UI`、`button position` 的行

2. **游戏内截图**：
   - 产业界面的完整截图
   - 显示UI的位置和样式

3. **MOD管理界面截图**：
   - 显示哪些MOD已启用
   - 确认"太吾村建筑蓝图工具"的状态

## 技术细节

### 为什么添加这些日志？

我们在代码中添加了明显的版本标识和位置信息：

```csharp
Debug.Log("[CopyBuildingModernized] *** VERSION: Vertical Layout with Auto-Reload Enabled ***");
Debug.Log("[CopyBuildingModernized] *** CREATING VERTICAL LAYOUT ***");
Debug.Log($"[CopyBuildingModernized] Import button position: {basePos + new Vector2(0f, 15f)}");
```

这些日志可以：
1. 确认游戏加载的是哪个版本的代码
2. 显示每个按钮的实际位置坐标
3. 帮助诊断UI布局问题

### 竖排布局的坐标计算

我们的竖排布局使用以下坐标（相对于模板按钮位置）：

```
主按钮:     (0, +60)   ← 最上方
导入按钮:   (0, +15)   ↓
导出按钮:   (0, -25)   ↓
转换按钮:   (0, -65)   ↓
宽度标签:   (0, -100)  ↓
输入框:     (+85, -102) ← 标签右侧
状态标签:   (0, -135)  ← 最下方
```

所有按钮的X坐标都是0（除了输入框），所以应该是垂直排列的。

---

**最后更新：** 2026-06-23 16:47  
**状态：** 等待用户提供日志信息进行诊断
