@echo off
chcp 65001 >nul
echo ========================================
echo FullDiscovery build
echo ========================================
echo.

pushd "%~dp0Plugins\FullDiscovery.Backend"
dotnet build -c Release
if errorlevel 1 (
    popd
    echo.
    echo [FAILED] Backend build failed
    exit /b 1
)
popd

copy /Y "%~dp0Plugins\FullDiscovery.Backend\bin\Release\netstandard2.1\FullDiscovery.Backend.dll" "%~dp0Plugins\FullDiscovery.Backend.dll" >nul
copy /Y "%~dp0Plugins\FullDiscovery.Backend\bin\Release\netstandard2.1\FullDiscovery.Backend.pdb" "%~dp0Plugins\FullDiscovery.Backend.pdb" >nul

pushd "%~dp0Plugins\FullDiscovery.Frontend"
dotnet build -c Release
if errorlevel 1 (
    popd
    echo.
    echo [FAILED] Frontend build failed
    exit /b 1
)
popd

copy /Y "%~dp0Plugins\FullDiscovery.Frontend\bin\Release\netstandard2.1\FullDiscovery.Frontend.dll" "%~dp0Plugins\FullDiscovery.Frontend.dll" >nul
copy /Y "%~dp0Plugins\FullDiscovery.Frontend\bin\Release\netstandard2.1\FullDiscovery.Frontend.pdb" "%~dp0Plugins\FullDiscovery.Frontend.pdb" >nul

echo.
echo [OK] FullDiscovery DLL build completed
