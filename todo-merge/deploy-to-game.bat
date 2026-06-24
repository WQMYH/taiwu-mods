@echo off
chcp 65001 >nul
echo ========================================
echo   Deploy to Game Mod Directory
echo ========================================
echo.
echo This script deploys all contents from merged/ to game's Mod directory
echo.

:: Configuration
set SOURCE_DIR=%~dp0merged
set GAME_MOD_DIR=a:\SteamLibrary\steamapps\common\The Scroll of Taiwu\Mod

:: Check if source directory exists
if not exist "%SOURCE_DIR%" (
    echo [ERROR] Source directory not found: %SOURCE_DIR%
    echo Please run merge.bat first to create Tianji-Creations directory
    pause
    exit /b 1
)

echo Source: %SOURCE_DIR%
echo Target: %GAME_MOD_DIR%
echo.

:: Ask for confirmation
echo WARNING: This will overwrite files in the game's Mod directory!
set /p CONFIRM="Continue? (y/n): "
if /i not "%CONFIRM%"=="y" (
    echo Cancelled.
    pause
    exit /b 0
)

echo.
echo [1/2] Preparing target directory...
if not exist "%GAME_MOD_DIR%" (
    echo [ERROR] Game Mod directory not found: %GAME_MOD_DIR%
    pause
    exit /b 1
)
echo [OK] Target directory ready
echo.

echo [2/2] Deploying all files from merged/...
:: Copy all contents from merged/ to game's Mod directory
xcopy "%SOURCE_DIR%\*" "%GAME_MOD_DIR%\" /E /I /Y >nul
echo [OK] All files deployed
echo.

echo [Verify] Checking deployment...
set MISSING=0

if exist "%GAME_MOD_DIR%\Tianji-Creations\Config.lua" (
    echo [OK] Tianji-Creations/Config.lua
) else (
    echo [FAIL] Config.lua missing!
    set /a MISSING+=1
)

if exist "%GAME_MOD_DIR%\Tianji-Creations\Plugins\" (
    set PLUGIN_COUNT=0
    for %%f in ("%GAME_MOD_DIR%\Tianji-Creations\Plugins\*.dll") do set /a PLUGIN_COUNT+=1
    echo [OK] Tianji-Creations/Plugins/ (!PLUGIN_COUNT! DLLs)
) else (
    echo [FAIL] Plugins directory missing!
    set /a MISSING+=1
)

if exist "%GAME_MOD_DIR%\Tianji-Creations\TianDao\" (
    echo [OK] Tianji-Creations/TianDao/
) else (
    echo [FAIL] TianDao directory missing!
    set /a MISSING+=1
)

if exist "%GAME_MOD_DIR%\Tianji-Creations\幻梦蝶.png" (
    echo [OK] Cover image
) else (
    echo [WARN] Cover image missing (optional)
)

echo.
if %MISSING% equ 0 (
    echo [SUCCESS] All critical files verified!
) else (
    echo [WARNING] !MISSING! critical file(s) missing!
)

echo.
echo ========================================
echo   Deployment Complete!
echo ========================================
echo.
echo Source: %SOURCE_DIR%
echo Target: %GAME_MOD_DIR%
echo.
echo All contents from merged/ have been deployed to game's Mod directory.
echo Mod location: %GAME_MOD_DIR%\Tianji-Creations\
echo.
echo Next Steps:
echo 1. Open Taiwu game
echo 2. Go to Mod Manager
echo 3. Enable "Tianji Creations" mod
echo 4. Disable old individual mods if enabled
echo 5. Test all features
echo.
pause
