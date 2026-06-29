@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "GAME_MOD_DIR=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\CharacterStudio"

echo ========================================
echo   人物工坊 (CharacterStudio) - 部署脚本
echo ========================================

echo [1/3] 编译项目...
call build.bat
if errorlevel 1 (
    echo [Error] 编译失败，请检查错误信息。
    pause
    exit /b 1
)

echo [2/3] 准备 MOD 目录...
if not exist "%GAME_MOD_DIR%" mkdir "%GAME_MOD_DIR%"
if not exist "%GAME_MOD_DIR%\Plugins" mkdir "%GAME_MOD_DIR%\Plugins"

echo [3/3] 复制文件...
copy /Y "Config.lua" "%GAME_MOD_DIR%\Config.lua" >nul
copy /Y "Plugins\CharacterStudio.Backend.dll" "%GAME_MOD_DIR%\Plugins\CharacterStudio.Backend.dll" >nul
copy /Y "Plugins\CharacterStudio.Frontend.dll" "%GAME_MOD_DIR%\Plugins\CharacterStudio.Frontend.dll" >nul

echo.
echo ========================================
echo   部署完成！
echo ========================================
echo MOD 位置: %GAME_MOD_DIR%
echo.
echo 请在游戏中启用 MOD 并重启游戏以应用更改。
pause
