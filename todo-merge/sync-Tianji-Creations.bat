@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
echo ========================================
echo   Sync Tianji-Creations from Workshop
echo ========================================
echo.
echo This script syncs Plugins folder from Steam Workshop to merged directory
echo.

:: Configuration
set WORKSHOP_BASE=a:\SteamLibrary\steamapps\workshop\content\838350
set TARGET_DIR=%~dp0merged\Tianji-Creations\Plugins
set TEMP_DIR=%~dp0temp_sync_check

:: Check if target directory exists
if not exist "%TARGET_DIR%" (
    echo [ERROR] Target directory not found: %TARGET_DIR%
    echo Please run merge.bat first to create the merged mod
    pause
    exit /b 1
)

echo Target: %TARGET_DIR%
echo.

:: Step 1: Check workshop directories
echo [Step 1/3] Checking Workshop mods...
set /a MISSING_COUNT=0
set /a FOUND_COUNT=0

call :check_mod 3737804824
call :check_mod 3746015837
call :check_mod 3746021415
call :check_mod 3746022765
call :check_mod 3746024435
call :check_mod 3746071342

echo.
if !MISSING_COUNT! GTR 0 (
    echo [WARNING] !MISSING_COUNT! mod(s^) not found in Workshop!
    echo Please subscribe to the missing mods in Steam Workshop:
    call :show_missing 3737804824
    call :show_missing 3746015837
    call :show_missing 3746021415
    call :show_missing 3746022765
    call :show_missing 3746024435
    call :show_missing 3746071342
    echo.
    set /p CONTINUE="Continue with available mods? (y/n): "
    if /i "!CONTINUE!"=="n" (
        echo Cancelled.
        pause
        exit /b 0
    )
)

if !FOUND_COUNT! EQU 0 (
    echo [ERROR] No workshop mods found. Cannot proceed.
    pause
    exit /b 1
)

goto :after_check

:check_mod
if exist "%WORKSHOP_BASE%\%1\Plugins\" (
    echo [OK] Found mod %1
    set /a FOUND_COUNT+=1
) else (
    echo [MISSING] Mod %1 not found in Workshop
    set /a MISSING_COUNT+=1
)
goto :eof

:show_missing
if not exist "%WORKSHOP_BASE%\%1\Plugins\" (
    echo   - https://steamcommunity.com/sharedfiles/filedetails/?id=%1
)
goto :eof

:after_check

:: Step 2: Backup current Plugins for comparison
echo.
echo [Step 2/3] Backing up current Plugins for comparison...
if exist "%TEMP_DIR%" rmdir /S /Q "%TEMP_DIR%"
mkdir "%TEMP_DIR%"
xcopy "%TARGET_DIR%\*" "%TEMP_DIR%\" /E /I /Y >nul
echo [OK] Backup created for comparison

:: Step 3: Sync Plugins from all available workshop mods
echo.
echo [Step 3/3] Syncing Plugins from Workshop...
set /a SYNCED_COUNT=0

call :sync_mod 3737804824
call :sync_mod 3746015837
call :sync_mod 3746021415
call :sync_mod 3746022765
call :sync_mod 3746024435
call :sync_mod 3746071342

echo [OK] Synced !SYNCED_COUNT! mod(s^)

goto :after_sync

:sync_mod
if exist "%WORKSHOP_BASE%\%1\Plugins\" (
    echo Syncing mod %1...
    xcopy "%WORKSHOP_BASE%\%1\Plugins\*" "%TARGET_DIR%\" /E /I /Y >nul
    set /a SYNCED_COUNT+=1
)
goto :eof

:after_sync

:: Step 4: Verify changes
echo.
echo [Verify] Checking file changes...
echo.

:: Count files before and after
set /a BEFORE_COUNT=0
set /a AFTER_COUNT=0

for %%f in ("%TEMP_DIR%\*.dll") do set /a BEFORE_COUNT+=1
for %%f in ("%TARGET_DIR%\*.dll") do set /a AFTER_COUNT+=1

echo Files before sync: !BEFORE_COUNT!
echo Files after sync:  !AFTER_COUNT!
echo.

:: Find added files
echo Added files:
set /a ADDED=0
for %%f in ("%TARGET_DIR%\*.dll") do (
    if not exist "%TEMP_DIR%\%%~nxf" (
        echo   + %%~nxf
        set /a ADDED+=1
    )
)
if !ADDED! EQU 0 echo   (none^)
echo.

:: Find removed files
echo Removed files:
set /a REMOVED=0
for %%f in ("%TEMP_DIR%\*.dll") do (
    if not exist "%TARGET_DIR%\%%~nxf" (
        echo   - %%~nxf
        set /a REMOVED+=1
    )
)
if !REMOVED! EQU 0 echo   (none^)
echo.

:: Cleanup temp directory
rmdir /S /Q "%TEMP_DIR%"

echo ========================================
echo   Sync Complete!
echo ========================================
echo.
echo Summary:
echo   - Mods checked: 6
echo   - Mods found: !FOUND_COUNT!
echo   - Mods synced: !SYNCED_COUNT!
echo   - Files added: !ADDED!
echo   - Files removed: !REMOVED!
echo.
if !MISSING_COUNT! GTR 0 (
    echo [WARNING] !MISSING_COUNT! mod(s^) were missing from Workshop
    echo Please subscribe to them for complete sync
)
echo.
pause
