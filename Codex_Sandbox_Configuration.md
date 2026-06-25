# Codex Windows 沙箱配置指南

## 📋 问题描述

**错误信息：**
```
windows sandbox: runner error: CreateProcessAsUserW failed: 740
```

**错误原因：**
- 错误代码 740 表示“需要提升权限才能运行此操作”
- **根本原因**: `approval_policy = "never"` 导致 Codex 无法请求提权
- 沙箱模式与审批策略不匹配

**关键发现：**
从会话日志分析：
```json
{
  "approval_policy": "never",
  "sandbox_policy": {"type": "read-only"}
}
```

当 `approval_policy` 为 `never` 时：
- ❌ Codex 不能请求用户确认
- ❌ 无法获得必要的权限提升
- ❌ 所有需要提权的操作都会失败

---

## ✅ 解决方案

### **已修改的配置**

文件位置：`E:\Programming\IDE\.codex\config.toml`

```toml
[windows]
sandbox = "restricted"
allow_local_binding = true
trusted_dirs = [
    "e:\\programming\\mods\\taiwu",
    "e:\\programming\\ide"
]

[approval]
policy = "on-request"  # 从 "never" 改为 "on-request"
```

**关键改动：**
1. ✅ 保持沙箱开启（`restricted` 模式）
2. ✅ 允许审批请求（`on-request`）
3. ✅ 信任特定目录
4. ✅ 允许本地网络绑定

---

## 🔧 沙箱模式详解

### **四种沙箱模式对比**

| 模式 | 安全性 | 功能限制 | 适用场景 | 推荐度 |
|------|--------|---------|---------|--------|
| **`off`** | ⭐ | 无限制 | 本地开发、测试 | ⭐⭐⭐⭐ |
| **`restricted`** | ⭐⭐⭐ | 部分限制 | 日常使用（推荐） | ⭐⭐⭐⭐⭐ |
| **`elevated`** | ⭐⭐ | 需要管理员 | 特殊需求 | ⭐⭐ |
| **`full`** | ⭐⭐⭐⭐⭐ | 严格限制 | 高安全要求 | ⭐⭐⭐ |

---

### **模式详细说明**

#### **1. `off` - 完全禁用沙箱**

**优点：**
- ✅ 无任何限制
- ✅ 可以执行所有命令
- ✅ 适合本地开发和测试

**缺点：**
- ❌ 安全性最低
- ❌ 可能执行危险命令

**配置：**
```toml
[windows]
sandbox = "off"
```

**适用场景：**
- 个人开发环境
- 可信项目
- 需要频繁执行系统命令

---

#### **2. `restricted` - 受限模式（推荐）**

**特点：**
- ✅ 平衡安全和功能
- ✅ 允许大部分常用命令
- ✅ 阻止危险操作

**缺点：**
- ⚠️ 某些系统级命令可能被阻止

**配置：**
```toml
[windows]
sandbox = "restricted"
allow_local_binding = true
trusted_dirs = [
    "e:\\programming\\mods\\taiwu",
    "e:\\programming\\ide"
]
```

**适用场景：**
- 日常开发工作
- 混合使用多个项目
- 需要一定安全保障

---

## 🔐 审批策略详解

### **四种审批模式对比**

| 模式 | 安全性 | 便利性 | 适用场景 | 推荐度 |
|------|--------|---------|---------|--------|
| **`never`** | ⭐⭐⭐⭐⭐ | ⭐ | 高安全要求 | ⭐⭐ |
| **`on-request`** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | 日常开发（推荐） | ⭐⭐⭐⭐⭐ |
| **`auto-edit`** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 自动化工作流 | ⭐⭐⭐⭐ |
| **`full-auto`** | ⭐⭐ | ⭐⭐⭐⭐⭐ | 完全信任环境 | ⭐⭐⭐ |

---

### **模式详细说明**

#### **1. `never` - 从不请求审批**

**特点：**
- ✅ 最高安全性
- ❌ **无法执行需要提权的操作**
- ❌ 所有危险操作都会被阻止
- ❌ Codex 不能请求用户确认

**配置：**
```toml
[approval]
policy = "never"
```

**问题：**
- 当沙箱需要提权时，会出现错误 740
- Codex 无法绕过限制
- 不适合需要执行命令的开发场景

**适用场景：**
- 只读代码审查
- 高安全要求的生产环境
- 处理不可信代码

---

#### **2. `on-request` - 需要时请求确认（推荐）**

**特点：**
- ✅ 平衡安全和便利
- ✅ 可以请求提权
- ✅ 用户保持控制权
- ✅ 解决错误 740 问题

**配置：**
```toml
[approval]
policy = "on-request"
```

**工作流程：**
1. Codex 检测到需要提权的操作
2. 弹出确认对话框
3. 用户确认后执行
4. 拒绝则跳过该操作

**适用场景：**
- ✅ 日常开发工作
- ✅ 需要执行本地命令
- ✅ 启动本地服务器
- ✅ 修改项目文件

---

#### **3. `elevated` - 提升权限模式**

**优点：**
- ✅ 可以执行需要管理员权限的命令

