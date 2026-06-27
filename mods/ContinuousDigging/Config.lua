return {
    Title = "连续挖掘",
    Author = "WQHH",
    Version = "1.1.0",
    Description = "[b]连续挖掘 (Continuous Digging)[/b]\n\n厌倦了每次挖到宝物后自动停止？这个 MOD 修改了太吾绘卷原版的连续挖掘逻辑。开启后，你的角色将不知疲倦地持续挖掘，直到地块被掏空、行动力耗尽或挖出了你设定的顶级宝物。\n\n[b]核心功能：[/b]\n[list]\n[*][b]真正的连续挖掘[/b]：突破原版“出宝即停”的限制，实现全自动循环挖掘。\n[*][b]智能退出机制[/b]：支持品级阈值、行动力保护及安全上限设置。\n[*][b]实验性后端模式[/b]：开启后可在后台瞬间完成多次挖掘结算，大幅节省过月等待时间。\n[/list]\n\n[b]注意：[/b]后端模式会跳过动画并将奖励直接送入背包，建议在地块杂物较多时使用。",
    Source = 0,
    GameVersion = "1.0.32",
    FrontendPlugins = {
        [1] = "ContinuousDigging.Frontend.dll",
    },
    Visibility = 2,

    DefaultSettings = {
        [1] = {
            SettingType = "Toggle",
            Key = "EnabledContinuousDigging",
            DisplayName = "启用连续挖掘",
            Description = "启用后修改原版连续挖宝成功即停止的行为。",
            DefaultValue = true,
        },
        [2] = {
            SettingType = "InputField",
            Key = "MaxGradeLimit",
            DisplayName = "最高品级停止阈值",
            Description = "0 表示不因品级停止；1-9 表示挖到该品级或更高品级时停止。数值越小品级越高。",
            DefaultValue = "0",
        },
        [3] = {
            SettingType = "InputField",
            Key = "ActionPointCostPerDig",
            DisplayName = "每次挖掘行动力",
            Description = "原版一次挖宝消耗 3 天，即 30 行动力。仅用于前端模式的提前检查。",
            DefaultValue = "30",
        },
        [4] = {
            SettingType = "Toggle",
            Key = "EnableActionPointCheck",
            DisplayName = "启用行动力检查",
            Description = "剩余行动力不足一次挖掘时停止。",
            DefaultValue = true,
        },
        [5] = {
            SettingType = "InputField",
            Key = "MaxConsecutiveDigs",
            DisplayName = "最大连续挖掘次数",
            Description = "连续挖掘的安全上限，防止异常无限循环。",
            DefaultValue = "50",
        },
        [6] = {
            SettingType = "Toggle",
            Key = "EnableDebugLog",
            DisplayName = "启用调试日志",
            Description = "记录每次结果、继续判断和停止原因。",
            DefaultValue = true,
        },
    },

    ChangeConfig = false,
    HasArchive = false,
    NeedRestartWhenSettingChanged = false,
    Cover = "Cover.png",
    WorkshopCover = "Cover.png",
}
