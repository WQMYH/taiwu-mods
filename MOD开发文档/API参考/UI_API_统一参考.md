# UI API 统一参考

> 生成依据：`spied/The Scroll of Taiwu/Assembly-CSharp_da99b9b8/UIElement.cs`、`UIBase.cs`、`UIManager.cs`、`EventModel.cs`、`UI_EventWindow.cs`、`UI_MonthNotify.cs`、`GameDataBridge.cs`、`TaiwuEventDisplayData.cs`、`EventOptionInfo.cs`。

## 1. 总览

太吾前端 UI 不是单一 MVC 架构，而是由 `UIElement` 静态注册表、`UIBase` 窗口脚本、`UIManager` 显隐调度、`GameDataBridge` 前后端数据通知、`GEvent/UiEvents` 前端事件共同组成。

```text
后端 Domain / Method
  -> GameDataBridge 通知队列
  -> 前端 listener.OnNotifyGameData(...)
  -> Model 或 UIBase 反序列化数据
  -> GEvent / UiEvents 通知 UI 刷新
  -> UIBase 子类渲染窗口
  -> 玩家点击或 MOD 自动点击
  -> DomainMethod.Call.* 回传后端
```

## 2. 核心接口与入口

| 模块 | 入口 | 用途 | MOD 使用建议 |
| --- | --- | --- | --- |
| UI 注册表 | `UIElement` | 保存所有可管理 UI 的静态入口 | 遍历/定位 UI 时优先查这里 |
| UI 基类 | `UIBase : Refers, IDisplay, IAsyncMethodRequestHandler` | 所有窗口脚本基类 | Patch 具体 UI 的 `OnInit/OnNotifyGameData/OnClick` |
| UI 管理器 | `UIManager.Instance.ShowUI/HideUI` | 统一显示/隐藏 UI | 打开原生窗口时使用 |
| 数据桥 | `GameDataBridge.RegisterListener/AddDataMonitor/AddMethodCall` | 前后端数据同步 | 监听数据变化或请求后端方法 |
| 前端事件 | `GEvent.OnEvent(UiEvents.*)` | UI 内部刷新和广播 | UI 状态变化监听优先查 `UiEvents` |
| 事件模型 | `EventModel.DisplayingEventData` | 当前事件窗口数据 | 自动选择/事件记录的核心数据源 |
| 事件选择 | `EventModel.Select(optionKey)` | 点击当前事件选项 | 自动点击时优先使用，调用前必须验证当前窗口 |

## 3. UIElement 运行时管理

