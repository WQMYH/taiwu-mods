return {
	Title = "连续挖掘",
	Author = "MOD Developer",
	Version = "1.0.1.0",
	Description = "修改原版连续挖宝逻辑：挖到宝物后不再自动停止，可持续挖到无宝、精力不足、达到品级阈值或达到安全次数上限。",
	Source = 0,
	GameVersion = "1.0.32",
	FrontendPlugins = {
		[1] = "ContinuousDigging.Frontend.dll",
	},
	Visibility = 2,

	-- 说明：
	-- 1. 本 MOD 使用游戏原版“连续挖宝”模式，只修改“挖到宝物就停止”的断点。
	-- 2. 一次挖宝原版按 3 天判定，即 30 行动点；不建议把“每次精力消耗”改低。
	-- 3. “最高品级限制”中，数值越小品级越高；0 表示不因奖励品级停止。
	-- 4. 特殊材料事件暂时交给原版流程处理，不强行连续。

	DefaultSettings = {
		[1] = {
			SettingType = "Toggle",
			Key = "EnabledContinuousDigging",
			DisplayName = "启用连续挖掘",
			Description = "开启后，原版连续挖宝在挖到宝物后会继续尝试下一次，而不是立即停止。",
			DefaultValue = true,
		},
		[2] = {
			SettingType = "InputField",
			Key = "MaxGradeLimit",
			DisplayName = "最高品级限制",
			Description = "0=不限制；1-9=挖到该品级或更高品级时停止。数值越小品级越高。",
			DefaultValue = "0",
		},
		[3] = {
			SettingType = "InputField",
			Key = "ActionPointCostPerDig",
			DisplayName = "每次挖掘精力消耗",
			Description = "原版一次挖宝需要 3 天，即 30 行动点。用于提前停止连续挖掘。",
			DefaultValue = "30",
		},
		[4] = {
			SettingType = "Toggle",
			Key = "EnableActionPointCheck",
			DisplayName = "启用精力检查",
			Description = "开启后，剩余行动点低于每次挖掘消耗时停止。",
			DefaultValue = true,
		},
		[5] = {
			SettingType = "InputField",
			Key = "MaxConsecutiveDigs",
			DisplayName = "最大连续挖掘次数",
			Description = "防止异常无限循环的安全上限。",
			DefaultValue = "50",
		},
		[6] = {
			SettingType = "Toggle",
			Key = "EnableDebugLog",
			DisplayName = "启用调试日志",
			Description = "记录 Patch、按钮、连续状态、挖掘结果和停止原因，用于排查连续挖掘未生效。",
			DefaultValue = true,
		},
	},
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = "Cover.png",
	WorkshopCover = "Cover.png",
}
