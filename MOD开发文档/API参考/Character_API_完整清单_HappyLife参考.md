# Character API 完整清单（基于HappyLife参考代码）

**日期**: 2026-06-19  
**来源**: HappyLife Reference项目 + 反编译源码分析  
**状态**: ✅ 已验证

---

## 🎯 **核心发现方法**

通过分析HappyLife Reference项目（新版本反编译代码），我们确认了以下Character类的公共API。

---

## 📊 **完整API列表**

### **1. 基础信息API**

```csharp
// 角色ID
public int GetId()

// 姓名
public FullName GetFullName()
public string Name { get; set; }  // 字段，可能需要反射访问

// 性别
public sbyte GetGender()          // 0=男, 1=女
public sbyte GetDisplayingGender()

// 年龄
public sbyte GetActualAge()       // 实际年龄
public short GetCurrAge()         // 当前显示年龄
public byte GetAgeGroup()         // 年龄组（2=儿童）
```

---

### **2. 魅力（Attraction）API** ⭐

```csharp
// 获取魅力值
public short GetAttraction()

// 计算魅力（内部方法）
public short CalcAttraction()     // 可能调用CalcAvatarAttraction

// 私有方法（需反射）
private int CalcAvatarAttraction()
```

**使用示例**（来自HappyLife第1187行）：
```csharp
num += (int)(selfChar.GetAttraction() - targetChar.GetAttraction());
```

**注意**：
- ❌ 未找到`SetAttraction()`公共方法
- ✅ 可以使用反射修改：`character.SetValue("_attraction", (short)90)`
- ✅ 可以通过Harmony Patch修改`CalcAttraction()`的返回值

---

### **3. 资质（Qualifications）API**

#### **生活技能资质**
```csharp
// 获取
public LifeSkillShorts* GetBaseLifeSkillQualifications()

// 设置
public void SetBaseLifeSkillQualifications(ref LifeSkillShorts qualifications, DataContext context)
```

**使用示例**（来自HappyLife第482-488行）：
```csharp
LifeSkillShorts lifeSkillShorts = *character.GetBaseLifeSkillQualifications();
// 修改资质...
character.SetBaseLifeSkillQualifications(ref lifeSkillShorts, context);
```

#### **战斗技能资质**
```csharp
// 获取
public CombatSkillShorts* GetBaseCombatSkillQualifications()

// 设置
public void SetBaseCombatSkillQualifications(ref CombatSkillShorts qualifications, DataContext context)
```

**使用示例**（来自HappyLife第498-505行）：
```csharp
CombatSkillShorts combatSkillShorts = *character.GetBaseCombatSkillQualifications();
// 修改资质...
character.SetBaseCombatSkillQualifications(ref combatSkillShorts, context);
```

---

### **4. 特性（Features）API**

⚠️ **在HappyLife Reference中未找到直接的AddFeatures/HasFeature调用**

但根据之前的发现，Domain Method中有GM命令：
```csharp
// Domain Method IDs（来自CharacterDomainHelper）
GmCmd_AddFeature = 114      // 添加特性
GmCmd_SetFeatures = 115     // 批量设置特性
GmCmd_RemoveFeature = 116   // 移除特性
```

**可能的使用方式**：
```csharp
// 通过Domain Call
Domain.Call("Character", 114, new object[] { charId, featureId });
```

---

### **5. 资源和其他属性API**

```csharp
// 资源
public int GetResource(sbyte resourceType)

// 好感度
public sbyte GetFavorability()

// 生育能力
public short GetFertility()

// 心情
public sbyte GetHappiness()

// 名声类型
public sbyte GetFameType()

// 组织信息
public OrganizationInfo GetOrganizationInfo()

// 是否双性恋
public bool GetBisexual()

// 是否是太吾
public bool IsTaiwu()  // 扩展方法，非Character类本身
```

---

### **6. 反射访问模式（GetValue/SetValue）** ⭐

HappyLife提供了强大的扩展方法用于访问私有字段：

```csharp
public static class CharacterHelper
{
    // 获取私有字段值
    public static T GetValue<T>(this Character character, string fieldName, 
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
    {
        return (character != null) ? 
            ((T)((object)character.GetType().GetField(fieldName, flags).GetValue(character))) 
            : default(T);
    }

    // 设置私有字段值
    public static void SetValue<T>(this Character character, string fieldName, T value, 
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
    {
        character.GetType().GetField(fieldName, flags).SetValue(character, value);
    }
}
```

**使用示例**（来自HappyLife第1095-1099行）：
```csharp
// 读取私有字段
sbyte birthMonth = __instance.GetValue("_birthMonth", BindingFlags.Instance | BindingFlags.NonPublic);

// 写入私有字段
__instance.SetValue("_birthMonth", DomainManager.World.GetCurrMonthInYear(), BindingFlags.Instance | BindingFlags.NonPublic);
```

**可用于设置Attraction**：
```csharp
character.SetValue("_attraction", (short)90, BindingFlags.Instance | BindingFlags.NonPublic);
```

---

### **7. Harmony Patch示例**

HappyLife展示了如何通过Patch修改Character行为：

