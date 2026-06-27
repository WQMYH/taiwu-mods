return {
	Title = "FullDiscovery 全量信息抓取器",
	Author = "WQMYH",
	Version = "0.1.0.0",
	Description = "只读抓取游戏配置、事件窗口、月度事件与 API 反射索引，输出到 Dump_out/FullDiscovery，供 MOD 开发查找使用。",
	Source = 0,
	GameVersion = "1.0.32",
	FrontendPlugins = {
		[1] = "FullDiscovery.Frontend.dll",
	},
	BackendPlugins = {
		[1] = "FullDiscovery.Backend.dll",
	},
	Visibility = 2,

	-- 使用说明：
	-- 1. 本 MOD 永远只读：不调用 EventSelect、HandleMonthlyEvent、EventModel.Select、SetInputResult。
	-- 2. 默认输出目录为游戏根目录/Dump_out/FullDiscovery。
	-- 3. 静态配置导出体量较大，如启动变慢，可关闭“全量 Config 表”只保留重点索引。
	-- 4. API 索引只反射类型/方法/字段/属性签名，不调用这些方法。
	-- 5. 运行时事件文本只能记录玩家实际触发过的窗口；未触发分支不会凭空生成。

	SettingGroups = {
		[1] = "基础",
		[2] = "静态配置",
		[3] = "运行时事件",
		[4] = "API 辅助",
		[5] = "性能保护",
	},

	DefaultSettings = {
		[1] = {
			SettingType = "Toggle",
			Key = "EnableDiscovery",
			DisplayName = "启用抓取",
			Description = "关闭后不写入任何抓取文件。",
			GroupName = "基础",
			DefaultValue = true,
		},
		[2] = {
			SettingType = "InputField",
			Key = "OutputDirectory",
			DisplayName = "输出目录",
			Description = "相对游戏根目录的输出路径。",
			GroupName = "基础",
			DefaultValue = "Dump_out/FullDiscovery",
		},
		[3] = {
			SettingType = "Toggle",
			Key = "VerboseLog",
			DisplayName = "详细日志",
			Description = "开启后在 Player.log 中写入更多抓取过程信息。",
			GroupName = "基础",
			DefaultValue = false,
		},
		[4] = {
			SettingType = "Toggle",
			Key = "ExportAllConfigTables",
			DisplayName = "全量 Config 表",
			Description = "启动时反射导出 Config 命名空间下的配置表。",
			GroupName = "静态配置",
			DefaultValue = true,
		},
		[5] = {
			SettingType = "Toggle",
			Key = "ExportFocusedIndexes",
			DisplayName = "重点索引",
			Description = "生成事件、建筑、人物模板、特性等常用索引。",
			GroupName = "静态配置",
			DefaultValue = true,
		},
		[6] = {
			SettingType = "Toggle",
			Key = "RecordEventWindows",
			DisplayName = "事件窗口记录",
			Description = "记录 EventModel 当前显示事件、文本、角色、选项与 ExtraData 摘要。",
			GroupName = "运行时事件",
			DefaultValue = true,
		},
		[7] = {
			SettingType = "Toggle",
			Key = "RecordMonthlyCollections",
			DisplayName = "月度集合记录",
			Description = "尝试记录月度通知集合；入口失效时会写入日志并依赖事件窗口记录补充。",
			GroupName = "运行时事件",
			DefaultValue = true,
		},
		[8] = {
			SettingType = "Toggle",
			Key = "ExportApiIndex",
			DisplayName = "API 反射索引",
			Description = "启动时生成类型、方法、字段、属性索引，便于后续查找 API。",
			GroupName = "API 辅助",
			DefaultValue = true,
		},
		[9] = {
			SettingType = "InputField",
			Key = "ApiNamespaceFilters",
			DisplayName = "API 命名空间过滤",
			Description = "逗号分隔；默认关注 GameData、Config、Game.Views、TaiwuModdingLib。",
			GroupName = "API 辅助",
			DefaultValue = "GameData,Config,Game.Views,TaiwuModdingLib",
		},
		[10] = {
			SettingType = "InputField",
			Key = "MaxConfigTables",
			DisplayName = "最大配置表数",
			Description = "0 表示不限制；用于防止一次导出过多表。",
			GroupName = "性能保护",
			DefaultValue = "0",
		},
		[11] = {
			SettingType = "InputField",
			Key = "MaxItemsPerTable",
			DisplayName = "单表最大条目",
			Description = "0 表示不限制；超出时截断并在文件中标记 truncated。",
			GroupName = "性能保护",
			DefaultValue = "0",
		},
		[12] = {
			SettingType = "InputField",
			Key = "MaxApiMethods",
			DisplayName = "最大 API 方法数",
			Description = "0 表示不限制；用于控制 api/methods.jsonl 体量。",
			GroupName = "性能保护",
			DefaultValue = "0",
		},
	},

	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = "Cover.png",
	WorkshopCover = "Cover.png",
}
