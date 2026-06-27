# FullDiscovery 全量信息抓取器

FullDiscovery 是只读型 Taiwu MOD 开发辅助工具，用于导出静态配置、运行时事件窗口、月度事件集合和 API 反射索引。

默认输出目录：

`游戏根目录/Dump_out/FullDiscovery`

## 输出结构

- `static/config/`：通用 Config 表 JSON。
- `static/focused/`：月度事件、事件选项、建筑、人物模板、人物特性等重点索引。
- `runtime/events/event_windows.jsonl`：运行时事件窗口。
- `runtime/monthly/monthly_collections.jsonl`：月度通知集合。
- `api/types.jsonl`、`api/methods.jsonl`、`api/members.jsonl`：反射索引。
- `logs/full_discovery.log`：人类可读日志。
- `manifest.json`：本次导出清单。

## 安全边界

本 MOD 不自动选择事件，不推进事件流程，不修改存档。代码中不应出现 `EventSelect`、`HandleMonthlyEvent`、`EventModel.Select`、`SetInputResult` 等执行入口调用。

## 使用

1. 构建：运行 `build.bat`。
2. 部署：运行 `deploy.bat`，或手动复制到游戏 `Mod/FullDiscovery`。
3. 启动游戏并进入存档。
4. 查看 `Dump_out/FullDiscovery`。
