# 运行时信息抓取 MOD - 数据文件说明

**生成时间**: 2026-06-23  
**数据来源**: 游戏运行时导出的 JSON/JSONL 文件

---

## 📁 文件列表

本目录包含从太吾绘卷游戏中导出的月度事件和交互选项数据，已转换为 CSV 格式便于分析。

### 1. monthly_events_catalog.csv (480 条记录)

**来源**: `monthly_events_catalog.json`  
**描述**: 游戏中所有月度事件的静态配置目录

#### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| templateId | int | 事件模板 ID（唯一标识） |
| name | string | 事件名称 |
| type | enum | 事件类型：NormalEvent / SpecialEvent / LockedEvent |
| eventGuid | string | 事件 GUID（可能为空） |
| desc | string | 事件描述文本（包含 {0}, {1} 等占位符） |
| parameters | string | 参数列表，用 `|` 分隔（如：Character\|Location） |
| score | int | 事件分数 |
| node | bool | 是否为节点事件 |

#### 使用场景

- **白名单筛选**: 根据 `type` 字段筛选可自动处理的事件（NormalEvent）
- **事件识别**: 通过 `templateId` 或 `eventGuid` 识别特定事件
- **参数分析**: 了解事件需要的参数类型

#### 示例数据

```csv
templateId,name,type,eventGuid,desc,parameters,score,node
0,灵光一闪,NormalEvent,,在阅读{0}时突然灵光一闪。,ItemKey,0,False
10,促织入梦,NormalEvent,8556787a-6a5f-413e-a2f6-56ed2d4330f1,{0}在{1}梦见一只奇异的促织！,Character|Location,0,False
```

---

### 2. interaction_options_catalog.csv (141 条记录)

**来源**: `interaction_options_catalog.json`  
**描述**: 游戏中所有交互选项的静态配置目录

#### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| templateId | int | 选项模板 ID（唯一标识） |
| optionGuid | string | 选项 GUID |
| name | string | 选项名称 |
| interactionType | enum | 交互类型：Talk / Competition / Practice / Intimate / Enemy |
| identityAbility | enum | 身份能力要求 |
| oncePerMonth | bool | 是否每月仅限一次 |
| actionPointCost | int | 行动点消耗 |

#### 使用场景

- **选项分类**: 根据 `interactionType` 分类不同类型的交互
- **成本分析**: 了解各选项的行动点消耗
- **频率控制**: 识别每月限制一次的选项

#### 示例数据

```csv
templateId,optionGuid,name,interactionType,identityAbility,oncePerMonth,actionPointCost
0,b83f08bc-38fb-4510-9aa1-f1b3c88e0aed,见闻闲谈,Talk,Invalid,True,10
7,1803031e-500e-4e0c-b751-beba5a194755,促织决斗,Competition,Invalid,True,30
```

---

### 3. runtime_event_options.csv (30 条记录)

**来源**: `runtime_event_options.jsonl`  
**描述**: 游戏运行时实际触发的事件和可用选项（动态数据）

#### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| timestamp | datetime | 事件触发时间戳 |
| eventGuid | string | 事件 GUID |
| eventContent | string | 事件内容文本 |
| mainCharacterId | int | 主要角色 ID（-1 表示无） |
| targetCharacterId | int | 目标角色 ID（-1 表示无） |
| optionsCount | int | 可用选项数量 |
| optionKeys | string | 选项 Key 列表，用 `|` 分隔 |
| optionContents | string | 选项内容列表，用 `|` 分隔 |
| optionTypes | string | 选项类型列表，用 `|` 分隔 |
| optionStates | string | 选项状态列表，用 `|` 分隔 |
| behaviors | string | 行为列表，用 `|` 分隔 |

#### 使用场景

- **真实事件分析**: 了解游戏中实际会触发哪些事件
- **选项映射**: 将事件 GUID 与可用选项关联
- **自动化策略**: 根据选项数量和状态决定如何处理

#### 示例数据

```csv
timestamp,eventGuid,eventContent,mainCharacterId,targetCharacterId,optionsCount,optionKeys,optionContents,optionTypes,optionStates,behaviors
2026-06-23 23:19:19.972,f52b9fe9-d0bf-4739-916e-019501497e43,破冢而出的"异人"杀至太吾白身前...,-1,-1,1,Option_-638887922, 阁下为了寻我...,-1,0,0
```

