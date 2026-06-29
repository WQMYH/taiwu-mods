return {
	Title = "月度事件自动处理器",
	Author = "WQMYH",
	Version = "1.1.0.0",
	Description = "【早期测试版】自动处理太吾绘卷中的月度交互事件。当前支持：NPC请求系列66-86、结果窗口继续、家庭事件（收养弃婴/胎教）、新生儿取名、智能选择功能。Steam创意工坊订阅后请手动开启所需功能。",
	Source = 0,
	GameVersion = "1.0.32",
	FrontendPlugins = {
		[1] = "AutoMonthlyEvent.Executor.Frontend.dll",
	},
	BackendPlugins = {
		[1] = "AutoMonthlyEvent.Executor.Backend.dll",
	},
	Visibility = 0,

	SettingGroups = {
		[1] = "基础设置",
		[2] = "日志配置",
		[3] = "NPC请求处理",
		[4] = "结果窗口处理",
		[5] = "家庭事件处理",
		[6] = "新生儿取名",
		[7] = "智能选择功能",
	},

	DefaultSettings = {
		-- ========================================
		-- 基础设置
		-- ========================================
		[1] = {
			SettingType = "Toggle",
			Key = "EnableAutoExecute",
			DisplayName = "启用自动执行",
			Description = "全局总开关。关闭时不会自动点击事件选项；日志仍可记录调试观察。",
			GroupName = "基础设置",
			DefaultValue = false,
		},
		[2] = {
			SettingType = "Toggle",
			Key = "DryRun",
			DisplayName = "演练模式",
			Description = "开启后只记录将要执行的动作，不点击选项。用于测试配置效果。",
			GroupName = "基础设置",
			DefaultValue = false,
		},

		-- ========================================
		-- 日志配置
		-- ========================================
		[3] = {
			SettingType = "Toggle",
			Key = "EnableActionLog",
			DisplayName = "启用执行日志",
			Description = "开启后在本 MOD 目录下写入中文日志和 JSONL 日志。",
			GroupName = "日志配置",
			DefaultValue = true,
		},
		[4] = {
			SettingType = "Toggle",
			Key = "EnableDebugLog",
			DisplayName = "启用调试日志",
			Description = "记录事件分类、选项匹配、关系判断与执行前后状态，用于定位自动化失败步骤。",
			GroupName = "日志配置",
			DefaultValue = true,
		},
		[5] = {
			SettingType = "InputField",
			Key = "LogDirectory",
			DisplayName = "日志目录",
			Description = "相对于 Mod/AutoMonthlyEvent.Executor 的目录。",
			GroupName = "日志配置",
			DefaultValue = "Logs",
		},

		-- ========================================
		-- NPC请求处理
		-- ========================================
		[6] = {
			SettingType = "Toggle",
			Key = "EnableRequestCategory",
			DisplayName = "NPC请求总开关",
			Description = "NPC请求类事件权限。实际执行还需要打开下面的具体请求开关。",
			GroupName = "NPC请求处理",
			DefaultValue = false,
		},
		[7] = {
			SettingType = "Toggle",
			Key = "EnableMonthlyRequest",
			DisplayName = "请求系列66-86",
			Description = "处理外伤、内伤、驱毒、续命、内息、内力、灭蛊、食物、茶酒、资源、物品、修理、淬毒、指点、研读、突破请求。",
			GroupName = "NPC请求处理",
			DefaultValue = false,
		},
		[8] = {
			SettingType = "Toggle",
			Key = "EnableResourceRequest",
			DisplayName = "资源请求",
			Description = "请求总开关开启时，自动处理NPC索要资源的事件。关闭后资源请求交给玩家。",
			GroupName = "NPC请求处理",
			DefaultValue = true,
		},
		[9] = {
			SettingType = "Toggle",
			Key = "EnableTeaWineItemRequest",
			DisplayName = "茶酒与物品请求",
			Description = "请求总开关开启时，自动处理NPC索要茶酒、物品及相关请求。关闭后交给玩家。",
			GroupName = "NPC请求处理",
			DefaultValue = true,
		},
		[10] = {
			SettingType = "InputField",
			Key = "RequestRelationMode",
			DisplayName = "关系筛选模式",
			Description = "1直系血亲+配偶；2血亲+结义+义亲+配偶；3血亲+结义+义亲+朋友+配偶。",
			GroupName = "NPC请求处理",
			DefaultValue = "3",
		},
		[11] = {
			SettingType = "InputField",
			Key = "FallbackFavorabilityThreshold",
			DisplayName = "好感阈值",
			Description = "关系不在允许范围内时，好感达到该值才给予或相助。",
			GroupName = "NPC请求处理",
			DefaultValue = "25000",
		},
		[12] = {
			SettingType = "Toggle",
			Key = "EnableBackendItemRequestInterceptor",
			DisplayName = "后端物品请求截获",
			Description = "拦截特定 RequestItemAction 路径的请求处理，实现更底层的自动化。",
			GroupName = "NPC请求处理",
			DefaultValue = false,
		},
		[13] = {
			SettingType = "Toggle",
			Key = "EnableBackendGiftInterceptor",
			DisplayName = "后端赠礼截获",
			Description = "命中后调用原版 ApplyChanges 并跳过等待窗口，提升赠礼效率。",
			GroupName = "NPC请求处理",
			DefaultValue = false,
		},

		-- ========================================
		-- 结果窗口处理
		-- ========================================
		[14] = {
			SettingType = "Toggle",
			Key = "EnableResultCategory",
			DisplayName = "结果继续总开关",
			Description = "结果窗口自动继续权限。实际执行还需要打开下面的具体结果继续开关。",
			GroupName = "结果窗口处理",
			DefaultValue = false,
		},
		[15] = {
			SettingType = "Toggle",
			Key = "AutoContinueWhitelistedResults",
			DisplayName = "其他白名单结果继续",
			Description = "结果窗口总开关开启时，自动继续已明确列入白名单、但不属于请求结果或指点结果的窗口。",
			GroupName = "结果窗口处理",
			DefaultValue = false,
		},
		[16] = {
			SettingType = "Toggle",
			Key = "EnableRequestResultContinue",
			DisplayName = "请求结果继续",
			Description = "自动点击请求成功或拒绝后的单一继续选项。",
			GroupName = "结果窗口处理",
			DefaultValue = false,
		},
		[17] = {
			SettingType = "Toggle",
			Key = "EnableGuidanceResultContinue",
			DisplayName = "指点结果继续",
			Description = "自动点击 NPC 指点结果的单一继续选项。",
			GroupName = "结果窗口处理",
			DefaultValue = false,
		},

		-- ========================================
		-- 家庭事件处理
		-- ========================================
		[18] = {
			SettingType = "Toggle",
			Key = "EnableFamilyCategory",
			DisplayName = "家庭事件总开关",
			Description = "家庭相关事件权限。实际执行还需要打开下面的具体事件开关。",
			GroupName = "家庭事件处理",
			DefaultValue = false,
		},
		[19] = {
			SettingType = "Toggle",
			Key = "EnableAdoptAbandonedBaby",
			DisplayName = "收养弃婴",
			Description = "按婴孩立场判断是否收养；不满足条件则搁置。",
			GroupName = "家庭事件处理",
			DefaultValue = false,
		},
		[20] = {
			SettingType = "InputField",
			Key = "AllowedAdoptionBehaviorTypes",
			DisplayName = "允许收养立场",
			Description = "逗号分隔：0刚正、1仁善、2中庸、3叛逆、4唯我。",
			GroupName = "家庭事件处理",
			DefaultValue = "0,1,2",
		},
		[21] = {
			SettingType = "InputField",
			Key = "AdoptionMaxChildAge",
			DisplayName = "婴孩最大年龄",
			Description = "用于从事件角色中识别婴孩，默认 3。",
			GroupName = "家庭事件处理",
			DefaultValue = "3",
		},
		[22] = {
			SettingType = "Toggle",
			Key = "EnablePrenatalEducation",
			DisplayName = "母亲胎教",
			Description = "按配置自动选择母亲胎教选项；目前由前端事件窗口执行。",
			GroupName = "家庭事件处理",
			DefaultValue = false,
		},
		[23] = {
			SettingType = "InputField",
			Key = "PrenatalEducationChoice",
			DisplayName = "默认胎教选项",
			Description = "1轻轻抚慰；2哼唱小曲；3调匀气息。非法值会回退为 1；默认 3。",
			GroupName = "家庭事件处理",
			DefaultValue = "3",
		},
		[24] = {
			SettingType = "Toggle",
			Key = "EnablePrenatalEducationResultContinue",
			DisplayName = "胎教结果退出",
			Description = "胎教选择后，自动点击结果窗口的'心满意足'。默认关闭，玩家会看到胎教处理结果并手动退出。",
			GroupName = "家庭事件处理",
			DefaultValue = false,
		},
		[25] = {
			SettingType = "Toggle",
			Key = "EnableBackendPrenatalEducation",
			DisplayName = "后端胎教截获",
			Description = "使用后端方式处理胎教事件，提供更稳定的自动化体验。",
			GroupName = "家庭事件处理",
			DefaultValue = false,
		},

		-- ========================================
		-- 新生儿取名
		-- ========================================
		[26] = {
			SettingType = "Toggle",
			Key = "EnableBirthNaming",
			DisplayName = "喜得贵子/千金取名",
			Description = "处理喜得贵子/千金的姓氏选择和亲自取名输入。仍需开启'家庭事件处理'总开关。",
			GroupName = "新生儿取名",
			DefaultValue = false,
		},
		[27] = {
			SettingType = "Toggle",
			Key = "TaiwuBirthUseOwnSurname",
			DisplayName = "太吾生产用自己姓氏",
			Description = "当太吾本人生产且出现'以自己的姓氏给孩子取名'时自动选择。若配置了字辈和名尾，则优先进入亲自取名以保证名字带字辈。",
			GroupName = "新生儿取名",
			DefaultValue = true,
		},
		[28] = {
			SettingType = "Toggle",
			Key = "PartnerBirthUseMotherSurname",
			DisplayName = "伴侣生产用母亲姓氏",
			Description = "当太吾妻子/伴侣生产且出现'以母亲的姓氏给孩子取名'时自动选择。默认关闭。",
			GroupName = "新生儿取名",
			DefaultValue = false,
		},
		[29] = {
			SettingType = "Toggle",
			Key = "BirthFallbackManualNaming",
			DisplayName = "未命中姓氏时亲自取名",
			Description = "没有命中姓氏取名选项时，是否进入亲自取名。若关闭则交给玩家。",
			GroupName = "新生儿取名",
			DefaultValue = false,
		},
		[30] = {
			SettingType = "InputField",
			Key = "BirthGenerationCharacter",
			DisplayName = "自定义字辈",
			Description = "只填写一个字。亲自取名时作为孩子'名'的第一个字，不包含姓氏。",
			GroupName = "新生儿取名",
			DefaultValue = "",
		},
		[31] = {
			SettingType = "InputField",
			Key = "BirthGivenNameSuffix",
			DisplayName = "自定义名尾",
			Description = "只填写一个字。与字辈组成孩子的名，例如字辈'承'、名尾'玉'会提交'承玉'。为空时不会自动填写取名输入。",
			GroupName = "新生儿取名",
			DefaultValue = "",
		},

		-- ========================================
		-- 智能选择功能
		-- ========================================
		[32] = {
			SettingType = "Toggle",
			Key = "EnableFrontendAutoSelectCategory",
			DisplayName = "智能选择总开关",
			Description = "前端智能选择权限。记忆选择、关键词选择、单选项继续仍需分别打开。",
			GroupName = "智能选择功能",
			DefaultValue = false,
		},
		[33] = {
			SettingType = "Toggle",
			Key = "EnableFrontendKeywordSelect",
			DisplayName = "关键词选择",
			Description = "启用前端关键词锚定选择。只对已接入的白名单规则生效。",
			GroupName = "智能选择功能",
			DefaultValue = false,
		},
		[34] = {
			SettingType = "Toggle",
			Key = "EnableFrontendRememberSelection",
			DisplayName = "记录玩家手动选择",
			Description = "记录严格签名下的玩家手动选择，供后续复用。",
			GroupName = "智能选择功能",
			DefaultValue = false,
		},
		[35] = {
			SettingType = "Toggle",
			Key = "EnableFrontendMemorySelect",
			DisplayName = "复用玩家记忆选择",
			Description = "仅在事件文本、选项文本、角色和选项列表严格一致时复用选择。",
			GroupName = "智能选择功能",
			DefaultValue = false,
		},
		[36] = {
			SettingType = "Toggle",
			Key = "EnableFrontendSingleOptionContinue",
			DisplayName = "安全单选项继续",
			Description = "高级功能。仅自动点击唯一可用且命中安全继续词的选项；较艺、战斗、奇遇检定等风险窗口会跳过。",
			GroupName = "智能选择功能",
			DefaultValue = false,
		},
		[37] = {
			SettingType = "Toggle",
			Key = "EnableAnySingleOptionContinue",
			DisplayName = "所有单选项跳过",
			Description = "高风险功能。开启后，只要当前事件窗口只有一个可用普通选项，且没有输入框、物品选择、人物选择等额外操作，就自动点击；不再检查继续词白名单。风险由玩家承担。",
			GroupName = "智能选择功能",
			DefaultValue = false,
		},
		[38] = {
			SettingType = "Toggle",
			Key = "EnableCustomDialogSkip",
			DisplayName = "自定义对话跳过",
			Description = "启用玩家自定义记忆的对话自动选择。事件窗口会出现'记住本次选择'开关，规则保存到本 MOD 目录 UserData/custom_dialog_skip.tsv。",
			GroupName = "智能选择功能",
			DefaultValue = false,
		},
		[39] = {
			SettingType = "InputField",
			Key = "CustomDialogSkipSuspendHotkey",
			DisplayName = "禁用自定义对话快捷键",
			Description = "仅在'自定义对话跳过'开启时生效。支持 Ctrl+A、Ctrl+Alt+S、Shift+F8。按一次全局暂停所有玩家自定义自动选择，再按一次恢复；本次游戏会话有效。",
			GroupName = "智能选择功能",
			DefaultValue = "Ctrl+A",
		},
	},
	ChangeConfig = false,
	HasArchive = false,
	NeedRestartWhenSettingChanged = false,
	Cover = "Cover.png",
	WorkshopCover = "Cover.png",
}
