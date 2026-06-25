@echo off
echo ========================================
echo AutoMonthlyEvent.Executor deploy
echo ========================================
echo.

if "%TAIWU_GAME_DIR%"=="" (
    set "GAME_DIR=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu"
) else (
    set "GAME_DIR=%TAIWU_GAME_DIR%"
)
set "MODS_DIR=%GAME_DIR%\Mod\AutoMonthlyEvent.Executor"

if not exist "%GAME_DIR%" (
    echo [ERROR] Game directory does not exist: %GAME_DIR%
    exit /b 1
)

if not exist "%MODS_DIR%" mkdir "%MODS_DIR%"
if not exist "%MODS_DIR%\Plugins" mkdir "%MODS_DIR%\Plugins"

copy /Y "Config.lua" "%MODS_DIR%\Config.lua" >nul
if %errorlevel% neq 0 (
    echo [FAILED] Config.lua
    exit /b 1
)
echo [OK] Config.lua

if not exist "Plugins\AutoMonthlyEvent.Executor.Frontend\bin\Release\netstandard2.1\AutoMonthlyEvent.Executor.Frontend.dll" (
    echo [ERROR] Frontend DLL not found. Run build.bat first.
    exit /b 1
)

copy /Y "Plugins\AutoMonthlyEvent.Executor.Frontend\bin\Release\netstandard2.1\AutoMonthlyEvent.Executor.Frontend.dll" "%MODS_DIR%\Plugins\" >nul
if %errorlevel% neq 0 (
    echo [FAILED] AutoMonthlyEvent.Executor.Frontend.dll
    exit /b 1
)
echo [OK] AutoMonthlyEvent.Executor.Frontend.dll

echo.
echo [OK] Deployed to %MODS_DIR%
