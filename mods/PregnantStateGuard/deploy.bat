@echo off
chcp 65001 >nul
echo ========================================
echo PregnantStateGuard - 部署脚本
echo ========================================
echo.

if "%TAIWU_GAME_DIR%"=="" (
    set "GAME_DIR=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu"
) else (
    set "GAME_DIR=%TAIWU_GAME_DIR%"
)
set "MODS_DIR=%GAME_DIR%\Mod\PregnantStateGuard"

echo 检查游戏目录...
if not exist "%GAME_DIR%" (
    echo [错误] 游戏目录不存在: %GAME_DIR%
    pause
    exit /b 1
)

echo 创建MOD目录...
if not exist "%MODS_DIR%" mkdir "%MODS_DIR%"
if not exist "%MODS_DIR%\Plugins" mkdir "%MODS_DIR%\Plugins"

echo.
echo 复制配置文件...
copy /Y "Config.lua" "%MODS_DIR%\Config.lua" >nul
if errorlevel 1 (
    echo [失败] Config.lua
    pause
    exit /b 1
) else (
    echo [成功] Config.lua
)

echo.
echo 复制后端插件...
if exist "Plugins\PregnantStateGuard.Backend.dll" (
    copy /Y "Plugins\PregnantStateGuard.Backend.dll" "%MODS_DIR%\Plugins\" >nul
) else if exist "Plugins\PregnantStateGuard.Backend\bin\Release\net8.0\PregnantStateGuard.Backend.dll" (
    copy /Y "Plugins\PregnantStateGuard.Backend\bin\Release\net8.0\PregnantStateGuard.Backend.dll" "%MODS_DIR%\Plugins\" >nul
) else (
    echo [错误] 未找到 PregnantStateGuard.Backend.dll，请先执行 build.bat
    pause
    exit /b 1
)

if errorlevel 1 (
    echo [失败] PregnantStateGuard.Backend.dll
    pause
    exit /b 1
) else (
    echo [成功] PregnantStateGuard.Backend.dll
)

echo.
echo ========================================
echo 部署完成
echo MOD目录: %MODS_DIR%
echo ========================================
echo.
pause

