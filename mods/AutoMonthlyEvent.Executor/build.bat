@echo off
echo ========================================
echo AutoMonthlyEvent.Executor build
echo ========================================
echo.

dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK was not found.
    exit /b 1
)

echo Building Frontend...
cd Plugins\AutoMonthlyEvent.Executor.Frontend
dotnet build -c Release
if %errorlevel% neq 0 (
    echo [FAILED] Frontend build failed.
    cd ..\..
    exit /b 1
)
cd ..\..

echo.
echo [OK] Build completed.
echo Output: Plugins\AutoMonthlyEvent.Executor.Frontend\bin\Release\netstandard2.1\
