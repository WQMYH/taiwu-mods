@echo off
chcp 65001 >nul
echo ========================================
echo PregnantStateGuard - 构建脚本
echo ========================================
echo.

pushd "%~dp0Plugins\PregnantStateGuard.Backend"
dotnet build -c Release
if errorlevel 1 (
    popd
    echo.
    echo [失败] 后端插件构建失败
    pause
    exit /b 1
)
popd

echo.
echo [成功] PregnantStateGuard.Backend.dll 构建完成
echo.
pause

