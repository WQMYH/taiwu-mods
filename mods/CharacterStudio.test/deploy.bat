@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "GAME_MOD_DIR=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu\Mod\CharacterStudio.test"

echo ========================================
echo   人物工坊[测试版] (CharacterStudio.test) - 部署脚本
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
if not exist "%GAME_MOD_DIR%\Profiles" mkdir "%GAME_MOD_DIR%\Profiles"
if not exist "%GAME_MOD_DIR%\UserData" mkdir "%GAME_MOD_DIR%\UserData"
if not exist "%GAME_MOD_DIR%\Languages" mkdir "%GAME_MOD_DIR%\Languages"

echo [3/3] 复制文件...
copy /Y "Config.lua" "%GAME_MOD_DIR%\Config.lua" >nul
copy /Y "Plugins\CharacterStudio.Backend.dll" "%GAME_MOD_DIR%\Plugins\CharacterStudio.Backend.dll" >nul
copy /Y "Plugins\CharacterStudio.Frontend.dll" "%GAME_MOD_DIR%\Plugins\CharacterStudio.Frontend.dll" >nul
copy /Y "Profiles\preset_profiles.json" "%GAME_MOD_DIR%\Profiles\preset_profiles.json" >nul
copy /Y "Profiles\character_rules.json" "%GAME_MOD_DIR%\Profiles\character_rules.json" >nul
copy /Y "Profiles\README.md" "%GAME_MOD_DIR%\Profiles\README.md" >nul
if not exist "%GAME_MOD_DIR%\UserData\character_profiles.json" copy /Y "UserData\character_profiles.json" "%GAME_MOD_DIR%\UserData\character_profiles.json" >nul
copy /Y "Languages\zh-Hans.lng" "%GAME_MOD_DIR%\Languages\zh-Hans.lng" >nul
copy /Y "Languages\en-US.lng" "%GAME_MOD_DIR%\Languages\en-US.lng" >nul

echo.
echo ========================================
echo   部署完成！
echo ========================================
echo MOD 位置: %GAME_MOD_DIR%
echo.
echo 请在游戏中启用 MOD 并重启游戏以应用更改。
pause
