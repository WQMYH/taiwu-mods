# FullDiscovery 全量运行时信息抓取器计划

## Summary

新建独立只读 mod：`mods/FullDiscovery`。该 mod 不覆盖 `RuntimeInfoGrabber`，不自动处理事件，不修改存档，只抓取静态配置、运行时事件窗口、月度事件集合和 API 反射索引。

默认输出到：

`游戏根目录/Dump_out/FullDiscovery`

## 可获得的信息

- 静态配置表：通过反射枚举 `Config` 命名空间下带 `Instance` 且可枚举的配置表，导出原始 public 字段/属性。
- 重点索引：月度事件、事件选项、建筑、人物模板、人物特性、人物称号等常用表额外生成轻量索引。
- 运行时事件窗口：通过 `EventModel.OnNotifyGameData` 记录实际显示的事件 GUID、文本、角色 ID、选项和 ExtraData 摘要。
- 月度集合：继续尝试 Patch `UI_MonthNotify.OnNotifyGameData`，记录 `MonthlyEventCollection`；入口失效时写日志并依赖 EventModel 补充。
- API 辅助：反射已加载程序集，生成类型、方法、字段、属性索引，仅记录元数据，不调用方法。

## 实施范围

- 新建 `FullDiscovery.Frontend` 和 `FullDiscovery.Backend`。
- Frontend 承担抓取、导出、Patch 和 API 索引。
- Backend 仅保留只读插件骨架，方便后续扩展。
- `Config.lua` 采用中文分组配置：基础、静态配置、运行时事件、API 辅助、性能保护。
- 输出按模块分目录：`static/config`、`static/focused`、`runtime/events`、`runtime/monthly`、`api`、`logs`。

## 安全边界

代码中不应调用：

- `EventSelect`
- `HandleMonthlyEvent`
- `EventModel.Select`
- `SetInputResult`

FullDiscovery 永远不负责自动点击、自动选择、流程推进或存档修改。

## 验证

- `dotnet build -c Release` 验证 Frontend/Backend。
- `build.bat` 应能生成并复制 DLL/PDB 到 `mods/FullDiscovery/Plugins`。
- 静态搜索确认不存在事件推进入口调用。
- 游戏内验证时检查 `Dump_out/FullDiscovery/manifest.json`、静态配置、重点索引、运行时事件 JSONL 和日志文件。
