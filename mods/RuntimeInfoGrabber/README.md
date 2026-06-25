# 运行时信息抓取 (RuntimeInfoGrabber)

## 功能说明

自动抓取太吾绘卷运行时的游戏数据，包括：
- 月度事件配置和运行时数据
- 交互选项配置和运行时数据
- 导出为 JSON/JSONL/CSV 格式供分析使用

## 配置项

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| EnableAutoProcess | Toggle | false | 启用自动处理（暂未实现） |
| DiscoveryMode | Toggle | true | 发现模式，只导出数据不处理事件 |
| DumpToJson | Toggle | true | 将发现的数据写入文件 |
| DumpDirectory | InputField | Dump_out | 输出目录路径（相对于游戏根目录） |
| SkipSpecialEvents | Toggle | true | 跳过特殊事件类型 |
| LogVerbose | Toggle | false | 启用详细日志 |

## 输出文件

导出的数据保存在 `Dump_out/` 目录下：

### 静态配置数据
- `monthly_events_catalog.json` - 所有月度事件配置
- `interaction_options_catalog.json` - 所有交互选项配置

### 运行时数据 (JSONL 格式)
- `runtime_monthly_events.jsonl` - 运行时触发的月度事件
- `runtime_event_options.jsonl` - 运行时的事件选项
- `runtime_monthly_event_options.jsonl` - 月度事件的选项
- `runtime_monthly_event_windows.jsonl` - 月度事件窗口数据
- `runtime_monthly_event_flow.jsonl` - 月度事件流程数据
- `runtime_monthly_event_candidates.jsonl` - 候选月度事件

### CSV 格式 (data/ 目录)
- `monthly_events_catalog.csv` - 月度事件配置（480条）
- `interaction_options_catalog.csv` - 交互选项配置（141条）
- `runtime_event_options.csv` - 运行时事件选项（30条）

## 使用方法

1. 将 MOD 文件夹复制到游戏的 `Mod/` 目录
2. 启动游戏，在 MOD 管理中启用"运行时信息抓取"
3. 进入游戏后，数据会自动导出到 `Dump_out/` 目录
4. 查看 `data/README.md` 了解数据字段说明

## 技术架构

- **Backend**: RuntimeInfoGrabber.Backend.dll - 后端插件（预留扩展）
- **Frontend**: RuntimeInfoGrabber.Frontend.dll - 前端插件（Harmony Patch + 数据导出）
- **核心类**: DiscoveryDumper - 负责数据抓取和导出

## 开发信息

- **作者**: WQMYH
- **版本**: 0.1.0.0
- **游戏版本**: 0.84.54-test
- **许可证**: MIT