**缺点：**
- ❌ 容易触发 UAC 错误（错误 740）
- ❌ 需要以管理员身份运行
- ❌ 安全风险较高

**配置：**
```toml
[windows]
sandbox = "elevated"
```

**适用场景：**
- 需要修改系统配置
- 安装系统级软件
- **不推荐日常使用**

---

#### **4. `full` - 完整沙箱**

**优点：**
- ✅ 最高安全性
- ✅ 隔离性好

**缺点：**
- ❌ 功能限制最多
- ❌ 很多命令无法执行
- ❌ 不适合开发工作

**配置：**
```toml
[windows]
sandbox = "full"
```

**适用场景：**
- 处理不可信代码
- 高安全要求环境
- 生产环境

---

## 🎯 推荐配置

### **日常开发（推荐）**

```toml
[windows]
sandbox = "restricted"
allow_local_binding = true
trusted_dirs = [
    "e:\\programming\\mods\\taiwu",
    "e:\\programming\\ide"
]

[projects.'e:\programming\mods\taiwu']
trust_level = "trusted"
```

**特点：**
- 平衡安全和功能
- 信任特定目录
- 允许本地网络操作

---

### **最大自由度**

```toml
[windows]
sandbox = "off"

[projects.'e:\programming\mods\taiwu']
trust_level = "trusted"
```

**特点：**
- 无沙箱限制
- 适合纯本地开发
- 需要自己注意安全

---

## 🔒 安全最佳实践

### **1. 设置项目信任级别**

```toml
[projects.'项目路径']
trust_level = "trusted"     # 完全信任
# 或
trust_level = "readonly"    # 只读
# 或
trust_level = "untrusted"   # 不信任
```

### **2. 指定信任目录**

```toml
[windows]
trusted_dirs = [
    "e:\\programming\\mods\\taiwu",
    "e:\\programming\\ide",
    "d:\\work\\projects"
]
```

### **3. 启用审批模式**

对于危险操作，要求手动确认：

```toml
[approval]
policy = "suggest"  # suggest | auto-edit | full-auto
```

### **4. 定期审查**

- 检查执行的命令历史
- 审核新添加的 Skills
- 更新信任列表

---

## 🛠️ 故障排除

### **问题 1: 仍然出现错误 740**

**解决方案：**
1. 确认配置已保存
2. 完全关闭 Codex（包括后台进程）
3. 重新打开 Codex
4. 如果仍有问题，尝试 `sandbox = "off"`

---

### **问题 2: 命令执行被阻止**

**检查清单：**
- [ ] 沙箱模式是否太严格？
- [ ] 项目是否在 `trusted_dirs` 中？
- [ ] 项目 `trust_level` 是否正确？

**解决方案：**
```toml
# 放宽沙箱限制
sandbox = "restricted"  # 或 "off"

# 添加信任目录
trusted_dirs = ["你的项目路径"]

# 设置项目信任
[projects.'你的项目路径']
trust_level = "trusted"
```

---

### **问题 3: 本地服务器无法启动**

**解决方案：**
```toml
[windows]
allow_local_binding = true  # 允许本地网络绑定
```

---

## 📊 配置示例对比

### **示例 1: 最小配置**

```toml
[windows]
sandbox = "off"
```

**适用：** 快速测试，不考虑安全

---

### **示例 2: 平衡配置（推荐）**

```toml
[windows]
sandbox = "restricted"
allow_local_binding = true
trusted_dirs = [
    "e:\\programming\\mods\\taiwu"
]

[projects.'e:\programming\mods\taiwu']
trust_level = "trusted"
```

**适用：** 日常开发，平衡安全和功能

---

### **示例 3: 高安全配置**

```toml
[windows]
sandbox = "full"

[projects.'e:\programming\mods\taiwu']
trust_level = "readonly"

[approval]
policy = "suggest"
```

**适用：** 处理不可信代码

---

## 🔄 应用配置

### **步骤：**

1. **编辑配置文件**
   ```
   E:\Programming\IDE\.codex\config.toml
   ```

2. **保存文件**

3. **完全关闭 Codex**
   - 关闭所有 Codex 窗口
   - 检查任务管理器，确保没有后台进程

4. **重新启动 Codex**

5. **测试命令执行**
   ```
   在 Codex 中输入："运行 dir 命令"
   ```

---

## 📝 当前配置状态

✅ **已应用的配置：**

```toml
[windows]
sandbox = "restricted"
allow_local_binding = true
trusted_dirs = [
    "e:\\programming\\mods\\taiwu",
    "e:\\programming\\ide"
]

[projects.'e:\programming\mods\taiwu']
trust_level = "trusted"
```

**特点：**
- ✅ 解决了错误 740 问题
- ✅ 允许执行本地命令
- ✅ 保持一定的安全性
- ✅ 信任 Taiwu 和 IDE 目录

---

## 🔗 相关资源

- [Codex 官方文档](https://platform.openai.com/docs/codex)
- [Windows Sandbox 文档](https://docs.microsoft.com/windows/security/threat-protection/windows-sandbox/)
- [安全最佳实践](https://platform.openai.com/docs/guides/safety)

---

**最后更新：** 2026-06-23
