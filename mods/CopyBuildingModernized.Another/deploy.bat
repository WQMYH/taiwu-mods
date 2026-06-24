@echo off
chcp 65001 >nul
echo ========================================
echo 蓝图宽度转换工具 - 部署脚本
echo ========================================
echo.

REM 设置游戏MOD目录（根据实际情况修改）
set GAME_MOD_DIR=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\CopyBuildingModernized.Another

echo 检查源文件...
if not exist "Plugins\CopyBuildingModernized.Backend.dll" (
    echo 错误: 找不到 Backend DLL
    pause
    exit /b 1
)

if not exist "Plugins\CopyBuildingModernized.Frontend.dll" (
    echo 错误: 找不到 Frontend DLL
    pause
    exit /b 1
)

echo.
echo 源文件检查通过 ✓
echo   - CopyBuildingModernized.Backend.dll
echo   - CopyBuildingModernized.Frontend.dll
echo.

REM 创建目标目录
if not exist "%GAME_MOD_DIR%" (
    echo 创建MOD目录: %GAME_MOD_DIR%
    mkdir "%GAME_MOD_DIR%" 2>nul
    if errorlevel 1 (
        echo 错误: 无法创建目录
        pause
        exit /b 1
    )
)

if not exist "%GAME_MOD_DIR%\Plugins" (
    echo 创建Plugins目录...
    mkdir "%GAME_MOD_DIR%\Plugins" 2>nul
)

echo.
echo 正在复制文件...
copy /Y "Plugins\CopyBuildingModernized.Backend.dll" "%GAME_MOD_DIR%\Plugins\" >nul
copy /Y "Plugins\CopyBuildingModernized.Frontend.dll" "%GAME_MOD_DIR%\Plugins\" >nul

if errorlevel 1 (
    echo 错误: 文件复制失败
    pause
    exit /b 1
)

echo.
echo 复制其他必要文件...
if exist "Config.lua" copy /Y "Config.lua" "%GAME_MOD_DIR%\" >nul
if exist "Settings.Lua" copy /Y "Settings.Lua" "%GAME_MOD_DIR%\" >nul
if exist "Cover.png" copy /Y "Cover.png" "%GAME_MOD_DIR%\" >nul

echo.
echo ========================================
echo ✅ 部署完成！
echo ========================================
echo.
echo MOD目录: %GAME_MOD_DIR%
echo.
echo 已部署文件:
echo   Plugins\CopyBuildingModernized.Backend.dll
echo   Plugins\CopyBuildingModernized.Frontend.dll
echo.
echo 下一步:
echo   1. 启动游戏
echo   2. 在MOD管理器中启用 CopyBuildingModernized.Another
echo   3. 进入太吾村产业管理界面
echo   4. 查看"转换宽度"按钮
echo.
pause
