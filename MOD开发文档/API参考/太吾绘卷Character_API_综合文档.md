# 太吾绘卷Character API - 基于反编译源码和dnSpy分析

## 📅 创建时间
2026-06-19

## 🎯 数据来源

本文档基于以下来源综合整理：
1. **dnSpyEx直接分析**：Assembly-CSharp.dll中的Character相关API ⭐主要来源
2. **反编译源码**：TaiwuDecompiler_New反编译结果（Domain Method接口）
3. **谷中密友团MOD**：实际使用的API参考和验证
4. **官方MOD API文档**：ECharacterPropertyReferencedType枚举定义

### ️ **重要说明：反编译源码的局限性**

通过TaiwuDecompiler_New反编译的源码中：
- ✅ **包含**：CharacterDomainMethod.Call中的各种GM命令和方法调用接口
- ❌ **不包含**：Character数据类的完整定义（字段、属性、实例方法）

**原因**：
- Character类可能是Unity序列化数据类，其实现细节在DLL中
- ILSpy v11.0.0可能跳过了某些编译器生成的类型
- 实际的Character实例方法需要通过dnSpyEx直接查看DLL

**解决方案**：
- 使用dnSpyEx加载Assembly-CSharp.dll直接查看API
- 参考本文档中已整理的API列表
- 结合谷中密友团等现有MOD的实现

---

## ⚠️ **重要说明**

### 反编译源码的限制

通过TaiwuDecompiler_New反编译的源码中：
- ✅ **包含**：CharacterDomainMethod.Call中的各种GM命令和方法调用接口
- ❌ **不包含**：Character数据类的完整定义（字段、属性、实例方法）

**原因**：
- Character类可能是Unity序列化数据类，其实现细节在DLL中
- 反编译工具主要生成了Domain Method的调用接口
- 实际的Character实例方法需要通过dnSpyEx直接查看DLL

### 推荐的API查找方式

1. **快速查看public方法**：使用dnSpyEx加载Assembly-CSharp.dll
2. **查看Domain Method接口**：查看反编译源码中的CharacterDomainMethod.cs
3. **参考现有MOD**：查看谷中密友团等项目的实际用法

---

## 📋 **Character API分类**

### 1. Domain Method调用接口（来自反编译源码）

这些是通过`CharacterDomainMethod.Call`调用的方法，用于前后端通信：

#### 年龄相关
```csharp
// 获取显示年龄
public static void GetDisplayingAge(int listenerId, int charId)

// 获取生理年龄
public static void GetPhysiologicalAge(int listenerId, int charId)

// 获取生理年龄影响因子
public static void GetPhysiologicalAgeAffector(int listenerId, int charId)
```

#### 特性相关
```csharp
// GM命令：添加单个特性
public static void GmCmd_AddFeature(int charId, short templateId)

// GM命令：设置特性列表
public static void GmCmd_SetFeatures(int charId, List<short> features)
```

#### 其他GM命令
```csharp
// 设置已学会的技艺
public static void GmCmd_SetLearnedLifeSkills(int charId, List<LifeSkillItem> learnedLifeSkills)

// 设置当前内力
public static void GmCmd_SetCurrNeili(int charId, int value)

// 设置基础内力五行比例
public static void GmCmd_SetCharBaseNeiliProportionOfFiveElements(int charId, NeiliProportionOfFiveElements fiveElements)

// 设置冒险性格
public static void GmCmd_SetAdventurePersonalities(int charId, int[] personalities)
```

---

### 2. Character实例方法（来自dnSpy分析）

这些是Character对象上的实例方法，需要通过反射或直接调用：

#### 魅力相关 ✅
```csharp
// 获取魅力值
public int GetAttraction()

// 设置魅力值
public void SetAttraction(int value)
```

**dnSpy验证结果**：
- ✅ 在Assembly-CSharp.dll中搜索`GetAttraction`找到相关成员
- ✅ 发现`Attraction`字段及其getter/setter方法
- ✅ 所属类可能是`CharacterDataMonitor.DetailInfoMonitor`或类似监控器类
- ⚠️ 具体类名需要进一步确认（请在dnSpy中双击查看）

**使用示例**：
```csharp
int attraction = character.GetAttraction();
character.SetAttraction(90);
```

---

