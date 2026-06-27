@echo off
chcp 65001 >nul
echo ========================================
echo ContinuousDigging build
echo ========================================
echo.

cd /d "%~dp0"

if exist "src\Frontend\bin" rmdir /s /q "src\Frontend\bin"
if exist "src\Frontend\obj" rmdir /s /q "src\Frontend\obj"
if not exist "Plugins" mkdir "Plugins"
if exist "Plugins\ContinuousDigging.Backend.dll" del /q "Plugins\ContinuousDigging.Backend.dll"
if exist "Plugins\ContinuousDigging.Backend.pdb" del /q "Plugins\ContinuousDigging.Backend.pdb"

dotnet build src\Frontend\ContinuousDigging.Frontend.csproj -c Release
if errorlevel 1 (
    echo [FAILED] Frontend build failed
    exit /b 1
)

copy /Y "src\Frontend\bin\Release\netstandard2.1\ContinuousDigging.Frontend.dll" "Plugins\ContinuousDigging.Frontend.dll" >nul
copy /Y "src\Frontend\bin\Release\netstandard2.1\ContinuousDigging.Frontend.pdb" "Plugins\ContinuousDigging.Frontend.pdb" >nul

echo.
echo [OK] ContinuousDigging build completed