---

## 🔧 数据转换

### 转换脚本

```bash
python convert_json_to_csv.py
```

**功能**:
- 读取游戏导出的 JSON/JSONL 文件
- 清洗数据（处理 BOM、转义字符等）
- 转换为 UTF-8 with BOM 格式的 CSV（兼容 Excel）
- 输出到 `data/` 目录

### 源文件位置

```
a:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Dump_out\
├── monthly_events_catalog.json
├── interaction_options_catalog.json
└── runtime_event_options.jsonl
```

### 输出文件位置

```
e:\Programming\Mods\Taiwu\AutoMonthlyEvent\data\
├── monthly_events_catalog.csv
├── interaction_options_catalog.csv
└── runtime_event_options.csv
```

---

## 📊 数据分析建议

### 1. 筛选可自动处理的事件

```python
import pandas as pd

# 读取数据
df = pd.read_csv('monthly_events_catalog.csv')

# 筛选 NormalEvent 类型的事件
normal_events = df[df['type'] == 'NormalEvent']

# 排除重要的剧情事件（根据实际情况调整）
safe_events = normal_events[~normal_events['name'].str.contains('死亡|入魔|入邪|袭击')]

print(f"可安全自动处理的事件: {len(safe_events)} 个")
print(safe_events[['templateId', 'name']].to_string())
```

### 2. 统计事件类型分布

```python
# 统计各类型事件数量
type_counts = df['type'].value_counts()
print(type_counts)

# 输出:
# NormalEvent     350
# SpecialEvent     80
# LockedEvent      50
```

### 3. 分析交互选项成本

```python
options_df = pd.read_csv('interaction_options_catalog.csv')

# 按交互类型分组统计平均行动点消耗
avg_cost = options_df.groupby('interactionType')['actionPointCost'].mean()
print(avg_cost)

# 找出高成本选项
high_cost = options_df[options_df['actionPointCost'] > 30]
print(high_cost[['name', 'actionPointCost']])
```

### 4. 关联运行时事件与静态配置

```python
import pandas as pd

# 读取数据
events_catalog = pd.read_csv('monthly_events_catalog.csv')
runtime_events = pd.read_csv('runtime_event_options.csv')

# 通过 eventGuid 关联
merged = runtime_events.merge(
    events_catalog[['eventGuid', 'name', 'type']], 
    on='eventGuid', 
    how='left'
)

# 查看实际触发的事件类型
print(merged.groupby('type').size())
```

---

## 🎯 下一步行动

### 阶段 1: 数据分析 ✅ 已完成

- [x] 导出静态事件目录
- [x] 导出交互选项目录
- [x] 捕获运行时事件
- [x] 转换为 CSV 格式

### 阶段 2: 白名单制定（进行中）

- [ ] 分析哪些 NormalEvent 可以安全自动处理
- [ ] 标记需要跳过的特殊事件
- [ ] 创建白名单配置文件

### 阶段 3: 实现自动处理器（待开始）

- [ ] 创建 `AutoMonthlyEventProcessor.cs`
- [ ] 实现基于白名单的判断逻辑
- [ ] 集成到 MOD 中

---

## ⚠️ 注意事项

1. **编码格式**: CSV 文件使用 UTF-8 with BOM 编码，确保 Excel 正确显示中文
2. **分隔符**: 多个值使用 `|` 分隔（如 parameters、optionKeys）
3. **换行符**: 文本中的换行符已转义为 `\n`
4. **空值**: 空字符串表示该字段无值
5. **动态数据**: `runtime_event_options.csv` 会随着游戏进行不断增加新记录

---

## 📝 更新日志

- **2026-06-23**: 初始版本，完成 3 个文件的转换
  - monthly_events_catalog.csv: 480 条记录
  - interaction_options_catalog.csv: 141 条记录
  - runtime_event_options.csv: 30 条记录

---

**维护者**: AutoMonthlyEvent MOD 开发团队  
**最后更新**: 2026-06-23
