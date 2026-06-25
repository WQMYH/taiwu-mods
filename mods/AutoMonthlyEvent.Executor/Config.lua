return {
	Title = "月度事件自动处理器",
	Author = "WQMYH",
	Version = "0.1.0.0",
	Description = "按白名单和筛选条件自动处理部分月度事件；未知事件交由玩家处理。",
	Source = 0,
	GameVersion = "0.84.54-test",
	FrontendPlugins = {
		[1] = "AutoMonthlyEvent.Executor.Frontend.dll",
	},
	BackendPlugins = {
	},
	Visibility = 2,

	-- 配置说明：
	-- 启用自动执行=false 时，本 MOD 不处理事件，也不写执行日志。
	-- 启用自动执行=true 且启用日志=true 时，会执行规则并记录日志。
	-- 演练模式=true 时，只记录将要执行的动作，不点击选项。
	-- 日志目录相对于 Mod/AutoMonthlyEvent.Executor，不是游戏根目录，也不是 Dump_out。
	-- 当前支持事件：资源请求、茶酒/物品请求、请求结果继续、指点结果继续、收养弃婴。
	-- 收养弃婴第一版只判断婴孩立场：允许立场则收养，其余全部搁置。

	DefaultSettings = {
		[1] = {
			SettingType = "Toggle",
			Key = "EnableAutoExecute",
			DisplayName = "【总开关】启用自动执行",
			Description = "关闭时不处理事件，也不写执行决策日志。",
			DefaultValue = false,
		},
		[2] = {
			SettingType = "Toggle",
			Key = "DryRun",
			DisplayName = "【总开关】演练模式",
			Description = "开启后只记录将要执行的动作，不点击选项。",
			DefaultValue = false,
		},
		[3] = {
			SettingType = "Toggle",
			Key = "EnableActionLog",
			DisplayName = "【日志】启用执行日志",
			Description = "开启后在本 MOD 目录下写入中文日志和 JSONL 日志。",
			DefaultValue = true,
		},
		[4] = {
			SettingType = "InputField",
			Key = "LogDirectory",
			DisplayName = "【日志】日志目录",
			Description = "相对于 Mod/AutoMonthlyEvent.Executor 的目录。",
			DefaultValue = "Logs",
		},
		[5] = {
			SettingType = "InputField",
			Key = "FallbackFavorabilityThreshold",
			DisplayName = "【请求筛选】好感阈值",
			Description = "关系不在允许范围内时，好感达到该值才给予请求。",
			DefaultValue = "15000",
		},
		[6] = {
			SettingType = "Toggle",
			Key = "EnableResourceRequest",
			DisplayName = "【已支持事件】资源请求",
			Description = "自动处理银钱、木材、金石、织物、药材、食材请求。",
			DefaultValue = true,
		},
		[7] = {
			SettingType = "Toggle",
			Key = "EnableTeaWineItemRequest",
			DisplayName = "【已支持事件】茶酒物品请求",
			Description = "自动处理已验证白名单内的茶酒或物品请求。",
			DefaultValue = true,
		},
		[8] = {
			SettingType = "Toggle",
			Key = "EnableRequestResultContinue",
			DisplayName = "【已支持事件】请求结果继续",
			Description = "自动点击请求成功或拒绝后的单一继续选项。",
			DefaultValue = true,
		},
		[9] = {
			SettingType = "Toggle",
			Key = "EnableGuidanceResultContinue",
			DisplayName = "【已支持事件】指点结果继续",
			Description = "自动点击 NPC 指点结果的单一继续选项。",
			DefaultValue = true,
		},
		[10] = {
			SettingType = "Toggle",
			Key = "EnableAdoptAbandonedBaby",
			DisplayName = "【已支持事件】收养弃婴",
			Description = "按婴孩立场判断是否收养；不满足条件则搁置。",
			DefaultValue = true,
		},
		[11] = {
			SettingType = "InputField",
			Key = "AllowedAdoptionBehaviorTypes",
			DisplayName = "【收养筛选】允许立场",
			Description = "逗号分隔：0刚正、1仁善、2中庸、3叛逆、4唯我。",
			DefaultValue = "0,1,2",
		},
		[12] = {
			SettingType = "InputField",
			Key = "AdoptionMaxChildAge",
			DisplayName = "【收养筛选】婴孩最大年龄",
			Description = "用于从事件角色中识别婴孩，默认 3。",
			DefaultValue = "3",
		},
	},
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = "Cover.png",
	WorkshopCover = "Cover.png",
}
