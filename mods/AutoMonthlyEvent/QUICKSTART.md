# 快速开始指南

## 第一次使用

### 1. 编译MOD

双击运行 `build.bat`，脚本会自动：
- 检查.NET SDK是否安装
- 编译Backend项目
- 编译Frontend项目

编译成功后，DLL文件会生成在：
```
Plugins\AutoMonthlyEvent.Backend\bin\Release\netstandard2.1\AutoMonthlyEvent.Backend.dll
Plugins\AutoMonthlyEvent.Frontend\bin\Release\netstandard2.1\AutoMonthlyEvent.Frontend.dll
```

### 2. 配置游戏路径

编辑 `deploy.bat` 文件，修改第8行的游戏路径：
```batch
set GAME_DIR=D:\Games\The Scroll of Taiwu
```
改为你的实际游戏安装目录。

### 3. 部署MOD

双击运行 `deploy.bat`，脚本会自动：
- 创建MOD目录
- 复制Config.lua配置文件
- 复制编译后的DLL文件

### 4. 启动游戏

启动太吾绘卷，MOD会自动加载。

## 验证MOD是否工作

1. 过月时查看Player.log（位于游戏根目录）
2. 搜索 "[AutoMonthlyEvent]" 关键字
3. 应该能看到类似以下日志：
   ```
   [AutoMonthlyEvent] Backend plugin loaded successfully.
   [AutoMonthlyEvent] Frontend plugin loaded successfully.
   ```

## 测试自动处理

1. 开启详细日志：编辑Config.lua，设置 `LogVerbose = true`
2. 过月，观察是否有月度事件
3. 如果只有NormalEvent，应该会自动处理并关闭UI
4. 如果有SpecialEvent或LockedEvent，会保持UI打开让玩家手动处理

## 常见问题

### Q: 编译失败，提示找不到引用
A: 确保TaiwuGameDir环境变量已设置，或在.csproj中硬编码游戏路径

### Q: MOD加载但没有生效
A: 检查Config.lua中EnableAutoProcess是否为true

### Q: 想禁用MOD
A: 编辑Config.lua，设置 `EnableAutoProcess = false`，无需删除文件

### Q: 如何查看日志
A: Player.log位于游戏根目录，GameData_*.log位于游戏根目录的Logs文件夹

## 下一步

- 阅读 README.md 了解详细功能
- 根据需要调整Config.lua配置
- 遇到问题查看故障排除章节

祝你游戏愉快！
