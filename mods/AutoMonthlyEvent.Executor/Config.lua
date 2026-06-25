return {
	Title = "Auto Monthly Event Executor",
	Author = "WQMYH",
	Version = "0.1.0.0",
	Description = "Automatically handles a strict whitelist of common monthly event windows. Unknown events wait for the player.",
	Source = 0,
	GameVersion = "0.84.54-test",
	FrontendPlugins = {
		[1] = "AutoMonthlyEvent.Executor.Frontend.dll",
	},
	BackendPlugins = {
	},
	TagList = {
		[1] = "Modifications",
		[2] = "Quality of Life",
	},
	Visibility = 2,

	-- Safety notes:
	-- EnableAutoExecute=false means this mod will not click any option.
	-- DryRun=true records the decision only; set it to false only after checking the log.
	-- UnknownPolicy="WaitPlayer" keeps unknown, story, adventure, sparring, contest and challenge events for manual handling.
	-- LogDirectory is relative to this mod directory: Mod/AutoMonthlyEvent.Executor, not the game root and not Dump_out.
	-- EnableActionLog=false disables executor_actions.jsonl.
	Executor = {
		EnableAutoExecute = false,
		DryRun = true,
		UnknownPolicy = "WaitPlayer",
		AutoContinueWhitelistedResults = true,
		RequestDirection = "NpcToTaiwu",
		FallbackFavorabilityThreshold = 15000,
		EnableActionLog = true,
		LogDirectory = "Logs",
		ActionLogFileName = "executor_actions.jsonl",
		AllowedRelationTypes = { 1024, 1, 2, 8, 16, 64, 128, 512, 8192 },
	},

	DefaultSettings = {
		[1] = {
			SettingType = "Toggle",
			Key = "EnableAutoExecute",
			DisplayName = "Enable auto execution",
			Description = "When disabled, this mod never clicks event options.",
			DefaultValue = false,
		},
		[2] = {
			SettingType = "Toggle",
			Key = "DryRun",
			DisplayName = "Dry run",
			Description = "When enabled, decisions are logged but no option is clicked.",
			DefaultValue = true,
		},
		[3] = {
			SettingType = "Toggle",
			Key = "EnableActionLog",
			DisplayName = "Enable action log",
			Description = "Write executor_actions.jsonl under this mod directory.",
			DefaultValue = true,
		},
		[4] = {
			SettingType = "InputField",
			Key = "LogDirectory",
			DisplayName = "Log directory",
			Description = "Relative to Mod/AutoMonthlyEvent.Executor.",
			DefaultValue = "Logs",
		},
		[5] = {
			SettingType = "InputField",
			Key = "FallbackFavorabilityThreshold",
			DisplayName = "Favorability threshold",
			Description = "Requests are accepted when relation is not whitelisted but favorability reaches this value.",
			DefaultValue = "15000",
		},
	},
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = "Cover.png",
	WorkshopCover = "Cover.png",
}
