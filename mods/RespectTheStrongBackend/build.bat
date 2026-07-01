@echo off
chcp 65001 >nul
pushd "%~dp0Plugins\RespectTheStrongBackend"
dotnet build -c Release
set "RESULT=%ERRORLEVEL%"
popd
if not "%RESULT%"=="0" (
    echo [失败] RespectTheStrongBackend 构建失败
    exit /b %RESULT%
)
echo [成功] Plugins\RespectTheStrongBackend.dll
