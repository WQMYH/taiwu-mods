return {
	Title = "怀孕状态崩溃防护",
	Author = "WQMYH",
	Version = "0.1.0.0",
	Description = "为怀孕状态缺失导致的过月 KeyNotFoundException 添加后端防护，避免 GameData Worker 崩溃。本 MOD 不伪造怀孕状态，只跳过异常角色的本次怀孕状态更新。",
	Source = 0,
	GameVersion = "1.0.29",
	BackendPlugins = {
		[1] = "PregnantStateGuard.Backend.dll",
	},
	TagList = {
		[1] = "Modifications",
		[2] = "Bug Fixes",
	},
	Visibility = 2,
	DefaultSettings = {},
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = "Cover.png",
	WorkshopCover = "Cover.png",
}

