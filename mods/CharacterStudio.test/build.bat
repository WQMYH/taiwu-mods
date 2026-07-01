@echo off
chcp 65001 >nul
cd /d "%~dp0"
if not exist "Plugins" mkdir "Plugins"
dotnet build "src\Backend\CharacterStudio.Backend.csproj" -c Release
if errorlevel 1 exit /b 1
dotnet build "src\Frontend\CharacterStudio.Frontend.csproj" -c Release
if errorlevel 1 exit /b 1
copy /Y "src\Backend\bin\Release\net8.0\CharacterStudio.Backend.dll" "Plugins\CharacterStudio.Backend.dll" >nul
copy /Y "src\Frontend\bin\Release\netstandard2.1\CharacterStudio.Frontend.dll" "Plugins\CharacterStudio.Frontend.dll" >nul
echo [OK] CharacterStudio.test build completed.
