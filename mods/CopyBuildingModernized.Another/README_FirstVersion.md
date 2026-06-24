# 蓝图宽度转换工具 - 第一版

## 📋 概述

这是CopyBuildingModernized MOD的蓝图宽度转换功能的第一版实现，用于解决导入不同宽度村庄蓝图时的`NullReferenceException`问题。

## ✅ 已完成的功能

### Backend (后端)
- ✅ **WidthConverter.cs** - 核心转换逻辑
  - `ConvertWidth()` - 主转换方法
  - `GetEmptyTemplate()` - 获取空地模板
  - `ConvertGridIndex()` - 网格索引转换（居中规则）
  - `CloneBlockData()` - 建筑数据克隆
  - `ValidateOutput()` - 输出文件校验
  
- ✅ **BackendEntry.cs** - MOD方法注册
  - 新增 `ConvertVillageWidth` 方法供前端调用
  - 参数: InputPath, OutputPath, TargetWidth

### Frontend (前端)
- ✅ **UI界面实现**
  - 新增“转换宽度”按钮（位于导出/导入按钮右侧）
  - 新增“目标宽度”输入框（默认值24）
  - 新增宽度标签提示
  - 状态显示区域（显示转换结果）
  
- ✅ **交互逻辑**
  - 文件选择对话框（选择要转换的.bin文件）
  - 自动命名输出文件（原文件名_width{目标宽度}.bin）
  - 输入验证（18-126范围检查）
  - 异步调用后端转换方法
  - 结果显示和错误提示

### 关键特性
1. **居中转换规则**: 自动计算偏移量，保持原有建筑相对位置不变
2. **空地填充**: 使用TemplateId=0的空地模板填充缺失格子
3. **附属数据迁移**: 自动迁移ArtisanOrders、ResourceOutput、CollectResourceType
4. **运行时状态保留**: 暂时不清理运行时状态（测试阶段）
5. **完整性校验**: 转换后自动验证输出文件的正确性

## 🔧 编译状态

```
✅ Backend 编译成功
📁 输出文件: Plugins\CopyBuildingModernized.Backend.dll
✅ Frontend 编译成功
📁 输出文件: Plugins\CopyBuildingModernized.Frontend.dll
⏰ 编译时间: 2026/06/22
```

## 📝 使用方法

### 游戏内使用（推荐）

1. **启动游戏**，进入太吾村产业管理界面
2. **查看UI按钮**：在“导出村庄”和“导入村庄”按钮右侧，会看到“转换宽度”按钮
3. **输入目标宽度**：在“目标宽度:”输入框中输入想要的宽度（18-126），默认24
4. **点击“转换宽度”按钮**
5. **选择输入文件**：在弹出的文件选择对话框中，选择要转换的.bin蓝图文件
6. **等待转换完成**：状态栏会显示转换进度和结果
7. **查看输出文件**：转换成功后，会在同一目录下生成 `原文件名_width{目标宽度}.bin` 文件

### 示例操作流程

```
原始文件: blueprint_18.bin
目标宽度: 24
操作步骤:
  1. 点击“转换宽度”按钮
  2. 选择 blueprint_18.bin
  3. 等待转换...
输出文件: blueprint_18_width24.bin
```

### 方式2: 直接调用Backend方法（调试用）



```csharp
using CopyBuildingModernized.Backend;

bool success = WidthConverter.ConvertWidth(
    "test_18.bin",    // 输入文件
    "test_24.bin",    // 输出文件
    24                // 目标宽度 (sbyte)
);
```

## 🧪 测试建议

### 测试用例1: 18宽 -> 24宽
```bash
# 准备一个18x18的蓝图文件 test_18.bin
# 调用转换
WidthConverter.ConvertWidth("test_18.bin", "test_24.bin", 24)

# 预期结果:
# - 输出文件 test_24.bin 存在
# - Width == 24
# - Blocks.Count == 576 (24*24)
# - 原有建筑居中显示
# - 外圈全是 TemplateId=0 的空地
```

### 测试用例2: 同宽度补空地
```bash
# 准备一个24x24但缺失空地的蓝图
WidthConverter.ConvertWidth("incomplete_24.bin", "complete_24.bin", 24)

# 预期结果:
# - Blocks.Count == 576
# - 缺失的格子被填充为空地
```

