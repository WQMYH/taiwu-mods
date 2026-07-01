# 当前后端兼容基线

扫描日期：2026-06-30。目标为本机当前太吾绘卷 `1.0.40`
（游戏 Build Date `202606272342330829`，Steam build `23957505`）。

## 类型与程序集

| 类型/方法 | 程序集 | 适配方式 |
|---|---|---|
| `CharacterCreation.Create*Attributes/Qualifications` | `GameData.dll` | 对全部当前重载安装结果缩放 Postfix |
| `CharacterCreation.NormalDistribute(IRandomSource,int,int)` | `GameData.dll` | Prefix 调整人物生成跨度 |
| `CharacterDomain.ParallelCreateIntelligentCharacter` | `GameData.dll` | Prefix 固定成长品级，Postfix 应用天才奖励 |
| `Character.LearnNewLifeSkill/LearnNewCombatSkill` | `GameData.dll` | Postfix 为非太吾角色创建秘籍 |
| `Equipping.CalcCombatSkillScore` | `GameData.dll` | 当前原版已包含目标评分项，兼容键保留但不重复加分 |
| `Equipping.OfflineBreakoutCombatSkill` | `GameData.dll` | 线程局部调用范围守卫 |
| `CombatSkillHelper.CalcForceBreakoutInjuriesAndDisorderOfQi` | `GameData.dll` | 仅在 NPC 离线突破范围内跳过 |
| `SettlementCharacter.CalcInfluencePower` | `GameData.dll` | Postfix 根据当前人物强度调整 |
| `ItemTemplateHelper.GetGiftLevel` | `GameData.Shared.dll` | Postfix 覆盖礼物等级 |

## SHA-256

- `GameData.dll`: `A3F6378AFF9EF82F35FB12A73BFB0238322737FB13273AECED93B761B8EFBA45`
- `GameData.Shared.dll`: `754FA85FD7707B71C715B2CAC78E37674A290DA23335E1A46EFAC4048D8F7EAD`
- `GameData.Common.dll`: `752A4BBF643617C135FE75D1D6C65AFF6CBC0E94CAF57BA578FDC09FD4665CBF`
- `GameData.Utilities.dll`: `360D75EDD31FF4D9D5FAA4BACEF1E953E2565B06EB23F78E63E1E9E94E57C175`
- `GameData.Utilities.Structure.dll`: `4A67935BC0D3554F80F220D3C7E46276DF97065536A9BE387682FAFE3493DEAB`

游戏更新后应重新扫描方法签名和哈希。插件本身会逐项记录 `Installed`、`Disabled` 或 `Failed`，目标变化不会阻止其余功能加载。