#### 资质相关 ✅
```csharp
// 同步玩家资质到角色
public void SyncTalentsFromPlayer()

// 获取功法资质
public List<CombatSkillQualification> GetCombatSkillQualifications()

// 设置基础功法资质
public void SetBaseCombatSkillQualifications(ref List<CombatSkillQualification> qualifications, DataContext context)

// 获取技艺资质
public List<LifeSkillQualification> GetLifeSkillQualifications()

// 设置基础技艺资质
public void SetBaseLifeSkillQualifications(ref List<LifeSkillQualification> qualifications, DataContext context)
```

**使用示例**：
```csharp
// 同步资质
character.SyncTalentsFromPlayer();

// 复制功法资质
var combatSkills = protagonist.GetCombatSkillQualifications();
friend.SetBaseCombatSkillQualifications(ref combatSkills, context);
```

---

#### 特性相关 ✅
```csharp
// 批量添加特性
public void AddFeatures(List<int> features)

// 检查是否拥有特性
public bool HasFeature(int featureId)

// 获取特性列表（推测存在，需验证）
public List<int> GetFeatures()
```

**使用示例**：
```csharp
// 批量添加特性
var features = new List<int> { 1, 2, 3 };
character.AddFeatures(features);

// 检查特性
if (character.HasFeature(1))
{
    Console.WriteLine("拥有特性1");
}
```

---

#### 姓名相关 ✅
```csharp
// 获取完整姓名对象
public FullName GetFullName()

// 姓名字段（可读写）
public string Name { get; set; }
```

**FullName结构**：
```csharp
public struct FullName
{
    public int Type;              // 姓名类型标志
    public int CustomSurnameId;   // 自定义姓氏ID
    public int CustomGivenNameId; // 自定义名字ID
    public short SurnameId;       // 标准姓氏ID
    public short GivenNameId;     // 标准名字ID
}
```

**使用示例**：
```csharp
var fullName = character.GetFullName();
fullName.CustomSurnameId = DomainManager.World.RegisterCustomText(context, "张");
fullName.CustomGivenNameId = DomainManager.World.RegisterCustomText(context, "三丰");
```

---

#### 性别相关 ✅
```csharp
// 获取性别
public sbyte GetGender()
// 返回值：0=男，1=女
```

**使用示例**：
```csharp
sbyte gender = character.GetGender();
bool isFemale = (gender == 1);
```

---

#### 基础属性相关 ⚠️
```csharp
// 设置主要属性（private方法，需要反射）
private void SetMainAttributes(int index, int value)
```

**属性索引对照**：
- 0: 膂力
- 1: 体质
- 2: 灵敏
- 3: 根骨
- 4: 悟性
- 5: 定力
- 6: 魅力（建议使用SetAttraction）

**使用示例**（通过反射）：
```csharp
using System.Reflection;

var method = typeof(Character).GetMethod("SetMainAttributes", 
    BindingFlags.NonPublic | BindingFlags.Instance);
    
if (method != null)
{
    method.Invoke(character, new object[] { 2, 80 }); // 设置灵敏为80
}
```

---

## 🔍 **CloseFriendsEnhanced项目API使用情况**

### ✅ 正确使用的API

| API | 位置 | 状态 |
|-----|------|------|
| `GetGender()` | BackendEntry.cs:80, 137 | ✅ 正确 |
| `GetFullName()` | BackendEntry.cs:163 | ✅ 正确 |
| `GetCombatSkillQualifications()` | BackendEntry.cs:195 | ✅ 正确 |
| `SetBaseCombatSkillQualifications()` | BackendEntry.cs:196 | ✅ 正确 |
| `GetLifeSkillQualifications()` | BackendEntry.cs:199 | ✅ 正确 |
| `SetBaseLifeSkillQualifications()` | BackendEntry.cs:200 | ✅ 正确 |

### ⚠️ 缺失或未实现的API

| API | 优先级 | 说明 |
|-----|--------|------|
| `AddFeatures()` | 🔴 高 | 特性同步未实现（BackendEntry.cs:214-218） |
| `SetAttraction()/GetAttraction()` | 🟡 中 | 魅力同步缺失 |
| `SetMainAttributes()` | 🟡 中 | 基础属性同步被注释 |
| Age设置 | 🟡 中 | CloseFriendAge配置未使用 |

### 💡 修复建议

#### 1. 实现特性同步（高优先级）