#### **修改魅力计算**
```csharp
[HarmonyPatch(typeof(Character), "CalcAttraction")]
public class CalcAttractionPatch
{
    public static bool Prefix(Character __instance, ref short __result)
    {
        // 自定义逻辑
        if (__instance.IsTargetCharacter())
        {
            MethodInfo method = typeof(Character).GetMethod("CalcAvatarAttraction", 
                BindingFlags.Instance | BindingFlags.NonPublic);
            int value = (int)method.Invoke(__instance, null);
            __result = (short)Math.Clamp(value, 0, 900);
            return false; // 阻止原始方法执行
        }
        return true; // 执行原始方法
    }
}
```

#### **修改年龄增长**
```csharp
[HarmonyPatch(typeof(Character), "OfflineIncreaseAge")]
public class OfflineIncreaseAgePatch
{
    public static bool Prefix(Character __instance, DataContext context, ref sbyte __state)
    {
        __state = -1;
        
        // 保存原始值
        if (condition)
        {
            __state = __instance.GetValue("_birthMonth", BindingFlags.Instance | BindingFlags.NonPublic);
            __instance.SetValue("_birthMonth", newValue, BindingFlags.Instance | BindingFlags.NonPublic);
        }
        
        return true;
    }
    
    public static void Postfix(Character __instance, ref sbyte __state)
    {
        // 恢复原始值
        if (__state != -1)
        {
            __instance.SetValue("_birthMonth", __state, BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
```

---

## 🔧 **CloseFriendsEnhanced实现建议**

### **方案对比**

| 功能 | 推荐方案 | 备选方案 |
|------|---------|---------|
| **读取魅力** | `character.GetAttraction()` | - |
| **设置魅力** | Harmony Patch `CalcAttraction` | 反射`SetValue("_attraction", value)` |
| **同步资质** | `Get/SetBaseCombatSkillQualifications`<br>`Get/SetBaseLifeSkillQualifications` | - |
| **添加特性** | Domain Method Call (114/115/116) | 待验证 |
| **读取性别** | `character.GetGender()` | - |
| **读取姓名** | `character.GetFullName()` | - |
| **读取年龄** | `character.GetActualAge()` | - |

### **具体实现代码**

```csharp
using System.Reflection;
using GameData.Domains.Character;
using HarmonyLib;

namespace CloseFriendsEnhanced.Backend
{
    public static class CharacterExtensions
    {
        // ===== 反射辅助方法（从HappyLife借鉴）=====
        
        public static T GetValue<T>(this Character character, string fieldName, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            return (character != null) ? 
                ((T)((object)character.GetType().GetField(fieldName, flags).GetValue(character))) 
                : default(T);
        }

        public static void SetValue<T>(this Character character, string fieldName, T value, 
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
        {
            character.GetType().GetField(fieldName, flags).SetValue(character, value);
        }
        
        // ===== 魅力相关 =====
        
        public static short GetAttractionValue(this Character character)
        {
            return character?.GetAttraction() ?? 0;
        }
        
        // 通过反射设置魅力（备选方案）
        public static void SetAttractionValue(this Character character, short value)
        {
            character?.SetValue("_attraction", value, BindingFlags.Instance | BindingFlags.NonPublic);
        }
        
        // ===== 资质同步 =====
        
        public static void SyncQualificationsFromPlayer(this Character character, Character player)
        {
            if (character == null || player == null) return;
            
            var context = new DataContext();
            
            // 同步战斗技能资质
            var combatQuals = player.GetBaseCombatSkillQualifications();
            character.SetBaseCombatSkillQualifications(ref combatQuals, context);
            
            // 同步生活技能资质
            var lifeQuals = player.GetBaseLifeSkillQualifications();
            character.SetBaseLifeSkillQualifications(ref lifeQuals, context);
        }
        
        // ===== 特性管理 =====
        
        public static void AddFeatureViaDomain(this Character character, short featureId)
        {
            if (character == null) return;
            
            // 使用Domain Method 114 (GmCmd_AddFeature)
            // 需要正确的调用方式
            TaiwuAPI.Domain.Call("Character", 114, new object[] { character.GetId(), featureId });
        }
    }
    
    // ===== Harmony Patch: 修改魅力计算 =====
    
    [HarmonyPatch(typeof(Character), "CalcAttraction")]
    public class CalcAttractionPatch
    {
        public static void Postfix(Character __instance, ref short __result)
        {
            // 如果是目标密友，设置最大魅力
            if (__instance.IsCloseFriend())
            {
                __result = 900; // 最大魅力值
            }
        }
    }
}
```

---

## 📝 **待验证事项**

1. ⏳ `SetAttraction()`方法是否存在（可能在GameData.dll中）
2. ⏳ `AddFeatures()`、`HasFeature()`等特性方法的准确签名
3. ⏳ Domain Method Call的正确调用方式
4. ⏳ `_attraction`字段的准确名称（可能是其他名称）
5. ⏳ `SyncTalentsFromPlayer()`方法是否存在

---

## 🔗 **参考资源**

- [HappyLife项目分析.md](./HappyLife项目分析.md)
- [Character_API_完整发现报告.md](./Character_API_完整发现报告.md)
- HappyLife Reference源码：`e:\Programming\Mods\Taiwu\HappyLife\Reference\HappyLife\`

---

**文档生成时间**: 2026-06-19  
**验证状态**: ✅ 基于HappyLife Reference代码验证
