# Codex 归档会话恢复指南

## 📋 概述

本指南说明如何恢复 Codex 中已归档的对话会话。

---

## 🔍 当前状态

**已找到的归档会话：** 7 个

位置：`E:\Programming\IDE\.codex\archived_sessions\`

### **会话列表：**

1. `rollout-2026-06-22T16-45-23-019eee81-...jsonl`
2. `rollout-2026-06-23T21-39-29-019ef4b5-...jsonl`
3. `rollout-2026-06-26T19-43-14-019f03bd-...jsonl`
4. `rollout-2026-06-26T19-43-18-019f03bd-...jsonl`
5. `rollout-2026-06-26T19-43-33-019f03be-...jsonl`
6. `rollout-2026-06-26T22-34-10-019f045a-...jsonl`
7. `rollout-2026-06-26T22-34-33-019f045a-...jsonl`

---

## 🛠️ 恢复方法

### **方法 1: 使用自动化脚本（推荐）**

#### **步骤：**

1. **运行脚本**
   ```bash
   e:\Programming\Mods\Taiwu\restore_codex_archived_sessions.bat
   ```

2. **选择操作**
   ```
   1. Restore ALL archived sessions to active  # 恢复所有
   2. Restore specific session by date         # 按日期恢复
   3. View session details                     # 查看详情
   4. Cancel                                   # 取消
   ```

3. **重启 Codex**
   - 完全关闭 Codex
   - 重新打开
   - 会话将出现在历史列表中

---

### **方法 2: 使用 `codex resume` 命令**

#### **步骤：**

1. **获取 Session ID**
   
   从文件名中提取 UUID，例如：
   ```
   rollout-2026-06-22T16-45-23-019eee81-7656-7110-a2d1-9b3a1ae5f81a.jsonl
                                              ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                              这是 Session ID
   ```

2. **恢复会话**
   ```bash
   codex resume 019eee81-7656-7110-a2d1-9b3a1ae5f81a
   ```

3. **会话将在 Codex 中打开**

---

### **方法 3: 手动移动文件**

#### **步骤：**

1. **确定会话日期**
   
   从文件名提取日期：
   ```
   rollout-2026-06-22T16-45-23-uuid.jsonl
           ^^^^^^^^^^
           2026-06-22
   ```

2. **创建目标目录**
   ```bash
   mkdir "E:\Programming\IDE\.codex\sessions\2026\06\22"
   ```

3. **移动文件**
   ```bash
   move "E:\Programming\IDE\.codex\archived_sessions\rollout-2026-06-22T*.jsonl" "E:\Programming\IDE\.codex\sessions\2026\06\22\"
   ```

4. **重启 Codex**

---

## 📊 会话存储结构

### **活跃会话**
```
E:\Programming\IDE\.codex\sessions\
├── 2026\
│   ├── 06\
│   │   ├── 20\
│   │   │   └── rollout-*.jsonl
│   │   ├── 22\
│   │   └── 26\
```

### **归档会话**
```
E:\Programming\IDE\.codex\archived_sessions\
├── rollout-2026-06-22T*.jsonl
├── rollout-2026-06-23T*.jsonl
└── rollout-2026-06-26T*.jsonl
```

---

## 🔧 高级工具：codex-provider-sync

如果切换了 provider 导致会话不可见，使用此工具：

### **安装：**

```bash
npm install -g git+https://github.com/Dailin521/codex-provider-sync.git
```

### **使用：**

```bash
# 检查状态
codex-provider status

# 同步会话元数据
codex-provider sync

# 从备份恢复
codex-provider restore C:\Users\WQ\.codex\backups_state\provider-sync\<timestamp>
```

---

## ⚠️ 注意事项

### **1. 会话加密内容**

- 如果会话包含 `encrypted_content`
- 跨 provider/account 后可能无法继续对话
- 只能恢复列表可见性

### **2. Desktop 显示限制**

- Codex Desktop 首屏只显示最近 50 条会话
- 旧会话可能需要滚动查看
- CLI `/resume` 可以看到所有会话

### **3. 备份建议**

在操作前备份：
```bash
xcopy "E:\Programming\IDE\.codex\archived_sessions" "D:\Backup\codex_archived" /E
```

---

## 🎯 快速操作

### **恢复所有归档会话：**

```bash
# 运行脚本，选择选项 1
e:\Programming\Mods\Taiwu\restore_codex_archived_sessions.bat
```

### **恢复特定日期的会话：**

```bash
# 运行脚本，选择选项 2
# 输入日期：2026-06-22
```

### **查看会话详情：**

```bash
# 运行脚本，选择选项 3
# 输入文件名或拖拽文件
```

---

## 📝 会话文件格式

每个会话文件是 JSONL 格式：

```jsonl
{"type":"session_meta","payload":{"id":"uuid","cwd":"path","model_provider":"openai"}}
{"type":"message","payload":{...}}
{"type":"response","payload":{...}}
```

**关键字段：**
- `id`: Session UUID（用于 `codex resume`）
- `cwd`: 工作目录
- `model_provider`: 模型提供商

---

## 🔗 相关资源

- [Codex 官方文档](https://platform.openai.com/docs/codex)
- [codex-provider-sync 工具](https://github.com/Dailin521/codex-provider-sync)
- [会话克隆工具](https://github.com/goodnightzsj/codex-session-cloner)

---

**最后更新：** 2026-06-26