```csharp
private static void SyncFeaturesToFriend(Character friend, Character protagonist, DataContext context)
{
    try
    {
        // 需要确认GetFeatures()方法是否存在
        // 如果不存在，可能需要其他方式获取特性列表
        // 或者使用GmCmd_SetFeatures通过DomainMethod调用
        
        var protagonistFeatures = protagonist.GetFeatures(); // 假设有这个方法
        
        if (protagonistFeatures != null && protagonistFeatures.Count > 0)
        {
            friend.AddFeatures(protagonistFeatures);
            Logger.Info("[CloseFriendsEnhanced] Features synced: {0} features", protagonistFeatures.Count);
        }
    }
    catch (Exception ex)
    {
        Logger.Warn(ex, "[CloseFriendsEnhanced] Failed to sync features");
    }
}
```

#### 2. 添加魅力同步（中优先级）

```csharp
// 在SyncAttributesAndQualifications方法中添加
int protagonistAttraction = protagonist.GetAttraction();
friend.SetAttraction(protagonistAttraction);
Logger.Info("[CloseFriendsEnhanced] Attraction synced: {0}", protagonistAttraction);
```

---

## 🛠️ **使用建议**

### 1. 如何在dnSpy中确认API

**步骤1：打开Assembly-CSharp.dll**
- 启动dnSpyEx
- 文件 → 打开 → 选择`Assembly-CSharp.dll`

**步骤2：搜索API**
- 按 `Ctrl+Shift+F` 打开搜索
- 搜索：`GetAttraction` 或 `SetAttraction`
- 查看搜索结果中的类名和方法签名

**步骤3：查看完整信息**
- 双击搜索结果中的类
- 查看右侧窗口的Methods列表
- 确认方法的访问级别（public/private）和参数

**步骤4：复制完全限定名**
- 右键点击类名 → "复制完全限定名"
- 得到完整的命名空间路径，如：`GameData.Domains.Character.Character`

### 2. 何时使用Domain Method vs 实例方法

**Domain Method.Call**：
- 用于前后端通信
- 需要listenerId接收回调
- 通常是异步操作
- 示例：`CharacterDomainMethod.Call.GetDisplayingAge(listenerId, charId)`

**实例方法**：
- 直接在Character对象上调用
- 同步操作，立即返回结果
- 示例：`character.GetAttraction()`

### 2. 反射调用private方法

对于`SetMainAttributes`等private方法：

```csharp
using System.Reflection;

var method = typeof(Character).GetMethod("SetMainAttributes", 
    BindingFlags.NonPublic | BindingFlags.Instance);
    
if (method != null)
{
    method.Invoke(character, new object[] { index, value });
}
else
{
    Logger.Warn("SetMainAttributes method not found");
}
```

### 3. 错误处理

始终添加空值检查和异常处理：

```csharp
if (character != null)
{
    try
    {
        character.SetAttraction(90);
    }
    catch (Exception ex)
    {
        Logger.Error($"设置魅力失败: {ex.Message}");
    }
}
```

---

## 🔗 **相关资源**

- [Character_API参考.md](Character_API参考.md) - 详细的Character API文档
- [API查找指南.md](API查找指南.md) - 如何使用dnSpyEx查看API
- [CloseFriendsEnhanced_API检查报告.md](CloseFriendsEnhanced_API检查报告.md) - API使用情况检查
- [反编译完成报告.md](../工具使用/反编译完成报告.md) - 反编译结果统计

---

##  **更新记录**

| 日期 | 版本 | 更新内容 |
|------|------|---------||
| 2026-06-19 | 1.1 | 添加dnSpy搜索结果说明，明确反编译源码局限性，添加API查找指南 |
| 2026-06-19 | 1.0 | 初始版本，基于反编译源码和dnSpy分析 |

---

## 🔬 **待验证事项**

以下API需要您在dnSpy中进一步确认：

### **1. Character类的完整命名空间**
- [ ] 确认Character类的完全限定名（如：`GameData.Domains.Character.Character`）
- [ ] 确认是否有多个Character相关类（如CharacterData、CharacterInfo等）

### **2. GetAttraction/SetAttraction的具体位置**
- [ ] 确认这些方法是在Character类中，还是在监控器类中
- [ ] 确认方法的访问级别（public/internal/private）

### **3. AddFeatures/HasFeature的实现**
- [ ] 确认GetFeatures()方法是否存在
- [ ] 确认AddFeatures的参数类型（List<int>还是其他）

### **如何验证**
1. 在dnSpy中搜索方法名
2. 双击打开包含该方法的类
3. 查看类的完整定义和方法列表
4. 记录完全限定名和方法签名

---

**最后更新**: 2026-06-19  
**维护者**: MOD开发团队
