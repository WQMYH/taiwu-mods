@echo off
echo ========================================
echo AutoMonthlyEvent MOD 编译脚本
echo ========================================
echo.

echo 检查.NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到.NET SDK，请先安装.NET SDK
    pause
    exit /b 1
)

echo.
echo 开始编译Backend项目...
cd Plugins\AutoMonthlyEvent.Backend
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [失败] Backend编译失败
    cd ..\..
    pause
    exit /b 1
)
echo [成功] Backend编译完成
cd ..\..

echo.
echo 开始编译Frontend项目...
cd Plugins\AutoMonthlyEvent.Frontend
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [失败] Frontend编译失败
    cd ..\..
    pause
    exit /b 1
)
echo [成功] Frontend编译完成
cd ..\..

echo.
echo ========================================
echo 编译完成！
echo 输出目录:
echo   Backend: Plugins\AutoMonthlyEvent.Backend\bin\Release\netstandard2.1\
echo   Frontend: Plugins\AutoMonthlyEvent.Frontend\bin\Release\netstandard2.1\
echo ========================================
echo.
echo 提示: 运行 deploy.bat 将MOD部署到游戏目录
echo.
pause
