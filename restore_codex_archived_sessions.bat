@echo off
chcp 65001 >nul
echo ========================================
echo   Codex Archived Sessions Manager
echo ========================================
echo.

set CODEX_HOME=E:\Programming\IDE\.codex
set ARCHIVED_DIR=%CODEX_HOME%\archived_sessions
set SESSIONS_DIR=%CODEX_HOME%\sessions

echo [1/3] Listing archived sessions...
echo.
dir "%ARCHIVED_DIR%" /b | findstr "rollout"
echo.

echo Total archived sessions: 
dir "%ARCHIVED_DIR%" /b | findstr "rollout" | find /c /v ""
echo.

echo ========================================
echo Options:
echo ========================================
echo.
echo 1. Restore ALL archived sessions to active
echo 2. Restore specific session by date
echo 3. View session details
echo 4. Cancel
echo.

set /p choice="Enter your choice (1-4): "

if "%choice%"=="1" goto restore_all
if "%choice%"=="2" goto restore_specific
if "%choice%"=="3" goto view_details
if "%choice%"=="4" goto end
goto end

:restore_all
echo.
echo [2/3] Restoring all archived sessions...
echo.

for %%f in ("%ARCHIVED_DIR%\rollout-*.jsonl") do (
    echo Processing: %%~nf
    
    REM Extract date from filename (format: rollout-YYYY-MM-DDTHH-MM-SS-uuid.jsonl)
    set "filename=%%~nf"
    set "date_part=!filename:~8,10!"
    
    REM Create year/month/day directories
    for /f "tokens=1-3 delims=-" %%a in ("!date_part!") do (
        set "year=%%a"
        set "month=%%b"
        set "day=%%c"
    )
    
    REM Remove T from date
    set "year=!year!"
    set "month=!month!"
    set "day=!day:~0,2!"
    
    mkdir "%SESSIONS_DIR%\!year!\!month!\!day!" 2>nul
    
    move "%%f" "%SESSIONS_DIR%\!year!\!month!\!day!\" >nul
    echo   -> Moved to sessions\!year!\!month!\!day!\
)

echo.
echo [3/3] Complete!
echo.
echo Please restart Codex to see restored sessions.
pause
goto end

:restore_specific
echo.
set /p date="Enter date to restore (YYYY-MM-DD): "

REM Parse date
for /f "tokens=1-3 delims=-" %%a in ("%date%") do (
    set "year=%%a"
    set "month=%%b"
    set "day=%%c"
)

mkdir "%SESSIONS_DIR%\%year%\%month%\%day%" 2>nul

echo.
echo Moving sessions from %date%...
move "%ARCHIVED_DIR%\rollout-%date%T*.jsonl" "%SESSIONS_DIR%\%year%\%month%\%day%\" 2>nul

if %errorlevel% equ 0 (
    echo Success!
) else (
    echo No sessions found for that date or already moved.
)

echo.
echo Please restart Codex.
pause
goto end

:view_details
echo.
set /p session_file="Enter session filename (or drag file here): "

if exist "%ARCHIVED_DIR%\%session_file%" (
    echo.
    echo Session details:
    echo File: %session_file%
    echo Location: %ARCHIVED_DIR%
    echo.
    echo First line (metadata):
    powershell -Command "Get-Content '%ARCHIVED_DIR%\%session_file%' -TotalCount 1 | ConvertFrom-Json | ConvertTo-Json -Depth 5"
) else (
    echo File not found: %session_file%
)

pause
goto end

:end
echo.
echo Done.
