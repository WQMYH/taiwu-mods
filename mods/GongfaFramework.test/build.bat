@echo off
setlocal
set "ROOT=%~dp0"
dotnet build "%ROOT%Plugins\GongfaFramework.test.Frontend\GongfaFramework.test.Frontend.csproj" -c Release -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
if errorlevel 1 exit /b 1
dotnet build "%ROOT%Plugins\GongfaFramework.test.Backend\GongfaFramework.test.Backend.csproj" -c Release -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false
if errorlevel 1 exit /b 1
echo GongfaFramework.test 0.1.0.0 build completed.
