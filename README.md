# Taiwu MODs - 太吾绘卷 MOD 开发工作区

## 📖 项目简介

基于 [taiwu-mods](https://github.com/Wanxiang-Sanctum/taiwu-mods) 模板的太吾绘卷 MOD 开发工作区，提供标准化的 MOD 创建、构建、打包和发布流程。

**核心特性：**
- ✅ 前后端分离架构（Frontend + Backend）
- ✅ 自动化构建和打包（MSBuild + CLI）
- ✅ GitHub Actions 自动发布
- ✅ Scriban 模板生成项目脚手架
- ✅ NuGet 依赖管理（Taiwu.ModKit.*）
- ✅ 智能体助手辅助开发（Lingma IDE）

---

## 🚀 快速开始

### 前置要求

1. **.NET SDK** - .NET 8.0+ 和 .NET Standard 2.1
2. **Visual Studio 2022** 或 **VS Code**
3. **GitHub Token** - 用于访问私有 NuGet 包
4. **太吾绘卷游戏** - Steam 版本

### 环境配置

#### 1. 设置 GitHub Token

```powershell
# PowerShell（用户级别永久设置）
[System.Environment]::SetEnvironmentVariable("TAIWU_MODKIT_GITHUB_USER", "YourUsername", "User")
[System.Environment]::SetEnvironmentVariable("TAIWU_MODKIT_GITHUB_TOKEN", "ghp_xxx", "User")
```

**验证配置：**
```powershell
.\setup-env.ps1
```

详见：[ENV_SETUP_COMPLETE.md](./ENV_SETUP_COMPLETE.md)

#### 2. 恢复依赖

```powershell
dotnet restore Taiwu.Mods.slnx
```

---

### 创建第一个 MOD

```powershell
# 创建名为 MyFirstMod 的新 MOD
dotnet run --project tools/Taiwu.Mods.Cli -- create-mod --name MyFirstMod
```

**生成位置：** `mods/MyFirstMod/`

**生成的文件：**
```
mods/MyFirstMod/
├── Config.Lua                    # MOD 配置
├── README.md                     # MOD 说明文档
├── DEVELOPMENT.md                # 开发文档
├── Taiwu.Mod.Pack.proj           # 打包配置
└── src/
    ├── Frontend/
    │   ├── MyFirstMod.Frontend.csproj
    │   └── FrontendPlugin.cs
    └── Backend/
        ├── MyFirstMod.Backend.csproj
        └── BackendPlugin.cs
```

---

### 构建和打包

```powershell
# 构建整个解决方案
dotnet build Taiwu.Mods.slnx

# 打包 MOD
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name MyFirstMod
```

**输出位置：** `artifacts/mods/MyFirstMod/`

---

### 部署到游戏

```powershell
# 复制到游戏 Mods 目录
xcopy /E /Y artifacts\mods\MyFirstMod\* "A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mods\MyFirstMod\"
```

---

### 发布到 GitHub Releases

```powershell
git add .
git commit -m "Release v1.0.0"
git tag mods/MyFirstMod/v1.0.0
git push origin master --tags
```

**GitHub Actions 将自动：**
- ✅ 打包 MOD
- ✅ 创建 ZIP 归档
- ✅ 上传到 GitHub Releases

---

## 📁 项目结构

```
e:\Programming\Mods\Taiwu\
│
├── ⚙️ 核心配置文件
│   ├── Taiwu.Mods.slnx              ← 解决方案文件
│   ├── Taiwu.Mods.Paths.props       ← MSBuild 路径配置
│   ├── Directory.Build.props        ← 构建配置
│   ├── Directory.Packages.props     ← NuGet 依赖版本
│   ├── NuGet.config                 ← NuGet 包源配置
│   └── .gitignore                   ← Git 忽略规则
│
├── 🛠️ 开发工具
│   ├── tools/                       ← CLI 工具源码
│   │   └── Taiwu.Mods.Cli/
│   ├── mods/                        ← MOD 项目目录
│   ├── templates/                   ← Scriban 模板
│   ├── shared/                      ← 共享库目录
│   └── artifacts/                   ← 构建输出目录
│
├── 📚 文档系统
│   ├── MOD开发文档/                 ← 开发参考文档
│   │   ├── API参考/                 ← 游戏 API 文档
│   │   ├── 工具使用/                ← 工具使用说明
│   │   ├── 最佳实践/                ← 开发最佳实践
│   │   ├── 问题排查/                ← 常见问题解决方案
│   │   └── 项目架构/                ← 架构设计文档
│   ├── .agents/                     ← 智能体技能文件
│   └── .lingma/                     ← Lingma 子智能体
│
├── 💾 参考和备份
│   ├── sample/                      ← GitHub 原始仓库
│   │   ├── taiwu-mods/
│   │   ├── community-taiwu-mods/
│   │   └── HappyLife/
│   └── spied/                       ← 反编译伪源码
│
├── 🔧 无源码 MOD 操作
│   └── todo-merge/                  ← Mod 合并项目
│
├── 🏗️ 现有 MOD 项目
│   └── CopyBuildingModernized.Another/  ← 建筑蓝图 MOD
│
└── 📄 文档
    ├── README.md                    ← 本文件
    ├── QUICK_START.md               ← 快速开始指南
    ├── CLI_USAGE.md                 ← CLI 工具说明
    ├── PROJECT_STRUCTURE.md         ← 项目结构详解
    ├── ENV_SETUP_COMPLETE.md        ← 环境配置完成报告
    └── DEPLOYMENT_REPORT.md         ← 部署报告
```

详见：[PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md)

---

## 🛠️ 常用命令

### 项目管理

```powershell
# 创建新 MOD
dotnet run --project tools/Taiwu.Mods.Cli -- create-mod --name MyMod

# 创建共享库
dotnet run --project tools/Taiwu.Mods.Cli -- create-shared --name MyCompany.Shared

# 从解决方案移除 MOD（保留文件）
dotnet run --project tools/Taiwu.Mods.Cli -- remove-mod --name MyMod
```

### 构建和打包

```powershell
# 恢复依赖
dotnet restore Taiwu.Mods.slnx

# 构建
dotnet build Taiwu.Mods.slnx

# 打包 MOD
dotnet run --project tools/Taiwu.Mods.Cli -- pack-mod --name MyMod
```

### 发布

```powershell
# 创建发布标签
git tag mods/MyMod/v1.0.0

# 推送并触发自动发布
git push origin master --tags
```

详见：[CLI_USAGE.md](./CLI_USAGE.md)

---

## 🤖 智能体助手

本项目集成了 Lingma IDE 智能体系统，提供三个专业助手：

### 1. taiwu-mod-creator - 项目创建助手

**功能：** 自动生成 MOD 项目脚手架

**使用方式：**
```
使用 taiwu-mod-creator 帮我创建一个名为 BetterUI 的 MOD
```

**位置：** `.lingma/agents/taiwu-mod-creator.md`

---

### 2. taiwu-mod-debugger - 问题诊断助手

**功能：** 使用 8 步完整诊断法定位和解决 MOD 问题

**使用方式：**
```
使用 taiwu-mod-debugger 帮我诊断这个崩溃问题
```

**核心能力：**
- ✅ 双日志对齐分析（Player.log + GameData.log）
- ✅ Harmony Patch 问题诊断
- ✅ 自动更新异常知识库
- ✅ 经典案例参考（GetBuildingOperationLeftTime）

**位置：** `.lingma/agents/taiwu-mod-debugger.md`

---

### 3. taiwu-mod-publisher - 发布助手（待创建）

**功能：** 自动化打包和发布流程

**位置：** `.agents/taiwu-mod-publisher.md`（技能文件已准备）

---

详见：[.agents/README.md](./.agents/README.md)

---

## 📖 文档系统

### MOD开发文档/

完整的太吾绘卷 MOD 开发参考文档，包含：

- **API参考/** - 游戏 API 文档和示例
- **工具使用/** - 开发工具使用说明
- **最佳实践/** - 开发规范和最佳实践
- **问题排查/** - 常见问题和解决方案
- **项目架构/** - 架构设计和模式

详见：[MOD开发文档/README.md](./MOD开发文档/README.md)

---

## 🔗 相关资源

### 官方仓库
- [taiwu-mods](https://github.com/Wanxiang-Sanctum/taiwu-mods) - MOD 模板仓库
- [community-taiwu-mods](https://github.com/Wanxiang-Sanctum/community-taiwu-mods) - 社区 MOD 集合
- [taiwu-modkit](https://github.com/Wanxiang-Sanctum/taiwu-modkit) - MOD Kit 工具集

### 本地文档
- [QUICK_START.md](./QUICK_START.md) - 快速开始指南（推荐新手阅读）
- [CLI_USAGE.md](./CLI_USAGE.md) - CLI 工具详细说明
- [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md) - 项目结构详解
- [ENV_SETUP_COMPLETE.md](./ENV_SETUP_COMPLETE.md) - 环境配置报告
- [DEPLOYMENT_REPORT.md](./DEPLOYMENT_REPORT.md) - 部署报告

### 智能体文档
- [.agents/README.md](./.agents/README.md) - 智能体系统说明
- [.lingma/agents/README.md](./.lingma/agents/README.md) - 子智能体使用指南

---

## ⚠️ 注意事项

### 安全提醒

- **GitHub Token** 已存储在用户级别环境变量，不会提交到 Git
- **不要分享** 完整的 token 或截图包含 token 的内容
- **定期更新** token（如果泄露立即撤销）

### 开发规范

- **命名规范：** PascalCase（如 `MyMod`、`BetterUI`）
- **版本管理：** 语义化版本（Major.Minor.Patch）
- **标签格式：** `mods/ModName/vVersion`
- **代码规范：** 遵循 `.editorconfig` 配置

### 目录约定

- **sample/** - 仅作为参考，不要直接修改
- **spied/** - 反编译伪源码，仅用于查看 API
- **mods/** - 实际开发目录
- **artifacts/** - 构建输出，已加入 .gitignore

---

## 📊 项目统计

| 项目 | 数量 |
|------|------|
| 核心配置文件 | 5 |
| CLI 工具文件 | 10 |
| 模板文件 | 13 |
| MOD 配置 | 8 |
| 文档文件 | 10+ |
| 智能体助手 | 3 |

---

## 📚 相关文档

### 核心文档（根目录）
- [README.md](./README.md) - 项目主文档（本文档）

### 完整文档中心
所有详细文档已整理到 `docs/` 目录：

**📖 快速开始和指南**
- [docs/guides/QUICK_START.md](./docs/guides/QUICK_START.md) - 项目快速开始指南
- [docs/guides/PROJECT_STRUCTURE.md](./docs/guides/PROJECT_STRUCTURE.md) - 项目结构详细说明
- [docs/guides/Codex_Lingma_Skills_Sharing.md](./docs/guides/Codex_Lingma_Skills_Sharing.md) - Codex 和 Lingma Skills 共享指南
- [docs/guides/SKILL_STANDARD_TEMPLATE.md](./docs/guides/SKILL_STANDARD_TEMPLATE.md) - 技能文件标准模板

**🛠️ 工具使用**
- [docs/tools/CLI_USAGE.md](./docs/tools/CLI_USAGE.md) - Taiwu MODs CLI 工具使用说明

**📊 更新报告**
- [docs/reports/DEPLOYMENT_REPORT.md](./docs/reports/DEPLOYMENT_REPORT.md) - taiwu-mods 部署报告
- [docs/reports/DOC_MANAGER_UPGRADE_REPORT.md](./docs/reports/DOC_MANAGER_UPGRADE_REPORT.md) - 文档管理器升级报告
- [docs/reports/FILE_MANAGEMENT_RULES_UPDATE.md](./docs/reports/FILE_MANAGEMENT_RULES_UPDATE.md) - 文件管理规则更新
- [docs/reports/FORCE_TAKEOVER_RULES_UPDATE.md](./docs/reports/FORCE_TAKEOVER_RULES_UPDATE.md) - 强制接管规则更新
- [docs/reports/AGENT_FILES_CLEANUP_REPORT.md](./docs/reports/AGENT_FILES_CLEANUP_REPORT.md) - 智能体文件整理报告

详见：[docs/README.md](./docs/README.md)

---

## 🎯 下一步

1. ✅ 阅读 [docs/guides/QUICK_START.md](./docs/guides/QUICK_START.md) 了解完整流程
2. ✅ 配置 GitHub Token（已完成）
3. ✅ 恢复依赖：`dotnet restore Taiwu.Mods.slnx`
4. ✅ 创建第一个 MOD
5. ✅ 开始编码！

---

**最后更新：** 2026-06-23  
**维护者：** Lingma AI Assistant  
**状态：** ✅ 活跃开发中
