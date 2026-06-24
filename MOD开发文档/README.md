# 太吾绘卷MOD开发文档

## 📁 文档结构说明

本文件夹包含了太吾绘卷MOD开发相关的所有文档，按类别组织如下：

---

### **1. API参考/** 
包含游戏API的详细说明和使用示例

**当前文档：**
- [API查找指南.md](API参考/API查找指南.md) - 如何使用dnSpyEx快速查找游戏API
- [Character_领域API_源码确认版.md](API参考/Character_领域API_源码确认版.md) - ⭐推荐：基于 codegraph 和反编译源码确认的 Character 领域 API
- [太吾绘卷Character_API_综合文档.md](API参考/太吾绘卷Character_API_综合文档.md) - 基于反编译源码、dnSpy分析和项目经验的综合API文档
- [Character_API_完整清单_HappyLife参考.md](API参考/Character_API_完整清单_HappyLife参考.md) - HappyLife 参考项目相关 Character API 清单

**适用场景：**
- 查找Character、Item、Combat等类的public方法
- 了解方法签名和参数
- 快速定位需要的API

---

### **2. 项目架构/**
包含MOD项目的架构设计、配置说明和最佳实践

**当前文档：**
- [项目资源分析报告.md](项目架构/项目资源分析报告.md) - 三个参考项目的详细分析
- [资源分析总结.md](项目架构/资源分析总结.md) - 快速参考总结

**适用场景：**
- 学习生产级MOD的配置方式
- 了解前后端分离架构
- 参考.csproj配置模板

---

### **3. 开发教程/**
包含MOD开发的逐步教程和示例

**当前文档：**
- （待添加）

**计划内容：**
- Harmony Patch基础教程
- 前后端通信机制
- UI开发指南
- 数据持久化方法

---

### **4. 工具使用/**
包含开发工具的使用说明和报告

**当前文档：**
- [功能检查报告.md](工具使用/功能检查报告.md) - TaiwuDecompiler_New功能检查
- [反编译完成报告.md](工具使用/反编译完成报告.md) - 太吾绘卷反编译结果报告
- [反编译源码问题分析.md](工具使用/反编译源码问题分析.md) - ⚠️ 详细分析反编译源码的局限性和问题

**适用场景：**
- 了解反编译工具的使用方法
- 查看反编译结果统计
- 工具问题排查

---

### **5. 问题排查/**
包含常见问题的解决方案和调试技巧

**当前文档：**
- [MOD导入与运行时错误诊断流程.md](问题排查/MOD导入与运行时错误诊断流程.md) - ⭐完整诊断流程：从版本确认到修复验证的8步法
- [GetBuildingOperationLeftTime修复报告.md](../CopyBuildingModernized.Another/FIX_REPORT_GetBuildingOperationLeftTime.md) - 具体案例：NullReferenceException的详细修复过程

**适用场景：**
- 蓝图导入崩溃诊断
- 宽度转换异常排查
- Harmony Patch相关问题
- 日志分析方法
- 反编译调试技巧

---

### **6. 最佳实践/**
包含MOD开发的最佳实践和设计模式

**当前文档：**
- （待添加）

**计划内容：**
- 代码组织规范
- 命名约定
- 错误处理策略
- 性能优化技巧
- 兼容性考虑

---

## 📝 文档维护规范

### **创建新文档时：**
1. 确定文档类别
2. 在对应文件夹中创建.md文件
3. 使用清晰的标题和结构
4. 包含代码示例（如适用）
5. 添加相关的链接和引用

### **文档命名规范：**
- 使用中文名称，清晰描述内容
- 避免特殊字符
- 使用下划线或连字符分隔单词
- 例如：`Harmony_Patch基础教程.md`

### **更新现有文档：**
- 在文档顶部添加"最后更新"日期
- 记录重要的变更
- 保持链接有效性

---

## 🔗 相关资源

### **外部资源：**
- [太吾绘卷官方MOD API文档](https://mod-doc.conchship.com.cn/)
- [ILSpy GitHub仓库](https://github.com/icsharpcode/ILSpy)
- [HarmonyLib文档](https://harmony.pardeik.net/)

### **内部资源：**
- [Decompiler_Tools/](../Decompiler_Tools/) - 反编译工具集
- [CloseFriendsEnhanced/](../CloseFriendsEnhanced/) - 当前开发的MOD项目

---

## 📊 文档统计

- **总文档数**: 13个
- **API参考**: 4个
- **项目架构**: 2个
- **开发教程**: 0个（待补充）
- **工具使用**: 3个
- **问题排查**: 2个（✅ 已添加完整诊断流程）
- **最佳实践**: 0个（待补充）

---

**最后更新**: 2026-06-23  
**维护者**: MOD开发团队
