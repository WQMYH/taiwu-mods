@echo off
chcp 65001 >nul
echo ========================================
echo ContinuousDigging build
echo ========================================
echo.

cd /d "%~dp0"

if exist "src\Frontend\bin" rmdir /s /q "src\Frontend\bin"
if exist "src\Frontend\obj" rmdir /s /q "src\Frontend\obj"
if exist "src\Backend\bin" rmdir /s /q "src\Backend\bin"
if exist "src\Backend\obj" rmdir /s /q "src\Backend\obj"
if not exist "Plugins" mkdir "Plugins"

dotnet build src\Frontend\ContinuousDigging.Frontend.csproj -c Release
if errorlevel 1 (
    echo [FAILED] Frontend build failed
    exit /b 1
)

copy /Y "src\Frontend\bin\Release\netstandard2.1\ContinuousDigging.Frontend.dll" "Plugins\ContinuousDigging.Frontend.dll" >nul
copy /Y "src\Frontend\bin\Release\netstandard2.1\ContinuousDigging.Frontend.pdb" "Plugins\ContinuousDigging.Frontend.pdb" >nul

dotnet build src\Backend\ContinuousDigging.Backend.csproj -c Release
if errorlevel 1 (
    echo [FAILED] Backend build failed
    exit /b 1
)

copy /Y "src\Backend\bin\Release\net8.0\ContinuousDigging.Backend.dll" "Plugins\ContinuousDigging.Backend.dll" >nul
copy /Y "src\Backend\bin\Release\net8.0\ContinuousDigging.Backend.pdb" "Plugins\ContinuousDigging.Backend.pdb" >nul

echo.
echo [OK] ContinuousDigging build completed
