# CharacterStudio 人物模板

`preset_profiles.json` 保存只读预设，`UserData/character_profiles.json` 保存游戏内创建的用户模板，
`character_rules.json` 将人物来源映射到模板。
Config 中来源模板为 `@rules` 时读取该路由；填写具体模板 ID 时以 Config 为准。

## 数值模式

- `Keep`：保留人物原值。
- `Minimum`：低于 `Value` 时提高到该值。
- `Override`：统一设为 `Value`。
- `RandomRange`：每项在 `Min` 到 `Max` 间独立生成。

固定数组长度由游戏决定：六维 6 项、技艺资质 16 项、武学资质 14 项。

## 原版人物模板

- `AreaSectByGender`：按太吾村所在地区、门派和性别选择原版模板。
- `Explicit`：使用 `ExplicitTemplateId`；ID 无效时该次创建失败并记录日志。

## 特性规则

- `RemoveNegative`：移除当前配置中 `Type == 2` 或 `Type == 0 && Level < 0` 的非隐藏特性。
- `AddAllPositive`：添加 `Type == 1` 或 `Type == 0 && Level > 0` 的非隐藏特性。
- `AddIds/RemoveIds`：额外显式增删特性。

用户模板可直接在游戏内通过人物工坊面板保存。手动修改 JSON 后，可在面板中选择“重新载入”。
JSON 无法解析时只保留
`vanilla_safe`，避免使用半初始化模板。

## 后续扩展

剧情角色和世界生成角色会增加新的来源类型及匹配条件。本版本不拦截全世界人物创建。
