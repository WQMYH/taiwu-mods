# 太吾绘卷 API 查找指南

## 🎯 目标

快速找到`GameData.Domains.Character.Character`类的所有public方法，用于MOD开发。

---

## ⚡ 最快方案：使用dnSpy（推荐）

### 第1步：下载dnSpy

如果还没有安装dnSpy：
- 下载地址：https://github.com/dnSpy/dnSpy/releases
- 选择最新版本（如dnSpy-net-win64-xxx.zip）
- 解压到任意目录

### 第2步：打开游戏DLL

1. 运行 `dnSpy.exe`
2. 文件 → 打开
3. 导航到：
   ```
   A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\The Scroll of Taiwu_Data\Managed\Assembly-CSharp.dll
   ```
4. 点击打开

### 第3步：查找Character类

在左侧树形结构中导航：
```
Assembly-CSharp
  └─ GameData
      └─ Domains
          └─ Character
              └─ Character (类)
```

或者使用搜索功能（Ctrl+Shift+F）：
- 搜索类型：`Character`
- 范围：当前程序集

### 第4步：查看Public方法

点击`Character`类后，右侧会显示所有成员。

**重点关注的方法类型：**
- ✅ Public实例方法（没有static关键字）
- ✅ Property访问器（get_*/set_*）
- ⚠️ 注意继承自基类的方法

### 第5步：记录关键API

创建笔记，记录以下信息：

```markdown
## Character API

### 属性访问
- GetName() → string
- GetGender() → int  
- GetAge() → int  ← 需要确认
- GetAttraction() → int  ← 需要确认

### 属性设置
- SetName(string name)
- SetBaseMainAttributes(...)  ← 已知存在
- SetAge(int age)  ← 需要确认
- SetAttraction(int value)  ← 需要确认

### 特性管理
- AddFeature(int featureId, ...)  ← 需要确认参数
- RemoveFeature(int featureId)
- HasFeature(int featureId) → bool

### 其他
- GetId() → int
- GetMorality() → int
- ...
```

### 第6步：导出源码（可选）

如果需要完整源码：
1. 右键 `Assembly-CSharp` 
2. 生成项目
3. 选择输出目录
4. 等待完成（可能需要几分钟）

---

## 🔧 备用方案：使用ILSpyCmd + dnSpy脱壳

如果dnSpy无法满足需求（例如需要自动化处理），可以使用这个方案。

### 前提条件

1. ✅ 已编译最新版ILSpy（已完成）
   - 位置：`e:\Programming\Mods\Taiwu\ILSpy_Latest`
   - ILSpyCmd：`ICSharpCode.ILSpyCmd\bin\Release\net10.0\ilspycmd.dll`

2. ⚠️ 需要手动脱壳Assembly-CSharp.dll

### 步骤

#### 1. 用dnSpy脱壳

1. 打开dnSpy
2. 加载 `Assembly-CSharp.dll`
3. 右键 Assembly-CSharp → Save module
4. 保存为 `Assembly-CSharp-Unpacked.dll`

#### 2. 使用ILSpyCmd反编译

```bash
cd e:\Programming\Mods\Taiwu\TaiwuDecompiler_New

# 运行批处理脚本
decompile.bat
```

或者手动执行：

```bash
dotnet e:\Programming\Mods\Taiwu\ILSpy_Latest\ICSharpCode.ILSpyCmd\bin\Release\net10.0\ilspycmd.dll \
  -p \
  -o .\TaiwuDecompiler_Output \
  Assembly-CSharp-Unpacked.dll \
  --nested-directories
```

#### 3. 查看输出

反编译后的源码在 `TaiwuDecompiler_Output` 目录中。

---

## 📋 需要查找的具体API

基于CloseFriendsEnhanced MOD的需求，我们需要确认以下API：

### 1. 年龄相关
```csharp
// 需要确认是否存在
int GetAge();
void SetAge(int age);
```

### 2. 魅力相关
```csharp
// ECharacterPropertyReferencedType.Attraction = 101
int GetAttraction();
void SetAttraction(int value);
// 或者
void SetBaseMainAttributes(ECharacterPropertyReferencedType type, int value);
```

### 3. 特性相关
```csharp
// 需要确认方法签名
void AddFeature(int featureTemplateId, ...);
void RemoveFeature(int featureTemplateId);
bool HasFeature(int featureTemplateId);
List<int> GetFeatures();
```

### 4. 其他可能需要的
```csharp
// 性别
int GetGender();
void SetGender(int gender);

// 姓名
string GetName();
void SetName(string name);

// 资质
int GetPotential(); // 或其他名称
void SetPotential(int value);
```

---

## 💡 提示和技巧

### 1. 使用搜索功能

在dnSpy中按 `Ctrl+Shift+F` 打开搜索：
- 搜索 `GetAge` - 查找年龄相关方法
- 搜索 `Attraction` - 查找魅力相关方法
- 搜索 `Feature` - 查找特性相关方法
- 搜索 `SetBaseMainAttributes` - 查看已知方法的用法

### 2. 查看方法引用

右键方法 → Analyze → Analyze Method
- 可以看到哪些地方调用了这个方法
- 帮助理解方法的用途

### 3. 对比谷中密友团DLL

如果谷中密友团的SuperGoodFriendBackend.dll可用：
1. 用dnSpy打开它
2. 查看CreateFriendPatch.cs
3. 看它如何调用Character的API
4. 复制正确的调用方式

### 4. 参考jianghu-youling的cookbook

文件位置：`e:\Programming\Mods\Taiwu\jianghu-youling\docs\m1-cookbook.md`

这个文档包含了大量已验证的API调用示例。

---

## 🚀 立即可执行的行动

### 现在就开始（5分钟）：

1. ✅ 打开dnSpy
2. ✅ 加载Assembly-CSharp.dll
3. ✅ 导航到Character类
4. ✅ 截图或复制所有public方法列表
5. ✅ 特别关注Age、Attraction、Feature相关方法

### 同时我可以做的：

- 整理jianghu-youling cookbook中的API信息
- 创建API参考文档模板
- 等待您的dnSpy结果来完善文档

---

## ❓ 遇到问题？

如果遇到以下问题：

**Q: dnSpy打不开Assembly-CSharp.dll？**
A: 确保使用的是最新版的dnSpy，旧版可能不支持.NET版本。

**Q: 找不到某个方法？**
A: 可能在基类中，检查Character的继承链。

**Q: 方法签名不清楚？**
A: 双击方法查看实现代码，或者查看调用者。

**Q: 不确定如何使用？**
A: 参考jianghu-youling的cookbook或谷中密友团的实现。

---

**最后更新**: 2026年6月19日  
**工具版本**: ILSpy 11.0.0, dnSpy (最新版)
