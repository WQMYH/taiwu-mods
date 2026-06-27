return {
    Title = "连续挖掘",
    Author = "MOD Developer",
    Version = "1.1.0",
    Description = "修改连续挖宝成功后自动停止的行为。默认使用前端续挖；可选启用后端批量结算。",
    Source = 0,
    GameVersion = "1.0.32",
    FrontendPlugins = {
        [1] = "ContinuousDigging.Frontend.dll",
    },
    BackendPlugins = {
        [1] = "ContinuousDigging.Backend.dll",
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
            SettingType = "Toggle",
            Key = "EnableBackendPatch",
            DisplayName = "启用后端批量挖掘（实验性）",
            Description = "默认关闭。开启后后端会接管所有挖宝请求（包括“挖掘一次”）并连续调用原版结算；前端续挖会自动停用。奖励全部进入背包，但窗口只显示最后一次有效结果。",
            DefaultValue = false,
        },
        [3] = {
            SettingType = "InputField",
            Key = "MaxGradeLimit",
            DisplayName = "最高品级停止阈值",
            Description = "0 表示不因品级停止；1-9 表示挖到该品级或更高品级时停止。数值越小品级越高。",
            DefaultValue = "0",
        },
        [4] = {
            SettingType = "InputField",
            Key = "ActionPointCostPerDig",
            DisplayName = "每次挖掘行动力",
            Description = "原版一次挖宝消耗 3 天，即 30 行动力。仅用于前端模式的提前检查。",
            DefaultValue = "30",
        },
        [5] = {
            SettingType = "Toggle",
            Key = "EnableActionPointCheck",
            DisplayName = "启用行动力检查",
            Description = "前端模式下，剩余行动力不足一次挖掘时停止。后端始终使用原版剩余天数检查。",
            DefaultValue = true,
        },
        [6] = {
            SettingType = "InputField",
            Key = "MaxConsecutiveDigs",
            DisplayName = "最大连续挖掘次数",
            Description = "前端和后端共用的安全上限，防止异常无限循环。",
            DefaultValue = "50",
        },
        [7] = {
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
