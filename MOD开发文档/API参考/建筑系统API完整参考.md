# 太吾绘卷建筑系统 API 完整参考

**版本：** 1.0  
**更新日期：** 2026年6月22日  
**来源：** GameData.Shared.dll 反编译源码

---

## 📋 目录

1. [概述](#概述)
2. [核心数据结构](#核心数据结构)
3. [BuildingBlockKey](#buildingblockkey)
4. [BuildingBlockData](#buildingblockdata)
5. [ArtisanOrder](#artisanorder)
6. [BuildingResourceOutputSetting](#buildingresourceoutputsetting)
7. [序列化格式详解](#序列化格式详解)
8. [使用示例](#使用示例)
9. [常见问题](#常见问题)

---

## 概述

本文档详细说明了太吾绘卷游戏中建筑系统的核心数据结构和序列化格式。所有信息均从 `GameData.Shared.dll` 反编译源码中提取，确保与游戏完全兼容。

### 适用范围
- MOD 开发中的建筑数据操作
- 存档文件解析和修改
- 外部工具开发（如建筑蓝图编辑器）
- 数据序列化/反序列化

### 命名空间
```csharp
using GameData.Domains.Building;
using GameData.Serializer;
```

---

## 核心数据结构

太吾绘卷的建筑系统由以下核心类型组成：

| 类型 | 说明 | 大小 |
|------|------|------|
| `BuildingBlockKey` | 建筑位置标识符 | 6字节（固定） |
| `BuildingBlockData` | 建筑块数据 | 16字节（固定） |
| `ArtisanOrder` | 工匠订单 | 可变（可扩展格式） |
| `BuildingResourceOutputSetting` | 资源产出设置 | 可变（可扩展格式） |

---

## BuildingBlockKey

**类型：** Struct  
**命名空间：** `GameData.Domains.Building`  
**序列化大小：** 6字节（对齐到8字节）  
**特性：** 固定大小，不可变

### 字段定义

```csharp
public struct BuildingBlockKey
{
    public short AreaId;                // 区域ID
    public short BlockId;               // 地块ID
    public short BuildingBlockIndex;    // 建筑块索引（0-based）
}
```

### 字段说明

| 字段 | 类型 | 偏移 | 大小 | 说明 |
|------|------|------|------|------|
| AreaId | short | 0 | 2字节 | 区域ID（太吾村通常为1） |
| BlockId | short | 2 | 2字节 | 地块ID（太吾村通常为2） |
| BuildingBlockIndex | short | 4 | 2字节 | 建筑块索引（0到Width*Width-1） |

### 特殊值

```csharp
public static readonly BuildingBlockKey Invalid = new BuildingBlockKey(-1, -1, 0);
```

### 序列化方法

#### Serialize
```csharp
public unsafe int Serialize(byte* pData)
{
    *(short*)pData = AreaId;              // 偏移 0
    *(short*)(pData + 2) = BlockId;       // 偏移 2
    *(short*)(pData + 4) = BuildingBlockIndex; // 偏移 4
    return 8; // 6字节对齐到8字节
}
```

#### Deserialize
```csharp
public unsafe int Deserialize(byte* pData)
{
    byte* ptr = pData;
    AreaId = *(short*)ptr;        ptr += 2;
    BlockId = *(short*)ptr;       ptr += 2;
    BuildingBlockIndex = *(short*)ptr; ptr += 2;
    return 8;
}
```

### C# BinaryReader/Writer 示例

```csharp
// 写入
writer.Write(key.AreaId);              // Int16
writer.Write(key.BlockId);             // Int16
writer.Write(key.BuildingBlockIndex);  // Int16

// 读取
var key = new BuildingBlockKey(
    reader.ReadInt16(),   // AreaId
    reader.ReadInt16(),   // BlockId
    reader.ReadInt16()    // BuildingBlockIndex
);
```

### 辅助方法

```csharp
// 判断是否无效
public bool IsInvalid => Equals(Invalid);

// 获取位置
public Location GetLocation()
{
    return new Location(AreaId, BlockId);
}

// 转换为 ulong（用于字典键）
public static explicit operator ulong(BuildingBlockKey value)
{
    return (ulong)(((long)value.AreaId << 32) + 
                   ((long)value.BlockId << 16) + 
                   value.BuildingBlockIndex);
}

// 从 ulong 转换
public static explicit operator BuildingBlockKey(ulong value)
{
    return new BuildingBlockKey(
        (short)(value >> 32),
        (short)(value >> 16),
        (short)value
    );
}
```

---

## BuildingBlockData

**类型：** Class  
**命名空间：** `GameData.Domains.Building`  
**接口：** `ISerializableGameData`  
**序列化大小：** 16字节（固定，实际15字节对齐）  
**特性：** 固定大小，包含建筑的所有状态信息

### 字段定义

```csharp
public class BuildingBlockData : ISerializableGameData
{
    public short BlockIndex;            // 建筑块索引
    public short TemplateId;            // 建筑模板ID
    public sbyte Level;                 // 等级
    public short RootBlockIndex;        // 根格索引
    public sbyte Durability;            // 耐久度
    public bool Maintenance;            // 是否维护
    public sbyte OperationType;         // 操作类型
    public short OperationProgress;     // 操作进度
    public bool OperationStopping;      // 操作是否停止
    public short ShopProgress;          // 商铺进度
}
```

### 字段详细说明

| # | 字段 | 类型 | 偏移 | 大小 | 默认值 | 说明 |
|---|------|------|------|------|--------|------|
| 1 | BlockIndex | short | 0 | 2 | - | 建筑块索引（0-based） |
| 2 | TemplateId | short | 2 | 2 | - | 建筑模板ID（-1=从属格） |
| 3 | Level | sbyte | 4 | 1 | 1 | 建筑等级 |
| 4 | RootBlockIndex | short | 5 | 2 | -1 | 根格索引（2x2建筑用） |
| 5 | Durability | sbyte | 7 | 1 | MaxDurability | 当前耐久度 |
| 6 | Maintenance | bool | 8 | 1 | true | 是否需要维护 |
| 7 | OperationType | sbyte | 9 | 1 | -1 | 操作类型（-1=无） |
| 8 | OperationProgress | short | 10 | 2 | 0 | 操作进度 |
| 9 | OperationStopping | bool | 12 | 1 | false | 操作是否停止 |
| 10 | ShopProgress | short | 13 | 2 | 0 | 商铺进度 |

**总计：** 15字节，对齐到16字节

### 构造函数

```csharp
// 默认构造函数
public BuildingBlockData() { }

// 带参数构造函数
public BuildingBlockData(short blockIndex, short templateId, sbyte level, short rootBlockIndex = -1)
{
    BlockIndex = blockIndex;
    TemplateId = templateId;
    Level = level;
    Durability = (sbyte)((templateId >= 0) ? 
        BuildingBlock.Instance[templateId].MaxDurability : (-1));
    Maintenance = true;
    RootBlockIndex = rootBlockIndex;
    OperationType = -1;
}

// 复制构造函数
public BuildingBlockData(BuildingBlockData other)
{
    BlockIndex = other.BlockIndex;
    TemplateId = other.TemplateId;
    Level = other.Level;
    RootBlockIndex = other.RootBlockIndex;
    Durability = other.Durability;
    Maintenance = other.Maintenance;
    OperationType = other.OperationType;
    OperationProgress = other.OperationProgress;
    OperationStopping = other.OperationStopping;
    ShopProgress = other.ShopProgress;
}
```

### 序列化方法

#### GetSerializedSize
```csharp
public int GetSerializedSize()
{
    int num = 15;
    if (num > 4)
    {
        return (num + 3) / 4 * 4; // 对齐到4的倍数
    }
    return num;
}
// 返回: 16
```

#### Serialize
```csharp
public unsafe int Serialize(byte* pData)
{
    *(short*)pData = BlockIndex;                    // 偏移 0, 2字节
    byte* num = pData + 2;
    *(short*)num = TemplateId;                      // 偏移 2, 2字节
    byte* num2 = num + 2;
    *num2 = (byte)Level;                            // 偏移 4, 1字节
    byte* num3 = num2 + 1;
    *(short*)num3 = RootBlockIndex;                 // 偏移 5, 2字节
    byte* num4 = num3 + 2;
    *num4 = (byte)Durability;                       // 偏移 7, 1字节
    byte* num5 = num4 + 1;
    *num5 = (Maintenance ? (byte)1 : (byte)0);     // 偏移 8, 1字节
    byte* num6 = num5 + 1;
    *num6 = (byte)OperationType;                    // 偏移 9, 1字节
    byte* num7 = num6 + 1;
    *(short*)num7 = OperationProgress;              // 偏移 10, 2字节
    byte* num8 = num7 + 2;
    *num8 = (OperationStopping ? (byte)1 : (byte)0); // 偏移 12, 1字节
    byte* num9 = num8 + 1;
    *(short*)num9 = ShopProgress;                   // 偏移 13, 2字节
    
    int num10 = (int)(num9 + 2 - pData);
    if (num10 > 4)
    {
        return (num10 + 3) / 4 * 4; // 返回 16
    }
    return num10;
}
```

#### Deserialize
```csharp
public unsafe int Deserialize(byte* pData)
{
    byte* ptr = pData;
    BlockIndex = *(short*)ptr;           ptr += 2;  // 偏移 0
    TemplateId = *(short*)ptr;           ptr += 2;  // 偏移 2
    Level = (sbyte)(*ptr);               ptr++;      // 偏移 4
    RootBlockIndex = *(short*)ptr;       ptr += 2;  // 偏移 5
    Durability = (sbyte)(*ptr);          ptr++;      // 偏移 7
    Maintenance = *ptr != 0;             ptr++;      // 偏移 8
    OperationType = (sbyte)(*ptr);       ptr++;      // 偏移 9
    OperationProgress = *(short*)ptr;    ptr += 2;  // 偏移 10
    OperationStopping = *ptr != 0;       ptr++;      // 偏移 12
    ShopProgress = *(short*)ptr;         ptr += 2;  // 偏移 13
    
    int num = (int)(ptr - pData);
    if (num > 4)
    {
        return (num + 3) / 4 * 4; // 返回 16
    }
    return num;
}
```

### C# BinaryReader/Writer 实现

```csharp
// 写入 BuildingBlockData
private static void WriteBuildingBlockData(BinaryWriter writer, BuildingBlockData data)
{
    writer.Write(data.BlockIndex);            // Int16 (2字节)
    writer.Write(data.TemplateId);            // Int16 (2字节)
    writer.Write(data.Level);                 // SByte (1字节)
    writer.Write(data.RootBlockIndex);        // Int16 (2字节)
    writer.Write(data.Durability);            // SByte (1字节)
    writer.Write(data.Maintenance);           // Boolean (1字节)
    writer.Write(data.OperationType);         // SByte (1字节)
    writer.Write(data.OperationProgress);     // Int16 (2字节)
    writer.Write(data.OperationStopping);     // Boolean (1字节)
    writer.Write(data.ShopProgress);          // Int16 (2字节)
    // 总计: 15字节
}

// 读取 BuildingBlockData
private static BuildingBlockData ReadBuildingBlockData(BinaryReader reader)
{
    var data = new BuildingBlockData();
    data.BlockIndex = reader.ReadInt16();         // 2字节
    data.TemplateId = reader.ReadInt16();         // 2字节
    data.Level = reader.ReadSByte();              // 1字节
    data.RootBlockIndex = reader.ReadInt16();     // 2字节
    data.Durability = reader.ReadSByte();         // 1字节
    data.Maintenance = reader.ReadBoolean();      // 1字节
    data.OperationType = reader.ReadSByte();      // 1字节
    data.OperationProgress = reader.ReadInt16();  // 2字节
    data.OperationStopping = reader.ReadBoolean();// 1字节
    data.ShopProgress = reader.ReadInt16();       // 2字节
    return data;
}
```

### 常用方法

```csharp
// 重置数据
public void ResetData(short templateId, sbyte level = 1, short rootBlockIndex = -1)
{
    TemplateId = templateId;
    Level = level;
    RootBlockIndex = rootBlockIndex;
    Durability = (sbyte)((templateId >= 0) ? 
        BuildingBlock.Instance[templateId].MaxDurability : (-1));
    Maintenance = true;
    OperationType = -1;
    OperationProgress = 0;
    OperationStopping = false;
}

// 克隆
public BuildingBlockData Clone()
{
    return new BuildingBlockData(this);
}

// 判断是否可用
public bool CanUse()
{
    return OperationType != 0;
}

// 判断是否为2x2建筑的从属格
public bool IsSupportingBlock => TemplateId == -1;

// 判断是否有操作在进行
public bool HasOperation => OperationType != -1 && !OperationStopping;
```

---

## ArtisanOrder

**类型：** Class  
**命名空间：** `GameData.Domains.Building`  
**接口：** `ISerializableGameData`  
**特性：** `[AutoGenerateSerializableGameData(IsExtensible = true)]`  
**序列化大小：** 可变（可扩展格式）

### 字段定义

```csharp
public class ArtisanOrder : ISerializableGameData
{
    public BuildingBlockKey BuildingBlockKey;      // 字段索引 0
    public int ArtisanId;                          // 字段索引 1
    public int SubscriberId;                       // 字段索引 2
    public short ItemSubType;                      // 字段索引 3
    public sbyte LifeSkillType;                    // 字段索引 4
    public int Progress;                           // 字段索引 5
    public int StorageType;                        // 字段索引 6
    public Dictionary<Production, int> ProductionWeight; // 字段索引 7
    public bool IsDebateWon;                       // 字段索引 8
    public int ProgressDelta;                      // 字段索引 9
    public int DebateCount;                        // 字段索引 10
    public int ProgressBaseDelta;                  // 字段索引 11
}
```

### 字段说明

| 索引 | 字段 | 类型 | 大小 | 默认值 | 说明 |
|------|------|------|------|--------|------|
| 0 | BuildingBlockKey | struct | 6 | Invalid | 建筑位置 |
| 1 | ArtisanId | int | 4 | -1 | 工匠ID |
| 2 | SubscriberId | int | 4 | -1 | 订阅者ID |
| 3 | ItemSubType | short | 2 | -1 | 物品子类型 |
| 4 | LifeSkillType | sbyte | 1 | 0 | 生活技能类型 |
| 5 | Progress | int | 4 | 0 | 进度 |
| 6 | StorageType | int | 4 | 2 | 存储类型 |
| 7 | ProductionWeight | dict | 可变 | - | 生产权重 |
| 8 | IsDebateWon | bool | 1 | false | 辩论胜利 |
| 9 | ProgressDelta | int | 4 | 0 | 进度增量 |
| 10 | DebateCount | int | 4 | 0 | 辩论次数 |
| 11 | ProgressBaseDelta | int | 4 | 0 | 基础进度增量 |

### 可扩展格式说明

ArtisanOrder 使用**可扩展格式**，序列化时先写字段数量，再按顺序写字段值。这样设计的好处是：
- 游戏更新添加新字段时，旧版本仍能正确读取
- 可以跳过不认识的字段

**序列化格式：**
```
[FieldCount: ushort] + [Field1] + [Field2] + ... + [FieldN]
```

### 序列化方法

#### Serialize
```csharp
public unsafe int Serialize(byte* pData)
{
    byte* ptr = pData;
    
    // 第1步：写入字段数量（12个字段）
    *(ushort*)ptr = 12;
    ptr += 2;
    
    // 第2步：写入 BuildingBlockKey（6字节）
    ptr += BuildingBlockKey.Serialize(ptr);
    
    // 第3步：写入简单字段
    *(int*)ptr = ArtisanId;        ptr += 4;
    *(int*)ptr = SubscriberId;     ptr += 4;
    *(short*)ptr = ItemSubType;    ptr += 2;
    *ptr = (byte)LifeSkillType;    ptr++;
    *(int*)ptr = Progress;         ptr += 4;
    *(int*)ptr = StorageType;      ptr += 4;
    
    // 第4步：写入字典 ProductionWeight
    if (ProductionWeight != null)
    {
        *(int*)ptr = ProductionWeight.Count;  ptr += 4;
        foreach (var item in ProductionWeight)
        {
            ptr += item.Key.Serialize(ptr);   // Production struct
            *(int*)ptr = item.Value;          ptr += 4;
        }
    }
    else
    {
        *(int*)ptr = 0;  ptr += 4;
    }
    
    // 第5步：写入剩余字段
    *ptr = (IsDebateWon ? (byte)1 : (byte)0);  ptr++;
    *(int*)ptr = ProgressDelta;     ptr += 4;
    *(int*)ptr = DebateCount;       ptr += 4;
    *(int*)ptr = ProgressBaseDelta; ptr += 4;
    
    return (int)(ptr - pData);
}
```

#### Deserialize
```csharp
public unsafe int Deserialize(byte* pData)
{
    byte* ptr = pData;
    ushort num = *(ushort*)ptr;  // 读取字段数量
    ptr += 2;
    
    if (num > 0)  ptr += BuildingBlockKey.Deserialize(ptr);
    if (num > 1)  { ArtisanId = *(int*)ptr; ptr += 4; }
    if (num > 2)  { SubscriberId = *(int*)ptr; ptr += 4; }
    if (num > 3)  { ItemSubType = *(short*)ptr; ptr += 2; }
    if (num > 4)  { LifeSkillType = (sbyte)(*ptr); ptr++; }
    if (num > 5)  { Progress = *(int*)ptr; ptr += 4; }
    if (num > 6)  { StorageType = *(int*)ptr; ptr += 4; }
    
    if (num > 7)
    {
        int dictCount = *(int*)ptr; ptr += 4;
        if (dictCount > 0)
        {
            if (ProductionWeight == null)
                ProductionWeight = new Dictionary<Production, int>();
            else
                ProductionWeight.Clear();
                
            for (int i = 0; i < dictCount; i++)
            {
                Production key = default(Production);
                ptr += key.Deserialize(ptr);
                int value = *(int*)ptr; ptr += 4;
                ProductionWeight.Add(key, value);
            }
        }
    }
    
    if (num > 8)  { IsDebateWon = *ptr != 0; ptr++; }
    if (num > 9)  { ProgressDelta = *(int*)ptr; ptr += 4; }
    if (num > 10) { DebateCount = *(int*)ptr; ptr += 4; }
    if (num > 11) { ProgressBaseDelta = *(int*)ptr; ptr += 4; }
    
    return (int)(ptr - pData);
}
```

### C# 实现示例

```csharp
// 写入 ArtisanOrder
private static void WriteArtisanOrder(BinaryWriter writer, ArtisanOrder order)
{
    if (order == null)
    {
        writer.Write((short)0);  // fieldCount = 0 表示 null
        return;
    }

    writer.Write((short)12);  // 12个字段

    // BuildingBlockKey (6字节)
    writer.Write(order.BuildingBlockKey.AreaId);
    writer.Write(order.BuildingBlockKey.BlockId);
    writer.Write(order.BuildingBlockKey.BuildingBlockIndex);

    // 简单字段
    writer.Write(order.ArtisanId);
    writer.Write(order.SubscriberId);
    writer.Write(order.ItemSubType);
    writer.Write(order.LifeSkillType);
    writer.Write(order.Progress);
    writer.Write(order.StorageType);

    // ProductionWeight 字典（简化：暂不支持）
    writer.Write(0);  // Count = 0

    // 剩余字段
    writer.Write(order.IsDebateWon);
    writer.Write(order.ProgressDelta);
    writer.Write(order.DebateCount);
    writer.Write(order.ProgressBaseDelta);
}

// 读取 ArtisanOrder
private static ArtisanOrder ReadArtisanOrder(BinaryReader reader)
{
    short fieldCount = reader.ReadInt16();
    if (fieldCount == 0) return null;

    var order = new ArtisanOrder();

    // BuildingBlockKey
    if (fieldCount > 0)
    {
        order.BuildingBlockKey = new BuildingBlockKey(
            reader.ReadInt16(),
            reader.ReadInt16(),
            reader.ReadInt16()
        );
    }

    // 根据字段数量读取
    if (fieldCount > 1) order.ArtisanId = reader.ReadInt32();
    if (fieldCount > 2) order.SubscriberId = reader.ReadInt32();
    if (fieldCount > 3) order.ItemSubType = reader.ReadInt16();
    if (fieldCount > 4) order.LifeSkillType = reader.ReadSByte();
    if (fieldCount > 5) order.Progress = reader.ReadInt32();
    if (fieldCount > 6) order.StorageType = reader.ReadInt32();

    // ProductionWeight 字典（跳过）
    if (fieldCount > 7)
    {
        int dictCount = reader.ReadInt32();
        // TODO: 读取 Production 字典
        for (int i = 0; i < dictCount; i++)
        {
            reader.BaseStream.Position += 16; // 跳过 Production
            reader.ReadInt32(); // 跳过 weight
        }
    }

    if (fieldCount > 8) order.IsDebateWon = reader.ReadBoolean();
    if (fieldCount > 9) order.ProgressDelta = reader.ReadInt32();
    if (fieldCount > 10) order.DebateCount = reader.ReadInt32();
    if (fieldCount > 11) order.ProgressBaseDelta = reader.ReadInt32();

    return order;
}
```

---

## BuildingResourceOutputSetting

**类型：** Class  
**命名空间：** `GameData.Domains.Building`  
**接口：** `ISerializableGameData`  
**特性：** `[SerializableGameData(IsExtensible = true)]`  
**序列化大小：** 可变（包含两个字典）

### 字段定义

```csharp
public class BuildingResourceOutputSetting : ISerializableGameData
{
    public Dictionary<sbyte, sbyte> ResourceStorage;  // 字段索引 0
    public Dictionary<sbyte, sbyte> ItemStorage;      // 字段索引 1
}
```

### 字段说明

| 索引 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 0 | ResourceStorage | Dictionary\<sbyte, sbyte\> | 资源存储位置 |
| 1 | ItemStorage | Dictionary\<sbyte, sbyte\> | 物品存储位置 |

#### ResourceStorage
- **Key:** 资源类型ID (0-6)
  - 0: 食材
  - 1: 木材
  - 2: 金铁
  - 3: 玉石
  - 4: 织物
  - 5: 药材
  - 6: 银钱
- **Value:** 存储位置
  - -1: 未设置
  - 0: 随身行囊
  - 1: 府库
  - 2: 仓库
  - 3: 货栈

#### ItemStorage
- **Key:** 物品类型ID (-1到5)
- **Value:** 存储位置
  - -2: 未设置
  - 其他值同 ResourceStorage

### 初始化方法

```csharp
public void Init()
{
    InitResourceStorage();
    InitItemStorage();
}

public void InitResourceStorage()
{
    if (ResourceStorage == null)
        ResourceStorage = new Dictionary<sbyte, sbyte>();
    
    ResourceStorage.Clear();
    for (sbyte b = 0; b <= 6; b++)
    {
        ResourceStorage[b] = -1;  // -1表示未设置
    }
}

public void InitItemStorage()
{
    if (ItemStorage == null)
        ItemStorage = new Dictionary<sbyte, sbyte>();
    
    ItemStorage.Clear();
    for (sbyte b = -1; b < 6; b++)
    {
        ItemStorage[b] = -2;  // -2表示未设置
    }
}
```

### 序列化格式

**字典序列化格式：**
```
[Count: int] + [Key1: sbyte][Value1: sbyte][Key2: sbyte][Value2: sbyte]...
```

**完整序列化格式：**
```
[FieldCount: ushort] + [ResourceStorage Dict] + [ItemStorage Dict]
```

### C# 实现示例

```csharp
// 写入字典
private static void WriteSbyteDictionary(BinaryWriter writer, Dictionary<sbyte, sbyte> dict)
{
    if (dict == null)
    {
        writer.Write(0);
        return;
    }

    writer.Write(dict.Count);
    foreach (var kvp in dict)
    {
        writer.Write(kvp.Key);    // sbyte
        writer.Write(kvp.Value);  // sbyte
    }
}

// 读取字典
private static Dictionary<sbyte, sbyte> ReadSbyteDictionary(BinaryReader reader)
{
    int count = reader.ReadInt32();
    var dict = new Dictionary<sbyte, sbyte>(count);

    for (int i = 0; i < count; i++)
    {
        sbyte key = reader.ReadSByte();
        sbyte value = reader.ReadSByte();
        dict[key] = value;
    }

    return dict;
}

// 写入 BuildingResourceOutputSetting
private static void WriteResourceOutput(BinaryWriter writer, BuildingResourceOutputSetting setting)
{
    if (setting == null)
    {
        writer.Write((short)0);
        return;
    }

    writer.Write((short)2);  // 2个字段
    WriteSbyteDictionary(writer, setting.ResourceStorage);
    WriteSbyteDictionary(writer, setting.ItemStorage);
}

// 读取 BuildingResourceOutputSetting
private static BuildingResourceOutputSetting ReadResourceOutput(BinaryReader reader)
{
    short fieldCount = reader.ReadInt16();
    if (fieldCount == 0) return null;

    var setting = new BuildingResourceOutputSetting();

    if (fieldCount > 0)
        setting.ResourceStorage = ReadSbyteDictionary(reader);
    
    if (fieldCount > 1)
        setting.ItemStorage = ReadSbyteDictionary(reader);

    return setting;
}
```

---

## 序列化格式详解

### 二进制布局总览

#### VillageBuildingData 结构
```
偏移  | 内容                  | 类型   | 说明
------+---------------------+--------+------------------
0     | Width               | sbyte  | 村庄宽度
1     | Blocks.Count        | int    | 建筑块数量
1+4   | Blocks entries      | varies | Key + Value
...   | ArtisanOrders.Count | int    | 工匠订单数量
...   | ArtisanOrders       | varies | Key + Value
...   | ResourceOutput.Count| int    | 资源设置数量
...   | ResourceOutput      | varies | Key + Value
...   | CollectType.Count   | int    | 采集类型数量
...   | CollectType         | varies | Key + sbyte
...   | AutoWorkBlocks.Count| int    | 自动工作数量
...   | AutoWorkBlocks      | short[]| 索引列表
...   | AutoSoldBlocks      | short[]| 索引列表
...   | Residence           | short[]| 索引列表
...   | Comfortable         | short[]| 索引列表
```

### 数据类型对照表

| C# 类型 | 大小 | BinaryWriter 方法 | BinaryReader 方法 |
|---------|------|-------------------|-------------------|
| sbyte | 1字节 | Write(sbyte) | ReadSByte() |
| byte | 1字节 | Write(byte) | ReadByte() |
| short | 2字节 | Write(short) | ReadInt16() |
| ushort | 2字节 | Write(ushort) | ReadUInt16() |
| int | 4字节 | Write(int) | ReadInt32() |
| uint | 4字节 | Write(uint) | ReadUInt32() |
| long | 8字节 | Write(long) | ReadInt64() |
| ulong | 8字节 | Write(ulong) | ReadUInt64() |
| bool | 1字节 | Write(bool) | ReadBoolean() |
| float | 4字节 | Write(float) | ReadSingle() |
| double | 8字节 | Write(double) | ReadDouble() |

### 对齐规则

- **基本类型：** 按自然对齐
- **Struct：** 总大小对齐到4的倍数
- **Class：** 先写 size（0表示null），再写数据

---

## 使用示例

### 示例 1：读取 bin 文件

```csharp
using System.IO;
using TaiwuBlueprintEditor.Models;
using TaiwuBlueprintEditor.Serialization;

// 读取 bin 文件
byte[] bytes = File.ReadAllBytes("village.bin");
VillageBuildingData data = BuildingDataSerializer.Deserialize(bytes);

Console.WriteLine($"村庄宽度: {data.Width}");
Console.WriteLine($"建筑数量: {data.Blocks.Count}");

// 遍历所有建筑
foreach (var kvp in data.Blocks)
{
    var key = kvp.Key;
    var block = kvp.Value;
    
    Console.WriteLine($"位置 [{key.BuildingBlockIndex}]: " +
                     $"TemplateId={block.TemplateId}, " +
                     $"Level={block.Level}, " +
                     $"Durability={block.Durability}");
}
```

### 示例 2：修改建筑等级

```csharp
// 找到要修改的建筑
var key = new BuildingBlockKey(1, 2, 10);  // 索引10的建筑

if (data.Blocks.TryGetValue(key, out var block))
{
    // 修改等级
    block.Level = 10;
    
    // 修改耐久
    block.Durability = 100;
    
    // 更新回字典
    data.Blocks[key] = block;
}
```

### 示例 3：添加新建筑

```csharp
// 创建新建筑
var newIndex = 50;
var newKey = new BuildingBlockKey(1, 2, (short)newIndex);
var newBlock = BuildingBlockData.CreateDefault((short)newIndex, 100);
newBlock.Level = 5;

// 添加到字典
data.Blocks[newKey] = newBlock;

// 添加到自动工作列表
data.AutoWorkBlocks.Add((short)newIndex);
```

### 示例 4：保存 bin 文件

```csharp
// 序列化为字节数组
byte[] bytes = BuildingDataSerializer.Serialize(data);

// 保存到文件（自动创建备份）
File.WriteAllBytes("village_new.bin", bytes);
```

### 示例 5：批量操作

```csharp
// 批量升满所有自然资源
foreach (var kvp in data.Blocks)
{
    var block = kvp.Value;
    
    // 判断是否为自然资源（根据 TemplateId）
    if (IsNaturalResource(block.TemplateId))
    {
        block.Level = GetMaxLevel(block.TemplateId);
        data.Blocks[kvp.Key] = block;
    }
}

// 批量开启自动工作
foreach (var kvp in data.Blocks)
{
    var block = kvp.Value;
    
    if (IsShop(block.TemplateId) && !data.AutoWorkBlocks.Contains(kvp.Key.BuildingBlockIndex))
    {
        data.AutoWorkBlocks.Add(kvp.Key.BuildingBlockIndex);
    }
}
```

---

## 常见问题

### Q1: 为什么 BuildingBlockData 是固定大小？

**A:** BuildingBlockData 实现了 `IsSerializedSizeFixed()` 返回 `true`，所有字段都是基本类型，没有可变长度的集合。这使得序列化更高效，可以快速计算任意建筑的偏移量。

### Q2: ArtisanOrder 为什么要用可扩展格式？

**A:** 可扩展格式允许游戏在更新时添加新字段，而不会破坏旧版本的兼容性。读取时通过 `fieldCount` 判断有哪些字段，跳过不认识的字段。

### Q3: 如何判断一个建筑是2x2建筑的从属格？

**A:** 检查 `TemplateId == -1`。2x2建筑由1个根格（TemplateId > 0）和3个从属格（TemplateId = -1）组成。

### Q4: LevelUnlockedFlags 在哪里？

**A:** 在当前版本的 BuildingBlockData 中**没有** LevelUnlockedFlags 字段。等级直接存储在 `Level` 字段中。某些特殊建筑可能通过其他方式处理等级。

### Q5: 如何处理字典序列化？

**A:** 字典的序列化格式为：`[Count: int] + [Key1][Value1][Key2][Value2]...`。先写数量，再依次写键值对。

### Q6: 序列化时需要注意什么？

**A:** 
1. **字段顺序必须严格一致** - 按照 Serialize 方法中的顺序
2. **数据类型必须精确匹配** - sbyte 不能用 byte，short 不能用 int
3. **注意对齐** - Struct 总大小对齐到4的倍数
4. **处理 null 值** - Class 类型先写 size（0表示null）

---

## 附录

### A. 建筑类型枚举

```csharp
public enum EBuildingBlockType
{
    Empty = 0,           // 空地
    NormalResource = 1,  // 普通资源
    SpecialResource = 2, // 特殊资源
    UselessResource = 3, // 废弃资源
    Building = 4,        // 建筑
    MainBuilding = 5,    // 主建筑
}
```

### B. 资源类型映射

| ID | 名称 | 说明 |
|----|------|------|
| 0 | 食材 | 五谷、杂粮、禽畜 |
| 1 | 木材 | 木料、竹枝、藤蔓 |
| 2 | 金铁 | 金银、铜铁 |
| 3 | 玉石 | 玉料、石料 |
| 4 | 织物 | 植物、丝茧、毛皮 |
| 5 | 药材 | 草木、矿石、虫兽药材 |
| 6 | 银钱 | 钱币 |
| 7 | 威望 | 权威值 |

### C. 存储位置枚举

```csharp
public enum TaiwuVillageStorageType
{
    Inventory = 0,  // 随身行囊
    Treasury = 1,   // 府库
    Warehouse = 2,  // 仓库
    Stock = 3,      // 货栈
}
```

---

## 参考资料

- GameData.Shared.dll 反编译源码
- CopyBuildingModernized MOD 源码
- 太吾绘卷官方文档

---

**文档版本：** 1.0  
**最后更新：** 2026年6月22日  
**维护者：** Taiwu MOD 开发社区
