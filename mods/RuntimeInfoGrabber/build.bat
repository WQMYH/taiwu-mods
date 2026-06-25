@echo off
chcp 65001 >nul
echo ========================================
echo RuntimeInfoGrabber - 构建脚本
echo ========================================
echo.

pushd "%~dp0Plugins\RuntimeInfoGrabber.Backend"
dotnet build -c Release
if errorlevel 1 (
    popd
    echo.
    echo [失败] 后端插件构建失败
    pause
    exit /b 1
)
popd

pushd "%~dp0Plugins\RuntimeInfoGrabber.Frontend"
dotnet build -c Release
if errorlevel 1 (
    popd
    echo.
    echo [失败] 前端插件构建失败
    pause
    exit /b 1
)
popd

echo.
echo [成功] RuntimeInfoGrabber DLL 构建完成
echo.
pause