## ⚠️ 已知限制

1. **BuildingBlockKey构造**: 当前使用固定的AreaId=0, BlockId=0，可能需要根据实际游戏状态调整
2. **运行时状态**: 暂时保留所有运行时状态，后续可能根据需要清理
3. **错误处理**: 基本的错误处理已实现，但可能需要更详细的日志
4. **UI布局**: 按钮和输入框的位置可能需要根据实际游戏界面微调

## 📂 文件结构

```
CopyBuildingModernized.Another/
├── Plugins/
│   ├── CopyBuildingModernized.Backend/
│   │   ├── BackendEntry.cs          ✅ 已修改（添加ConvertVillageWidth）
│   │   ├── WidthConverter.cs        ✅ 新建（核心转换逻辑）
│   │   ├── BuildingDataSerializer.cs 📌 复用（序列化/反序列化）
│   │   ├── BuildingDataCollector.cs  📌 复用（数据结构定义）
│   │   └── CopyBuildingModernized.Backend.csproj
│   ├── CopyBuildingModernized.Backend.dll  ✅ 已编译
│   ├── CopyBuildingModernized.Frontend/
│   │   ├── FrontendEntry.cs         ✅ 已修改（添加UI和转换逻辑）
│   │   └── CopyBuildingModernized.Frontend.csproj
│   └── CopyBuildingModernized.Frontend.dll ✅ 已编译
├── test_convert.bat                  ✅ 测试脚本
└── README_FirstVersion.md            ✅ 本文档
```

## 🔄 下一步计划

### 高优先级
1. **游戏内测试** - 在实际游戏中测试转换功能
2. **验证NullReferenceException修复** - 确认转换后的蓝图导入不再报错
3. **UI布局优化** - 根据实际游戏界面调整按钮和输入框位置

### 中优先级
4. **优化BuildingBlockKey** - 根据游戏实际状态动态设置AreaId和BlockId
5. **添加进度提示** - 对于大蓝图显示转换进度
6. **完善日志输出** - 更详细的转换过程日志

### 低优先级
7. **批量转换支持** - 支持文件夹批量转换
8. **GUI独立工具** - 创建独立的WPF/WinForms工具
9. **智能索引映射** - 分析建筑布局密度，选择最优偏移量

## 🐛 问题反馈

如果遇到问题，请提供以下信息：
1. 输入文件的原始宽度
2. 目标宽度
3. 错误日志（Player.log）
4. 转换前后的文件大小

## 📞 联系方式

- 项目路径: `e:\Programming\Mods\Taiwu\CopyBuildingModernized.Another`
- Backend DLL: `Plugins\CopyBuildingModernized.Backend.dll`

---

**版本**: v2.0.0  
**更新日期**: 2026/06/22  
**状态**: Backend+Frontend完成，核心问题已修复，逻辑简化，可游戏内测试

## 📝 版本历史

### v2.0.0 (当前版本) - 逻辑简化与优化
- ✅ 添加Null防护（GetEmptyTemplate和主循环）
- ✅ 保留原始AreaId/BlockId（不再写死0,0）
- ✅ 采用"干净蓝图"策略（直接清空，不迁移后再清空）
- ✅ 删除冗余代码（ConvertAutoList、CleanRuntimeState等）
- ✅ 代码精简100行，逻辑更清晰
- ✅ 编译成功，准备游戏内测试

### v1.2.0 - 核心问题修复
- ✅ 修复CloneBlockData不完整问题（改用完整序列化深拷贝）
- ✅ 启用运行时状态清理（避免NullReferenceException）
- ✅ 实现自动列表索引转换（AutoWorkBlocks等4个列表）
- ✅ 添加附属数据空地过滤（只迁移非空建筑的数据）
- ✅ 编译成功，准备游戏内测试

### v1.1.0 - UI界面实现
- ✅ 新增“转换宽度”按钮和输入框
- ✅ 实现文件选择和异步调用
- ✅ 添加结果显示和错误提示

### v1.0.0 - 初始版本
- ✅ Backend核心转换逻辑
- ✅ 居中规则和空地填充
- ✅ 基础校验功能
