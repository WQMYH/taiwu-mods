return {
    Title = "功法框架·测试版",
    Author = "WQMYH",
    Version = "0.1.0.0",
    Description = "功法框架测试版 0.1.0.0。当前版本提供游戏内功法数据读取、搜索筛选、详情查看、简易编辑，以及 JSON/CSV 导入、JSON 导出、差异预览、自动备份和前后端一致性诊断。所有定义、导出和日志均保存在本 MOD 目录。后续版本将依次支持指定门派新增功法、复用原版特效、JSON 组合功法效果、按原版梯度随机生成功法，以及通过稳定 API 接入外部特效 DLL。",
    Source = 0,
    GameVersion = "1.0.40",
    FrontendPlugins = {
        [1] = "GongfaFramework.test.Frontend.dll",
    },
    BackendPlugins = {
        [1] = "GongfaFramework.test.Backend.dll",
    },
    TagList = {
        [1] = "Modifications",
        [2] = "Utilities",
    },
    Visibility = 2,
    DefaultSettings = {
        {
            SettingType = "Toggle",
            Key = "EnableDefinitionLoading",
            DisplayName = "加载外部功法定义",
            Description = "启动时从本 MOD 的 Definitions 目录加载 JSON/CSV 字段补丁。",
            DefaultValue = true,
        },
        {
            SettingType = "InputField",
            Key = "FrameworkPanelHotkey",
            DisplayName = "功法框架界面快捷键",
            Description = "支持 Ctrl、Alt、Shift 与一个主键，例如 Ctrl+F8 或 Ctrl+Shift+G。",
            DefaultValue = "Ctrl+F8",
        },
        {
            SettingType = "Toggle",
            Key = "EnableLogging",
            DisplayName = "启用完整日志",
            Description = "日志写入本 MOD 的 UserData/logs 目录；致命错误始终写入游戏日志。",
            DefaultValue = true,
        },
    },
    ChangeConfig = false,
    HasArchive = false,
    NeedRestartWhenSettingChanged = true,
}
