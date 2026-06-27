@echo off
chcp 65001 >nul
setlocal

set "MOD_NAME=ContinuousDigging"
set "SOURCE_DIR=%~dp0"
if "%TAIWU_GAME_DIR%"=="" (
    set "GAME_DIR=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu"
) else (
    set "GAME_DIR=%TAIWU_GAME_DIR%"
)
set "TARGET_DIR=%GAME_DIR%\Mod\%MOD_NAME%"

call "%SOURCE_DIR%build.bat"
if errorlevel 1 exit /b 1

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
if not exist "%TARGET_DIR%\Plugins" mkdir "%TARGET_DIR%\Plugins"

copy /Y "%SOURCE_DIR%Config.lua" "%TARGET_DIR%\Config.lua" >nul
copy /Y "%SOURCE_DIR%Plugins\ContinuousDigging.Frontend.dll" "%TARGET_DIR%\Plugins\ContinuousDigging.Frontend.dll" >nul
copy /Y "%SOURCE_DIR%Plugins\ContinuousDigging.Frontend.pdb" "%TARGET_DIR%\Plugins\ContinuousDigging.Frontend.pdb" >nul
if exist "%TARGET_DIR%\Plugins\ContinuousDigging.Backend.dll" del /q "%TARGET_DIR%\Plugins\ContinuousDigging.Backend.dll"
if exist "%TARGET_DIR%\Plugins\ContinuousDigging.Backend.pdb" del /q "%TARGET_DIR%\Plugins\ContinuousDigging.Backend.pdb"

echo [OK] Deployed to "%TARGET_DIR%"
