@echo off
chcp 65001 >nul
echo ========================================
echo 蓝图宽度转换工具 - 测试脚本
echo ========================================
echo.

REM 检查输入参数
if "%1"=="" (
    echo 用法: test_convert.bat [输入文件] [目标宽度]
    echo 示例: test_convert.bat blueprint_18.bin 24
    echo.
    pause
    exit /b 1
)

if "%2"=="" (
    echo 错误: 请提供目标宽度参数
    echo 用法: test_convert.bat [输入文件] [目标宽度]
    pause
    exit /b 1
)

set INPUT_FILE=%1
set TARGET_WIDTH=%2
set OUTPUT_FILE=%INPUT_FILE:.bin=_width%TARGET_WIDTH%.bin%

echo 输入文件: %INPUT_FILE%
echo 目标宽度: %TARGET_WIDTH%
echo 输出文件: %OUTPUT_FILE%
echo.

REM 检查输入文件是否存在
if not exist "%INPUT_FILE%" (
    echo 错误: 输入文件不存在: %INPUT_FILE%
    pause
    exit /b 1
)

REM 调用转换工具（需要通过游戏MOD调用，这里只是演示）
echo 注意: 此转换功能需要在游戏中通过 MOD 界面调用
echo.
echo 当前Backend DLL已编译完成，位于:
echo   Plugins\CopyBuildingModernized.Backend.dll
echo.
echo 下一步:
echo   1. 将 DLL 复制到游戏 MOD 目录
echo   2. 启动游戏
echo   3. 通过前端界面调用 ConvertVillageWidth 方法
echo.

pause