- `UIElement` 是运行时 UI 的权威注册表，本文件共解析到 `
- `UIElement.Name` 来自 `_path` 的最后一段，不一定等同字段名。
- `UIElement.Exist` 表示 `UiBase` 存在且 GameObject active。
- `UIElement.IsShowing` 表示状态机不在 Sleep/AnimateOut/Hiding。
- `UIElement.SetOnInitArgs(ArgumentBox)` 是打开 UI 前传参的常见入口。
- `UIElement.UiBaseAs<T>()` 用于取得具体 UI 脚本实例。
- `UIElement.MonitorData()` 会根据 `UIBase.NeedGameDataListenerId()` 注册 `GameDataBridge` listener。

## 4. UIBase 生命周期

| 方法/字段 | 作用 | 注意事项 |
| --- | --- | --- |
| `OnInit(ArgumentBox argsBox)` | UI 初始化入口 | 具体 UI 必须实现，适合注入按钮/初始化状态 |
| `OnReset()` | UI 重置 | 关闭/重开前后可能触发 |
| `InitMonitorFieldIds()` | 声明要监听的数据字段 | 需要配合 `NeedDataListenerId` 或 monitor fields |
| `OnNotifyGameData(List<NotificationWrapper>)` | 接收后端数据通知 | 数据驱动 UI 的主要 patch 点 |
| `NotifyUIShow/NotifyUIHide` | 广播 UI 显隐事件 | 会触发 `UiEvents.OnUIElementShow/Hide` |
| `NeedGameDataListenerId(...)` | 判断是否需要 listener | `MonitorFields`、`NeedDataListenerId`、`NeedWaitData` 任一命中即可 |
| `AppendMonitorFieldId/RemoveMonitorFieldId` | 动态增加/移除监听字段 | 要避免忘记 unmonitor |

## 5. 事件窗口接口

- `EventModel` 是事件窗口的数据中枢，`Init()` 内注册 `GameDataBridge.RegisterListener(OnNotifyGameData)`。
- `EventModel.OnNotifyGameData` 接收 `DomainId == 12` 的事件域数据。
- `DataId == 21` 会反序列化到 `DisplayingEventData`。
- `DisplayingEventData.EventGuid` 为空时会被置空。
- `RefreshShowingEvent()` 根据 `DisplayingEventData` 决定隐藏或显示 `UIElement.EventWindow`。
- `UI_EventWindow.Data` 实际就是 `EventModel.DisplayingEventData`。
- `EventModel.Select(optionKey)` 会确认当前 `EventOptionInfos` 中存在该 key，然后调用 `TaiwuEventDomainMethod.Call.EventSelect(eventGuid, optionKey)`。
- `UI_EventWindow.SelectOption(...)` 还会额外检查 `CanSelect`、`OptionState == -1`、`Behavior` 与太吾当前行为限制。

### 5.1 TaiwuEventDisplayData

| 字段 | 含义 |
| --- | --- |
| `EventGuid` | 当前事件 GUID |
| `MainCharacter` | 主/左侧角色显示数据 |
| `TargetCharacter` | 目标/右侧角色显示数据 |
| `ExtraData` | 输入、选物、选人、选技能、额外显示数据 |
| `EventContent` | 当前事件正文 |
| `NameDecodeDataList` | 名称解析数据 |
| `ExtraFormatLanguageKeys` | 文本格式化语言键 |
| `EventTexture` | 事件背景图 |
| `MaskControlCode` / `MaskTweenTime` | 遮罩和转场控制 |
| `EscOptionIndex` | Esc/右键默认选项索引 |
| `EventOptionInfos` | 当前可见选项列表 |

### 5.2 EventOptionInfo

| 字段 | 含义 | 自动化注意事项 |
| --- | --- | --- |
| `OptionType` | 选项类型 | 类型 4 会走窗口淡出后选择 |
| `OptionKey` | 回传后端的选项 key | 自动点击必须使用当前窗口存在的 key |
| `OptionContent` | 选项显示文本 | 可作为关键词锚点，但不能单独作为稳定 ID |
| `OptionAvailableConditions` | 可用条件摘要 | 可用于日志解释 |
| `OptionAvailableConditionInfos` | 详细条件 | 可用于后续 Discovery |
| `OptionConsumeInfos` | 选项消耗 | 请求/给予类事件必须记录 |
| `OptionState` | 选项状态 | `-1` 已确认不可选，其他状态需结合实测 |
| `Behavior` | 行为倾向限制 | 走 UI 点击时可能被行为限制拦截 |
| `ExtraFormatLanguageKeys` | 额外格式化语言键 | 用于文本还原 |

## 6. 月度通知 UI

- 月度通知窗口是 `UI_MonthNotify`，入口是 `UIElement.MonthNotify`。
- `OnInit` 会调用 `WorldDomainMethod.Call.GetMonthlyEventCollection(Element.GameDataListenerId)`。
- `OnNotifyGameData` 接收 `MonthlyEventCollection` 与动态参数。
- `UpdateMonthlyEventCollection(...)` 将后端集合转成可渲染月度事件。
- `RefreshMonthlyEventScroll()` 刷新列表、按钮状态、默认处理可用性。
- 单条月度事件点击后调用 `WorldDomainMethod.Call.HandleMonthlyEvent(info.Offset)`。
- `DefaultAll` 会先检查 `HasForbidDefaultEvent()`，存在禁止默认的事件时不能全部默认处理。

## 7. MOD 开发建议

- 查 UI 优先顺序：`UIElement` 字段 -> 对应 `_path` -> 具体 `UI_*` 类 -> `OnInit/OnNotifyGameData/OnClick`。
- 自动事件选择优先 patch `EventModel.OnNotifyGameData` postfix，然后调用 `EventModel.Select(optionKey)`。
- UI 注入按钮优先 patch 具体窗口 `OnInit` 或数据刷新事件，避免在 prefab 未加载时访问控件。
- 单选项自动跳过必须排除 `InputRequestData`、`SelectItemData`、`SelectCharacterData`、`SelectCombatSkillData`、`SelectLifeSkillData`。
- Discovery 工具应记录 `EventGuid/EventContent/EventOptionInfos/ExtraData flags/mainCharacterId/targetCharacterId`。
- 不要只依赖 `UI_MonthNotify` 抓过月事件；很多结果阶段只经过 `EventModel`。

## 8. 全量 UIElement 注册表

| # | 字段名 | prefab/path | 分类 | ServeGroup | 源码行 |
| ---: | --- | --- | --- | --- | ---: |
| 1 | `LogoShow` | `UI_Logo` | Root | `` | 45 |
| 2 | `MainMenu` | `UI_MainMenu` | Root | `` | 51 |
| 3 | `WorldMap` | `UI_Worldmap` | Root | `` | 57 |
| 4 | `Dialog` | `UI_Dialog` | Root | `` | 63 |
| 5 | `TextureShow` | `UI_TextureShow` | Root | `` | 69 |
| 6 | `RecordSelect` | `UI_RecordSelect` | Root | `` | 75 |
| 7 | `Loading` | `UI_Loading` | Root | `` | 81 |
| 8 | `NewGame` | `NewGame/UI_NewGame` | NewGame | `` | 87 |
| 9 | `CatchCricket` | `UI_CatchCricket` | Root | `` | 93 |
| 10 | `CricketCombat` | `UI_CricketCombat` | Root | `` | 99 |
| 11 | `TaiwuVillageStoneRoom` | `Building/UI_TaiwuVillageStoneRoom` | Building | `` | 105 |
| 12 | `PartWorld` | `UI_PartWorldMap` | Root | `` | 110 |
| 13 | `BuildingBlockList` | `Building/UI_BuildingBlockList` | Building | `` | 115 |
| 14 | `ResourceBar` | `UI_ResourceBar` | Root | `` | 121 |
| 15 | `KungfuPracticeRoomPuppet` | `Building/UI_KungfuPracticeRoomPuppet` | Building | `` | 127 |
| 16 | `NewAreaNotify` | `UI_NewAreaNotify` | Root | `` | 132 |
| 17 | `LifeSkillCombatBegin` | `LifeSkillCombat/UI_LifeSkillCombatBegin` | LifeSkillCombat | `` | 138 |
| 18 | `SelectSkill` | `UI_SelectSkill` | Select | `` | 144 |
| 19 | `TutorialVideoPlayer` | `UI_TutorialVideoPlayer` | Root | `` | 149 |
| 20 | `SkillBreakPlate` | `UI_SkillBreakPlate2` | Root | `` | 156 |
| 21 | `SkillBreakBonusSelect` | `SkillBreak/UI_SkillBreakBonusSelect` | SkillBreak | `` | 161 |
| 22 | `Reading` | `Reading/UI_Reading` | Reading | `` | 166 |
| 23 | `SelectItem` | `UI_SelectItem` | Select | `` | 171 |
| 24 | `SelectConfigItem` | `UI_SelectConfigItem` | Select | `` | 176 |
| 25 | `SelectAreaItem` | `UI_SelectAreaItem` | Select | `` | 181 |
| 26 | `MultiSelectItem` | `UI_MultiSelectItem` | Root | `` | 186 |
| 27 | `SelectChicken` | `UI_SelectChicken` | Select | `` | 191 |
| 28 | `AvatarPreset` | `UI_AvatarPreset` | Root | `` | 196 |
| 29 | `Combat` | `UI_Combat` | Combat | `` | 201 |
| 30 | `CombatBackground` | `UI_CombatBackground` | Combat | `` | 207 |
| 31 | `CombatResult` | `UI_CombatResult` | Combat | `` | 212 |
| 32 | `Advance` | `UI_Advance` | Root | `` | 217 |
| 33 | `Bottom` | `UI_Bottom` | Root | `` | 223 |
| 34 | `Adventure` | `UI_Adventure` | Adventure | `` | 229 |
| 35 | `AdvanceConfirm` | `UI_AdvanceConfirm` | Root | `` | 235 |
| 36 | `BuildingArea` | `Building/UI_BuildingArea` | Building | `` | 241 |
| 37 | `BuildingManage` | `Building/UI_BuildingManage` | Building | `` | 247 |
| 38 | `MapBlockCharList` | `UI_MapBlockCharList` | Root | `` | 252 |
| 39 | `ButtonSheet` | `UI_ButtonSheet` | Root | `` | 258 |
| 40 | `DragShow` | `UI_DragShow` | Root | `` | 263 |
| 41 | `SearchResultShow` | `UI_SearchResultShow` | Root | `` | 268 |
| 42 | `CollectResource` | `UI_CollectResource` | Root | `` | 273 |
| 43 | `ChickenMap` | `UI_ChickenMap` | Root | `` | 279 |
| 44 | `GetItem` | `UI_GetItem` | Root | `` | 285 |
| 45 | `SectMainStoryUnlock` | `UI_SectMainStoryUnlock` | Root | `` | 291 |
| 46 | `SelectChar` | `UI_SelectChar` | Select | `` | 297 |
| 47 | `SelectEntertainChar` | `UI_SelectEntertainChar` | Select | `` | 302 |
| 48 | `SelectVillagerChar` | `UI_SelectVillagerChar` | Select | `` | 307 |
| 49 | `SelectVillagerCharInLineage` | `UI_SelectVillagerCharInLineage` | Select | `` | 312 |
| 50 | `SelectVillagerCharWithTotalPersonality` | `UI_SelectVillagerCharWithTotalPersonality` | Select | `` | 317 |
| 51 | `TravelNotification` | `UI_TravelNotification` | Root | `` | 322 |
| 52 | `CombatBegin` | `UI_CombatBegin` | Combat | `` | 327 |
| 53 | `AdventurePrepare` | `UI_AdventurePrepare` | Adventure | `` | 333 |
| 54 | `MakingSystem` | `UI_MakingSystem` | Root | `` | 338 |
| 55 | `EventWindow` | `UI_EventWindow` | Root | `` | 343 |
| 56 | `BlockInteract` | `UI_BlockInteract` | Root | `` | 349 |
| 57 | `VillagerWork` | `UI_VillagerWork` | Villager | `` | 355 |
| 58 | `WorldState` | `UI_WorldState` | Root | `` | 360 |
| 59 | `SystemOption` | `UI_SystemOption` | Root | `` | 366 |
| 60 | `SystemSetting` | `UI_SystemSetting` | Root | `` | 371 |
| 61 | `UpdateLog` | `UI_UpdateLog` | Root | `` | 376 |
| 62 | `Legacy` | `Legacy/UI_Legacy` | Legacy | `` | 381 |
| 63 | `SelectLegacyRewardGroup` | `Legacy/UI_SelectLegacyRewardGroup` | Legacy | `` | 386 |
| 64 | `SelectRandomLegacyReward` | `Legacy/UI_SelectRandomLegacyReward` | Legacy | `` | 391 |
| 65 | `LegacyActivate` | `Legacy/UI_LegacyActivate` | Legacy | `` | 396 |
| 66 | `DisplayConifgLegacy` | `Legacy/UI_DisplayConifgLegacy` | Legacy | `` | 402 |
| 67 | `CheckInscription` | `UI_CheckInscription` | Root | `` | 407 |
| 68 | `TutorialChaptersMenu` | `UI_TutorialChaptersMenu` | Root | `` | 412 |
| 69 | `AreaMoveMask` | `UI_AreaMoveMask` | Root | `` | 417 |
| 70 | `FullScreenMask` | `UI_FullScreenMask` | Root | `` | 423 |
| 71 | `RevertArchive` | `UI_RevertArchive` | Root | `` | 429 |
| 72 | `CricketCollection` | `Building/UI_CricketCollection` | Building | `` | 434 |
| 73 | `Shop` | `UI_Shop` | Root | `` | 439 |
| 74 | `SettlementTreasuryReplenish` | `Shop/UI_SettlementTreasuryReplenish` | Shop | `` | 444 |
| 75 | `SettlementInformation` | `UI_SettlementInformation` | Root | `` | 449 |
| 76 | `TaiwuShrine` | `Building/UI_BuildingTaiwuShrine` | Building | `` | 454 |
| 77 | `TaiwuVillageLineage` | `Building/UI_TaiwuVillageLineage` | Building | `` | 459 |
| 78 | `Warehouse` | `UI_Warehouse` | Root | `` | 464 |
| 79 | `AddCraftResource` | `UI_AddCraftResource` | Root | `` | 469 |
| 80 | `SelectResident` | `UI_SelectResident` | Select | `` | 474 |
| 81 | `TaiwuVillagers` | `UI_TaiwuVillagers` | Root | `` | 479 |
| 82 | `CombatSkillTree` | `UI_CombatSkillTree` | Combat | `` | 484 |
| 83 | `LifeSkillCombat` | `LifeSkillCombat/UI_LifeSkillCombat2` | LifeSkillCombat | `` | 489 |
| 84 | `LifeSkillCombatCardGroup` | `LifeSkillCombat/UI_LifeSkillCombatCardGroup` | LifeSkillCombat | `` | 494 |
| 85 | `BuildingOverview` | `Building/UI_BuildingOverview` | Building | `` | 499 |
| 86 | `CommonInput` | `UI_CommonInput` | Root | `` | 504 |
| 87 | `NumberSetter` | `UI_NumberSetter` | Root | `` | 510 |
| 88 | `ConfirmExitGame` | `UI_ConfirmExitGame` | Root | `` | 516 |
| 89 | `CharacterShave` | `UI_CharacterShave` | Root | `` | 521 |
| 90 | `AdventureInfo` | `UI_AdventureInfo` | Adventure | `` | 526 |
| 91 | `ReadingEvent` | `Reading/UI_ReadingEvent` | Reading | `` | 532 |
| 92 | `SelectWorker` | `Building/UI_SelectWorker` | Building | `` | 537 |
| 93 | `MonthNotify` | `UI_MonthNotify` | Root | `` | 542 |
| 94 | `ExchangeItem` | `UI_ExchangeItem` | Root | `` | 547 |
| 95 | `ExchangeResource` | `PopUp/UI_ExchangeResource` | PopUp | `` | 553 |
| 96 | `TeaHorseCaravan` | `Building/UI_TeaHorseCaravan` | Building | `` | 558 |
| 97 | `CgPlayer` | `UI_CgPlayer` | Root | `` | 563 |
| 98 | `InstantNotification` | `UI_InstantNotification` | Root | `` | 569 |
| 99 | `InstantNotificationEvent` | `UI_InstantNotificationEvent` | Root | `` | 574 |
| 100 | `ModPanel` | `Mod/UI_ModPanel` | Mod | `` | 580 |
| 101 | `Heal` | `UI_Heal` | Root | `` | 585 |
| 102 | `Encyclopedia` | `UI_EncyclopediaNew` | Root | `` | 590 |
| 103 | `SelectRandomSuccessor` | `UI_SelectRandomSuccessor` | Select | `` | 595 |
| 104 | `SelectItemInCombat` | `UI_SelectItemInCombat` | Select | `` | 601 |
| 105 | `Make` | `Building/UI_Make` | Building | `` | 606 |
| 106 | `CraftsmanPanel` | `Building/UI_CraftsmanPanel` | Building | `` | 611 |
| 107 | `SelectProductType` | `Building/UI_SelectProductType` | Building | `` | 616 |
| 108 | `GameLineScroll` | `UI_GameLineScroll` | Root | `` | 621 |
| 109 | `SetSelectCount` | `UI_SetSelectCount` | Root | `` | 626 |
| 110 | `SliceDownSheet` | `UI_SliceDownSheet` | Root | `` | 631 |
| 111 | `SamsaraPlatform` | `Building/UI_SamsaraPlatform` | Building | `` | 636 |
| 112 | `SelectInformation` | `UI_SelectInformation` | Select | `` | 641 |
| 113 | `SelectInformationForShopping` | `UI_SelectInformationForShopping` | Select | `` | 646 |
| 114 | `BlackMask` | `UI_BlackMask` | Root | `` | 651 |
| 115 | `LifeSkillCombatPrepare` | `LifeSkillCombat/UI_LifeSkillCombatPrepare` | LifeSkillCombat | `` | 656 |
| 116 | `ChickenCoop` | `Building/UI_ChickenCoop` | Building | `` | 662 |
| 117 | `AssignChicken` | `Building/UI_AssignChicken` | Building | `` | 667 |
| 118 | `VillagerRoleManage` | `VillagerRole/UI_VillagerRoleManage` | VillagerRole | `` | 672 |
| 119 | `VillagerRole` | `VillagerRole/UI_VillagerRole` | VillagerRole | `` | 677 |
| 120 | `VillagerRoleDispatch` | `VillagerRole/UI_VillagerRoleDispatch` | VillagerRole | `` | 682 |
| 121 | `VillagerRoleActionSetting` | `VillagerRole/UI_VillagerRoleActionSetting` | VillagerRole | `` | 687 |
| 122 | `VillagerRoleSelectStorageType` | `VillagerRole/UI_VillagerRoleSelectStorageType` | VillagerRole | `` | 692 |
| 123 | `VillagerSelectMerchantType` | `VillagerRole/UI_VillagerSelectMerchantType` | VillagerRole | `` | 697 |
| 124 | `VillagerCraftInputMaterial` | `VillagerRole/UI_VillagerCraftInputMaterial` | VillagerRole | `` | 702 |
| 125 | `PopupMenu` | `UI_PopupMenu` | Root | `` | 707 |
| 126 | `ExchangeBook` | `UI_ExchangeBook` | Root | `` | 712 |
| 127 | `SelectGrave` | `UI_SelectGrave` | Select | `` | 717 |
| 128 | `CharacterMenu` | `CharacterMenu/UI_CharacterMenu` | CharacterMenu | `` | 722 |
| 129 | `CricketCombatResult` | `UI_CricketCombatResult` | Root | `` | 788 |
| 130 | `ItemMultiplyOperation` | `UI_ItemMultiplyOperation` | Root | `` | 793 |
| 131 | `DebtOverview` | `UI_DebtOverview` | Root | `` | 798 |
| 132 | `ItemMultiplyOption` | `UI_ItemMultiplyOption` | Root | `` | 803 |
| 133 | `LifeSkillCombatResult` | `LifeSkillCombat/UI_LifeSkillCombatResult` | LifeSkillCombat | `` | 808 |
| 134 | `DebateResult` | `LifeSkillCombat/UI_DebateResult` | LifeSkillCombat | `` | 813 |
| 135 | `AiForceSilenceDialog` | `LifeSkillCombat/UI_AiForceSilenceDialog` | LifeSkillCombat | `` | 818 |
| 136 | `LegendaryBook` | `UI_LegendaryBook` | Root | `` | 823 |
| 137 | `TaskPopPanel` | `TaskPanel/UI_TaskPopUpPanel` | TaskPanel | `` | 828 |
| 138 | `TaskPanelMain` | `TaskPanel/UI_TaskPanelMain` | TaskPanel | `` | 833 |
| 139 | `AdventureResult` | `Adventure/UI_AdventureResult` | Adventure | `` | 838 |
| 140 | `ProfessionSkillConfirm` | `Profession/UI_ProfessionSkillConfirm` | Profession | `` | 843 |
| 141 | `ResourceListCostConfirm` | `Profession/UI_ResourceListCostConfirm` | Profession | `` | 848 |
| 142 | `ProfessionSkillPreConfirm` | `Profession/UI_ProfessionSkillPreConfirm` | Profession | `` | 853 |
| 143 | `ProfessionMask` | `Profession/UI_ProfessionMask` | Profession | `` | 858 |
| 144 | `Profession2` | `Profession/UI_Profession2` | Profession | `` | 864 |
| 145 | `ProfessionSkillUnlocked` | `Profession/UI_ProfessionSkillUnlocked` | Profession | `` | 869 |
| 146 | `MultiSelectSkillBook` | `Profession/UI_MultiSelectSkillBook` | Profession | `` | 875 |
| 147 | `TeachCombatSkillResultConfirm` | `Profession/UI_TeachCombatSkillResultConfirm` | Profession | `` | 880 |
| 148 | `TeachLifeSkillResultConfirm` | `Profession/UI_TeachLifeSkillResultConfirm` | Profession | `` | 885 |
| 149 | `SetEquipmentEffect` | `UI_SetEquipmentEffect` | Root | `` | 890 |
| 150 | `FindTreasure` | `UI_FindTreasure` | Root | `` | 895 |
| 151 | `UltimateSelectCharacter` | `UI_UltimateSelectCharacter` | Root | `` | 907 |
| 152 | `ResourceChoosyAnim` | `UI_ResourceChoosyAnim` | Root | `` | 912 |
| 153 | `ResourceChoosyConfirm` | `UI_ResourceChoosyConfirm` | Root | `` | 918 |
| 154 | `AreaStoryScroll` | `UI_AreaStoryScroll` | Root | `` | 923 |
| 155 | `MapInfoOption` | `UI_MapInfoOption` | Root | `` | 928 |
| 156 | `AudioSetting` | `UI_AudioSetting` | Root | `` | 933 |
| 157 | `SelectArea` | `UI_SelectMapArea` | Select | `` | 938 |
| 158 | `CharacterPracticeConfirm` | `UI_CharacterPracticeConfirm` | Root | `` | 943 |
| 159 | `MerchantDebtOverview` | `Shop/UI_MerchantDebtOverview` | Shop | `` | 948 |
| 160 | `UsingMedicineItem` | `CharacterMenu/UI_UsingMedicineItem` | CharacterMenu | `` | 953 |
| 161 | `ModifyBook` | `UI_ModifyBook` | Root | `` | 958 |
| 162 | `ModifyBookConfirm` | `UI_ModifyBookConfirm` | Root | `` | 963 |
| 163 | `UpgradeTeammateCommand` | `UI_UpgradeTeammateCommand` | Root | `` | 968 |
| 164 | `UpgradeTeammateCommand2` | `TeammateCommand/UI_UpgradeTeammateCommand2` | TeammateCommand | `` | 973 |
| 165 | `UpgradeTeammateCommandConfirm` | `UI_UpgradeTeammateCommandConfirm` | Root | `` | 978 |
| 166 | `ChangeTeammateCommand` | `TeammateCommand/UI_ChangeTeammateCommand` | TeammateCommand | `` | 983 |
| 167 | `DefendHeavenlyTree` | `UI_DefendHeavenlyTree` | Root | `` | 988 |
| 168 | `FiveElementsPanel` | `UI_FiveElementsPanel` | Root | `` | 993 |
| 169 | `ExtraordinaryCricket` | `UI_ExtraordinaryCricket` | Root | `` | 998 |
| 170 | `MusicPlayer` | `MusicPlayer/UI_MusicPlayer` | MusicPlayer | `` | 1003 |
| 171 | `CombatSkillSpecialBreak` | `CombatSkillSpecialBreak/UI_CombatSkillSpecialBreak` | CombatSkillSpecialBreak | `` | 1008 |
| 172 | `CombatSkillSpecialBreakMultiplySelect` | `CombatSkillSpecialBreak/UI_CombatSkillSpecialBreakMultiplySelect` | CombatSkillSpecialBreak | `` | 1013 |
| 173 | `EventLog` | `UI_EventLog` | Root | `` | 1018 |
| 174 | `OfferUpLevelChangeConfirm` | `Building/UI_OfferUpLevelChangeConfirm` | Building | `` | 1023 |
| 175 | `BuildingJiaoPool` | `UI_BuildingJiaoPool` | Building | `` | 1028 |
| 176 | `JiaoPoolSelectItem` | `UI_JiaoPoolSelectItem` | Root | `` | 1033 |
| 177 | `JiaoChangeLoong` | `UI_JiaoChangeLoong` | Root | `` | 1038 |
| 178 | `JiaoChangeLoongAnim` | `UI_JiaoChangeLoongAnim` | Root | `` | 1043 |
| 179 | `JiaoPoolRecord` | `UI_JiaoPoolRecord` | Root | `` | 1048 |
| 180 | `LoongDebuffAnimation` | `UI_LoongDebuffAnimation` | Root | `` | 1054 |
| 181 | `RecordContent` | `UI_RecordContent` | Root | `` | 1059 |
| 182 | `MakeWugKing` | `Sect/UI_MakeWugKing` | Sect | `` | 1064 |
| 183 | `MakeWugKingList` | `Sect/UI_MakeWugKingList` | Sect | `` | 1069 |
| 184 | `SectShaolinDemonSlayer` | `Sect/UI_SectShaolinDemonSlayer` | Sect | `` | 1074 |
| 185 | `SwapSoul` | `Building/UI_SwapSoul` | Building | `` | 1079 |
| 186 | `EditAvatar` | `UI_EditAvatar` | Root | `` | 1084 |
| 187 | `DevelopingDialog` | `UI_DevelopingDialog` | Root | `` | 1089 |
| 188 | `RecruitPeopleOverview` | `Building/UI_RecruitPeopleOverview` | Building | `` | 1095 |
| 189 | `ShopConfirm` | `Shop/UI_ShopConfirm` | Shop | `` | 1100 |
| 190 | `InvestCaravanConfirm` | `Shop/UI_InvestCaravanConfirm` | Shop | `` | 1105 |
| 191 | `ProtectCaravanConfirm` | `Shop/UI_ProtectCaravanConfirm` | Shop | `` | 1110 |
| 192 | `LegendaryBookKeeping` | `LegendaryBookKeeping/UI_LegendaryBookKeeping` | LegendaryBookKeeping | `` | 1115 |
| 193 | `GiveUpLegendaryBook` | `LegendaryBookKeeping/UI_GiveUpLegendaryBook` | LegendaryBookKeeping | `` | 1120 |
| 194 | `SamsaraPlatformRecords` | `Building/UI_SamsaraPlatformRecords` | Building | `` | 1125 |
| 195 | `LifeLink` | `UI_LifeLink` | Root | `` | 1130 |
| 196 | `SettlementTreasuryRecords` | `UI_SettlementTreasuryRecords` | Root | `` | 1135 |
| 197 | `Looping` | `Looping/UI_Looping` | Looping | `` | 1140 |
| 198 | `LoopingEvent` | `Looping/UI_LoopingEvent` | Looping | `` | 1145 |
| 199 | `Following` | `Following/UI_Following` | Following | `` | 1150 |
| 200 | `MerchantInfo` | `Shop/UI_MerchantInfo` | Shop | `` | 1155 |
| 201 | `MerchantCaravanDetail` | `Shop/UI_MerchantCaravanDetail` | Shop | `` | 1160 |
| 202 | `SettlementPrison` | `SettlementPrison/UI_SettlementPrison` | SettlementPrison | `` | 1165 |
| 203 | `SettlementBounty` | `SettlementPrison/UI_SettlementBounty` | SettlementPrison | `` | 1170 |
| 204 | `SettlementPrisonRecords` | `SettlementPrison/UI_SettlementPrisonRecords` | SettlementPrison | `` | 1175 |
| 205 | `TaiwuVillageStoragesRecord` | `Building/UI_TaiwuVillageStoragesRecord` | Building | `` | 1180 |
| 206 | `SectLaw` | `SettlementPrison/UI_SectLaw` | SettlementPrison | `` | 1185 |
| 207 | `CustomizeSectLaw` | `SettlementPrison/UI_CustomizeSectLaw` | SettlementPrison | `` | 1190 |
| 208 | `PunishmentSeverity` | `SettlementPrison/UI_PunishmentSeverity` | SettlementPrison | `` | 1195 |
| 209 | `ModDependenceChangeList` | `Mod/UI_ModDependenceChangeList` | Mod | `` | 1200 |
| 210 | `MakeMedicine` | `Profession/UI_MakeMedicine` | Profession | `` | 1205 |
| 211 | `ProfessionTravelerStation` | `Profession/UI_ProfessionTravelerStation` | Profession | `` | 1210 |
| 212 | `GearMate` | `GearMate/UI_GearMate` | GearMate | `` | 1215 |
| 213 | `CatchThief` | `UI_CatchThief` | Root | `` | 1220 |
| 214 | `TestBranchInvite` | `UI_TestBranchInvite` | Root | `` | 1226 |
| 215 | `MouseTipSingleDesc` | `MouseTip/UI_MouseTipSingleDesc` | MouseTip | `` | 1231 |
| 216 | `MouseTipSimple` | `MouseTip/UI_MouseTipSimple` | MouseTip | `` | 1236 |
| 217 | `MouseTipSimpleWithHotkeyDisplay` | `MouseTip/UI_MouseTipSimpleWithHotkeyDisplay` | MouseTip | `` | 1241 |
| 218 | `MouseTipCombatSkill` | `MouseTip/UI_MouseTipCombatSkill` | MouseTip | `` | 1246 |
| 219 | `MouseTipWeapon` | `MouseTip/UI_MouseTipWeapon` | MouseTip | `` | 1251 |
| 220 | `MouseTipBook` | `MouseTip/UI_MouseTipBook` | MouseTip | `` | 1256 |
| 221 | `MouseTipMakingTool` | `MouseTip/UI_MouseTipMakingTool` | MouseTip | `` | 1261 |
| 222 | `MouseTipMaterial` | `MouseTip/UI_MouseTipMaterial` | MouseTip | `` | 1266 |
| 223 | `PermanentTips` | `UI_PermanentTips` | Root | `` | 1271 |
| 224 | `MouseTipCricket` | `MouseTip/UI_MouseTipCricket` | MouseTip | `` | 1277 |
| 225 | `MouseTipArmor` | `MouseTip/UI_MouseTipArmor` | MouseTip | `` | 1282 |
| 226 | `MouseTipCarrier` | `MouseTip/UI_MouseTipCarrier` | MouseTip | `` | 1287 |
| 227 | `MouseTipClothing` | `MouseTip/UI_MouseTipClothing` | MouseTip | `` | 1292 |
| 228 | `MouseTipFood` | `MouseTip/UI_MouseTipFood` | MouseTip | `` | 1297 |
| 229 | `MouseTipMedicine` | `MouseTip/UI_MouseTipMedicine` | MouseTip | `` | 1302 |
| 230 | `MouseTipSundries` | `MouseTip/UI_MouseTipSundries` | MouseTip | `` | 1307 |
| 231 | `MouseTipTeaWine` | `MouseTip/UI_MouseTipTeaWine` | MouseTip | `` | 1312 |
| 232 | `MouseTipAccessory` | `MouseTip/UI_MouseTipAccessory` | MouseTip | `` | 1317 |
| 233 | `MouseTipLifeRecords` | `MouseTip/UI_MouseTipLifeRecords` | MouseTip | `` | 1322 |
| 234 | `MouseTipCharacter` | `MouseTip/UI_MouseTipCharacter` | MouseTip | `` | 1327 |
| 235 | `MouseTipResource` | `MouseTip/UI_MouseTipResource` | MouseTip | `` | 1332 |
| 236 | `MouseTipResourceHolder` | `MouseTip/UI_MouseTipResourceHolder` | MouseTip | `` | 1337 |
| 237 | `MouseTipEatingItems` | `MouseTip/UI_MouseTipEatingItems` | MouseTip | `` | 1342 |
| 238 | `MouseTipMapBlock` | `MouseTip/UI_MouseTipMapBlock` | MouseTip | `` | 1347 |
| 239 | `MouseTipFeature` | `MouseTip/UI_MouseTipFeature` | MouseTip | `` | 1352 |
| 240 | `MouseTipMartialArtTournament` | `MouseTip/UI_MouseTipMartialArtTournament` | MouseTip | `` | 1357 |
| 241 | `MouseTipSimpleWide` | `MouseTip/UI_MouseTipSimpleWide` | MouseTip | `` | 1362 |
| 242 | `MouseTipMakeItem` | `MouseTip/UI_MouseTipMakeItem` | MouseTip | `` | 1367 |
| 243 | `MouseTipInnateFiveElements` | `MouseTip/UI_MouseTipInnateFiveElements` | MouseTip | `` | 1372 |
| 244 | `MouseTipDisassembleItem` | `MouseTip/UI_MouseTipDisassembleItem` | MouseTip | `` | 1377 |
| 245 | `MouseTipRepairItem` | `MouseTip/UI_MouseTipRepairItem` | MouseTip | `` | 1382 |
| 246 | `MouseTipReading` | `MouseTip/UI_MouseTipReading` | MouseTip | `` | 1387 |
| 247 | `MouseTipSecretInformation` | `MouseTip/UI_MouseTipSecretInformation` | MouseTip | `` | 1392 |
| 248 | `MouseTipLifeCombatSkillValue` | `MouseTip/UI_MouseTipLifeCombatSkillValue` | MouseTip | `` | 1397 |
| 249 | `MouseTipBuildingShowItem` | `MouseTip/UI_MouseTipBuildingShowItem` | MouseTip | `` | 1402 |
| 250 | `MouseTipBuildingShowRecruitPeople` | `MouseTip/UI_MouseTipBuildingShowRecruitPeople` | MouseTip | `` | 1407 |
| 251 | `MouseTipSecretInformationBroadcastNotify` | `MouseTip/UI_MouseTipSecretInformationBroadcastNotifyTips` | MouseTip | `` | 1412 |
| 252 | `MouseTipDebtChange` | `MouseTip/UI_MouseTipDebtChange` | MouseTip | `` | 1417 |
| 253 | `MouseTipLegendaryBookBonus` | `MouseTip/UI_MouseTipLegendaryBookBonus` | MouseTip | `` | 1422 |
| 254 | `EquipCompareTips` | `UI_EquipCompareTips` | Root | `` | 1427 |
| 255 | `MouseTipProfessionSkill` | `MouseTip/UI_MouseTipProfessionSkill` | MouseTip | `` | 1433 |
| 256 | `MouseTipGearMateUpgradeAttribute` | `MouseTip/UI_MouseTipGearMateUpgradeAttribute` | MouseTip | `` | 1438 |
| 257 | `MouseTipGearMateUpgradeFeature` | `MouseTip/UI_MouseTipGearMateUpgradeFeature` | MouseTip | `` | 1443 |
| 258 | `MouseTipProfession` | `MouseTip/UI_MouseTipProfession` | MouseTip | `` | 1448 |
| 259 | `MouseTipAdventureNode` | `MouseTip/UI_MouseTipAdventureNode` | MouseTip | `` | 1453 |
| 260 | `MouseTipInjury` | `MouseTip/UI_MouseTipInjury` | MouseTip | `` | 1458 |
| 261 | `MouseTipCombatInjuryChange` | `MouseTip/UI_MouseTipCombatInjuryChange` | MouseTip | `` | 1464 |
| 262 | `MouseTipMapArea` | `MouseTip/UI_MouseTipMapArea` | MouseTip | `` | 1470 |
| 263 | `MouseTipAttachedPoison` | `MouseTip/UI_MouseTipAttachedPoison` | MouseTip | `` | 1476 |
| 264 | `MouseTipMixPoison` | `MouseTip/UI_MouseTipMixPoison` | MouseTip | `` | 1482 |
| 265 | `MouseTipAdventure` | `MouseTip/UI_MouseTipAdventure` | MouseTip | `` | 1488 |
| 266 | `MouseTipCharacterPoison` | `MouseTip/UI_MouseTipCharacterPoison` | MouseTip | `` | 1494 |
| 267 | `MouseTipLifeSkillValue` | `MouseTip/UI_MouseTipLifeSkillValue` | MouseTip | `` | 1500 |
| 268 | `MouseTipCombatSkillValue` | `MouseTip/UI_MouseTipCombatSkillValue` | MouseTip | `` | 1505 |
| 269 | `MouseTipCostNeiliAllocation` | `MouseTip/UI_MouseTipCostNeiliAllocation` | MouseTip | `` | 1510 |
| 270 | `MouseTipCombatSkillPractice` | `MouseTip/UI_MouseTipCombatSkillPractice` | MouseTip | `` | 1515 |
| 271 | `MouseTipBodyPart` | `MouseTip/UI_MouseTipBodyPart` | MouseTip | `` | 1520 |
| 272 | `MouseTipCombatSkillBanReason` | `MouseTip/UI_MouseTipCombatSkillBanReason` | MouseTip | `` | 1525 |
| 273 | `MouseTipFold` | `MouseTip/UI_MouseTipFold` | MouseTip | `` | 1530 |
| 274 | `MouseTipMonthNotify` | `MouseTip/UI_MouseTipMonthNotify` | MouseTip | `` | 1535 |
| 275 | `CombatSkillBreakout` | `MouseTip/UI_MouseTipCombatSkillBreakout` | MouseTip | `` | 1540 |
| 276 | `MouseTipCombatSkillBreakInfo` | `MouseTip/UI_MouseTipCombatSkillBreakInfo` | MouseTip | `` | 1545 |
| 277 | `MouseTipFlaw` | `MouseTip/UI_MouseTipFlaw` | MouseTip | `` | 1550 |
| 278 | `MouseTipCombatChangeTrick` | `MouseTip/UI_MouseTipCombatChangeTrick` | MouseTip | `` | 1555 |
| 279 | `MouseTipAdvance` | `MouseTip/UI_MouseTipAdvance` | MouseTip | `` | 1560 |
| 280 | `MouseTipTrickType` | `MouseTip/UI_MouseTipTrickType` | MouseTip | `` | 1565 |
| 281 | `MouseTipUpgradeTeammateCommand` | `MouseTip/UI_MouseTipUpgradeTeammateCommand` | MouseTip | `` | 1570 |
| 282 | `MouseTipFiveElements` | `MouseTip/UI_MouseTipFiveElements` | MouseTip | `` | 1575 |
| 283 | `MouseTipNeiliAllocation` | `MouseTip/UI_MouseTipNeiliAllocation` | MouseTip | `` | 1580 |
| 284 | `MouseTipMusic` | `MouseTip/UI_MouseTipMusic` | MouseTip | `` | 1585 |
| 285 | `MouseTipReadingEvent` | `MouseTip/UI_MouseTipReadingEvent` | MouseTip | `` | 1590 |
| 286 | `EventConfirm` | `UI_EventConfirm` | Root | `` | 1595 |
| 287 | `MouseTipLegacy` | `MouseTip/UI_MouseTipLegacy` | MouseTip | `` | 1601 |
| 288 | `MouseTipLegacyLevel` | `MouseTip/UI_MouseTipLegacyLevel` | MouseTip | `` | 1606 |
| 289 | `MouseTipFuyu` | `MouseTip/UI_MouseTipFuyu` | MouseTip | `` | 1611 |
| 290 | `MouseTipDynamicCondition` | `MouseTip/UI_MouseTipDynamicCondition` | MouseTip | `` | 1616 |
| 291 | `MouseTipJiao` | `MouseTip/UI_MouseTipJiao` | MouseTip | `` | 1621 |
| 292 | `MouseTipJiaoEgg` | `MouseTip/UI_MouseTipJiaoEgg` | MouseTip | `` | 1626 |
| 293 | `MouseTipLoongDebuff` | `MouseTip/UI_MouseTipLoongDebuff` | MouseTip | `` | 1631 |
| 294 | `MouseTipJiaoNurturance` | `MouseTip/UI_MouseTipJiaoNurturance` | MouseTip | `` | 1636 |
| 295 | `MouseTipCombatSkillBuff` | `MouseTip/UI_MouseTipCombatSkillBuff` | MouseTip | `` | 1641 |
| 296 | `MouseTipGeneralLines` | `MouseTip/UI_MouseTipGeneralLines` | MouseTip | `` | 1646 |
| 297 | `MouseTipMixPoisonEffectSimple` | `MouseTip/UI_MouseTipMixPoisonEffectSimple` | MouseTip | `` | 1651 |
| 298 | `MouseTipMixPoisonEffectDetailed` | `MouseTip/UI_MouseTipMixPoisonEffectDetailed` | MouseTip | `` | 1656 |
| 299 | `MouseTipDisorderOfQi` | `MouseTip/UI_MouseTipDisorderOfQi` | MouseTip | `` | 1661 |
| 300 | `MouseTipCostWugKing` | `MouseTip/UI_MouseTipCostWugKing` | MouseTip | `` | 1666 |
| 301 | `MouseTipMakeWugKing` | `MouseTip/UI_MouseTipMakeWugKing` | MouseTip | `` | 1671 |
| 302 | `MouseTipEmptyContainer` | `MouseTip/UI_MouseTipEmptyContainer` | MouseTip | `` | 1676 |
| 303 | `MouseTipCharacterComplete` | `MouseTip/UI_MouseTipCharacterComplete` | MouseTip | `` | 1681 |
| 304 | `MouseTipBuildingProduce` | `MouseTip/UI_MouseTipBuildingProduce` | MouseTip | `` | 1686 |
| 305 | `MouseTipBuildingProduceCollectResource` | `MouseTip/UI_MouseTipBuildingProduceCollectResource` | MouseTip | `` | 1691 |
| 306 | `MouseTipMixPoisonEffectOutCombat` | `MouseTip/UI_MouseTipMixPoisonEffectOutCombat` | MouseTip | `` | 1696 |
| 307 | `MouseTipEatingWug` | `MouseTip/UI_MouseTipEatingWug` | MouseTip | `` | 1701 |
| 308 | `MouseTipBuildingRequireCultureSafety` | `MouseTip/UI_MouseTipBuildingRequireCultureSafety` | MouseTip | `` | 1706 |
| 309 | `MouseTipChangeTrick` | `MouseTip/UI_MouseTipChangeTrick` | MouseTip | `` | 1711 |
| 310 | `MouseTipCombatBannedList` | `MouseTip/UI_MouseTipCombatBannedList` | MouseTip | `` | 1716 |
| 311 | `MouseTipCombatBlockAttack` | `MouseTip/UI_MouseTipCombatBlockAttack` | MouseTip | `` | 1721 |
| 312 | `MouseTipEquipLoad` | `MouseTip/UI_MouseTipEquipLoad` | MouseTip | `` | 1726 |
| 313 | `MouseTipDefeatMark` | `MouseTip/UI_MouseTipDefeatMark` | MouseTip | `` | 1731 |
| 314 | `MouseTipDamageValue` | `MouseTip/UI_MouseTipDamageValue` | MouseTip | `` | 1736 |
| 315 | `MouseTipDestiny` | `MouseTip/UI_MouseTipDestiny` | MouseTip | `` | 1741 |
| 316 | `MouseTipSettlementTreasury` | `MouseTip/UI_MouseTipSettlementTreasury` | MouseTip | `` | 1746 |
| 317 | `MouseTipLoopingEvent` | `MouseTip/UI_MouseTipLoopingEvent` | MouseTip | `` | 1751 |
| 318 | `MouseTipBuildingLevel` | `MouseTip/UI_MouseTipBuildingLevel` | MouseTip | `` | 1756 |
| 319 | `MouseTipLifeLinkNeiliType` | `MouseTip/UI_MouseTipLifeLinkNeiliType` | MouseTip | `` | 1761 |
| 320 | `MouseTipActiveRead` | `MouseTip/UI_MouseTipActiveRead` | MouseTip | `` | 1766 |
| 321 | `MouseTipActiveLoop` | `MouseTip/UI_MouseTipActiveLoop` | MouseTip | `` | 1771 |
| 322 | `MouseTipReadProgress` | `MouseTip/UI_MouseTipReadProgress` | MouseTip | `` | 1776 |
| 323 | `MouseTipLoopProgress` | `MouseTip/UI_MouseTipLoopProgress` | MouseTip | `` | 1781 |
| 324 | `MouseTipCharacterOnMapBlock` | `MouseTip/UI_MouseTipCharacterOnMapBlock` | MouseTip | `` | 1786 |
| 325 | `MouseTipTeammateCommand` | `MouseTip/UI_MouseTipTeammateCommand` | MouseTip | `` | 1791 |
| 326 | `MouseTipTeammateCount` | `MouseTip/UI_MouseTipTeammateCount` | MouseTip | `` | 1796 |
| 327 | `MouseTipFeatureMedal` | `MouseTip/UI_MouseTipFeatureMedal` | MouseTip | `` | 1801 |
| 328 | `MouseTipFulongFlame` | `MouseTip/UI_MouseTipFulongFlame` | MouseTip | `` | 1806 |
| 329 | `MouseTipVillagerRoleAvailableCount` | `MouseTip/UI_MouseTipVillagerRoleAvailableCount` | MouseTip | `` | 1811 |
| 330 | `MouseTipVillagerRoleEffect` | `MouseTip/UI_MouseTipVillagerRoleEffect` | MouseTip | `` | 1816 |
| 331 | `MouseTipCombatRawCreate` | `MouseTip/UI_MouseTipCombatRawCreate` | MouseTip | `` | 1821 |
| 332 | `MouseTipCombatUnlockProgress` | `MouseTip/UI_MouseTipCombatUnlockProgress` | MouseTip | `` | 1826 |
| 333 | `MouseTipCombatWeaponUnlock` | `MouseTip/UI_MouseTipCombatWeaponUnlock` | MouseTip | `` | 1831 |
| 334 | `MouseTipCaravanOperation` | `MouseTip/UI_MouseTipCaravanOperation` | MouseTip | `` | 1836 |
| 335 | `MouseTipTaiwuWanted` | `MouseTip/UI_MouseTipTaiwuWanted` | MouseTip | `` | 1841 |
| 336 | `MouseTipCaravanPath` | `MouseTip/UI_MouseTipCaravanPath` | MouseTip | `` | 1846 |
| 337 | `MouseTipCaravanPathDetail` | `MouseTip/UI_MouseTipCaravanPathDetail` | MouseTip | `` | 1851 |
| 338 | `MouseTipExtraProfessionSkill` | `MouseTip/UI_MouseTipExtraProfessionSkill` | MouseTip | `` | 1856 |
| 339 | `MouseTipOrganization` | `MouseTip/UI_MouseTipOrganization` | MouseTip | `` | 1861 |
| 340 | `MouseTipVillagerNeedItem` | `MouseTip/UI_MouseTipVillagerNeedItem` | MouseTip | `` | 1866 |
| 341 | `MouseTipNormalInformationType` | `MouseTip/UI_MouseTipNormalInformationType` | MouseTip | `` | 1871 |
| 342 | `MouseTipDemonSlayer` | `MouseTip/UI_MouseTipDemonSlayer` | MouseTip | `` | 1876 |
| 343 | `MouseTipSkillBreakBonus` | `MouseTip/UI_MouseTipSkillBreakBonus` | MouseTip | `` | 1881 |
| 344 | `MouseTipSkillBreakNormalCell` | `MouseTip/UI_MouseTipSkillBreakNormalCell` | MouseTip | `` | 1886 |
| 345 | `MouseTipSectStory` | `MouseTip/UI_MouseTipSectStory` | MouseTip | `` | 1891 |
| 346 | `MouseTipThreeVitals` | `MouseTip/UI_MouseTipThreeVitals` | MouseTip | `` | 1896 |
| 347 | `MouseTipPrisonerResistance` | `MouseTip/UI_MouseTipPrisonerResistance` | MouseTip | `` | 1901 |
| 348 | `MouseTipBuildingFeast` | `MouseTip/UI_MouseTipBuildingFeast` | MouseTip | `` | 1906 |
| 349 | `MouseTipLifeSkillDetailReadProgress` | `MouseTip/UI_MouseTipLifeSkillDetailReadProgress` | MouseTip | `` | 1911 |
| 350 | `MouseTipLifeSkillDetailUnlockBuilding` | `MouseTip/UI_MouseTipLifeSkillDetailUnlockBuilding` | MouseTip | `` | 1916 |
| 351 | `MouseTipLifeSkillDetailUnlockInformation` | `MouseTip/UI_MouseTipLifeSkillDetailUnlockInformation` | MouseTip | `` | 1921 |
| 352 | `MouseTipLifeSkillDetailUnlockStrategy` | `MouseTip/UI_MouseTipLifeSkillDetailUnlockStrategy` | MouseTip | `` | 1926 |
| 353 | `MouseTipLifeSkillCombatCardType` | `MouseTip/UI_MouseTipLifeSkillCombatCardType` | MouseTip | `` | 1931 |
| 354 | `MouseTipLifeSkillCombatUnit` | `MouseTip/UI_MouseTipLifeSkillCombatUnit` | MouseTip | `` | 1936 |
| 355 | `MouseTipLifeSkillCombatBlock` | `MouseTip/UI_MouseTipLifeSkillCombatBlock` | MouseTip | `` | 1941 |
| 356 | `MouseTipLifeSkillCombatStrategy` | `MouseTip/UI_MouseTipLifeSkillCombatStrategy` | MouseTip | `` | 1946 |
| 357 | `MouseTipLifeSkillCombatStress` | `MouseTip/UI_MouseTipLifeSkillCombatStress` | MouseTip | `` | 1951 |
| 358 | `MouseTipLifeSkillCombatFirstMove` | `MouseTip/UI_MouseTipLifeSkillCombatFirstMove` | MouseTip | `` | 1956 |
| 359 | `MouseTipLifeSkillCombatLastMove` | `MouseTip/UI_MouseTipLifeSkillCombatLastMove` | MouseTip | `` | 1961 |
| 360 | `MouseTipLifeSkillCombatAudience` | `MouseTip/UI_MouseTipLifeSkillCombatAudience` | MouseTip | `` | 1966 |
| 361 | `MouseTipSimpleList` | `MouseTip/UI_MouseTipSimpleList` | MouseTip | `` | 1971 |
| 362 | `MouseTipMatchVillagerRole` | `MouseTip/UI_MouseTipMatchVillagerRole` | MouseTip | `` | 1976 |
| 363 | `BuildingTeachBook` | `MouseTip/UI_MouseTipBuildingTeachBook` | MouseTip | `` | 1981 |
| 364 | `MouseTipTaiwuVillageStele` | `MouseTip/UI_MouseTipTaiwuVillageStele` | MouseTip | `` | 1986 |
| 365 | `WorkingStatus` | `MouseTip/UI_MouseTipWorkingStatus` | MouseTip | `` | 1991 |
| 366 | `LegendaryBookOwner` | `LegendaryBook/UI_LegendaryBookOwner` | LegendaryBook | `` | 1996 |
| 367 | `LegendaryBookGiveUp` | `MouseTip/UI_MouseTipGiveUpLegendaryBook` | MouseTip | `` | 2001 |
| 368 | `LegendaryBookCompetitors` | `LegendaryBook/UI_LegendaryBookCompetitors` | LegendaryBook | `` | 2006 |
| 369 | `LegendaryBookFallen` | `LegendaryBook/UI_LegendaryBookFallen` | LegendaryBook | `` | 2011 |
| 370 | `LegendaryBookUnlockBreakPlateConfirm` | `LegendaryBook/UI_LegendaryBookUnlockBreakPlateConfirm` | LegendaryBook | `` | 2016 |
| 371 | `MouseTipEncyclopedia` | `MouseTip/UI_MouseTipEncyclopedia` | MouseTip | `` | 2021 |
| 372 | `EventEditor` | `EditorViews/UI_EventEditor` | EditorViews | `` | 2026 |
| 373 | `AdventureEditor` | `EditorViews/UI_AdventureEditor` | EditorViews | `` | 2032 |
| 374 | `AdventureEditorRemake` | `EditorViews/UI_AdventureEditorRemake` | EditorViews | `` | 2038 |
| 375 | `AdventureTestTools` | `EditorViews/UI_AdventureTestTools` | EditorViews | `` | 2044 |
| 376 | `ItemTemplateSelector` | `EditorViews/UI_ItemTemplateSelector` | EditorViews | `` | 2050 |
| 377 | `FileExplorer` | `UI_FileExplorer` | Root | `` | 2055 |
| 378 | `AiEditor` | `EditorViews/UI_AiEditor` | EditorViews | `` | 2060 |
| 379 | `AiParamInputField` | `EditorViews/UI_AiParamInputField` | EditorViews | `` | 2066 |
| 380 | `AiShortNumberInputField` | `EditorViews/UI_AiShortNumberInputField` | EditorViews | `` | 2071 |
| 381 | `AiSearchInputField` | `EditorViews/UI_AiSearchInputField` | EditorViews | `` | 2076 |
| 382 | `MonthlyNotificationSortingGroupSettings` | `UI_MonthlyNotificationSortingGroupSettings` | Root | `` | 2081 |
| 383 | `CricketBetting` | `UI_CricketBetting` | Root | `` | 2086 |
| 384 | `BeggarSkill3` | `Profession/UI_BeggarSkill3` | Profession | `` | 2091 |
| 385 | `TravelingTaoistMonkSkill2` | `Profession/UI_TravelingTaoistMonkSkill2` | Profession | `` | 2096 |
| 386 | `EventStyleFeatureSelect` | `Profession/UI_EventStyleFeatureSelect` | Profession | `` | 2101 |
| 387 | `InteractCheckResult` | `InteractCheckResult/UI_InteractCheckResult` | InteractCheckResult | `` | 2106 |
| 388 | `MouseTipInteractCheckResult` | `MouseTip/UI_MouseTipInteractCheckResult` | MouseTip | `` | 2111 |
| 389 | `MouseTipExpCheck` | `MouseTip/UI_MouseTipExpCheck` | MouseTip | `` | 2116 |
| 390 | `MouseTipRecordIncompatible` | `MouseTip/UI_MouseTipRecordIncompatible` | MouseTip | `` | 2121 |
| 391 | `YuanshanMiniGame` | `MiniGame/UI_Yuanshan` | MiniGame | `` | 2126 |
| 392 | `CostResourceConfirm` | `UI_CostResourceConfirm` | Root | `` | 2131 |
| 393 | `ThreeVitals` | `Sect/UI_ThreeVitals` | Sect | `` | 2136 |
| 394 | `SelectThreeVitalsTarget` | `Sect/UI_SelectThreeVitalsTarget` | Sect | `` | 2141 |
| 395 | `BuildingFeastMenu` | `Building/UI_BuildingFeastMenu` | Building | `` | 2146 |
| 396 | `MouseTipSettlementTreasuryOrPrisonLayer` | `MouseTip/UI_MouseTipSettlementTreasuryOrPrisonLayer` | MouseTip | `` | 2151 |

## 9. 后续待补充

- `UiEvents` 的完整枚举与每个事件的触发/监听点。
- 每个 `UIElement` 对应的实际 `UIBase` 类名映射；当前反编译源码存在文件名混淆和重复类名，需结合 prefab 或运行时反射确认。
- `UILayer`、`ServeGroup`、窗口状态机的完整语义表。
- 各 UI 的 `OnClick` 按钮名与后端 `DomainMethod.Call` 映射。
