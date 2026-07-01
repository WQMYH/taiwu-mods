@echo off
setlocal
set "ROOT=%~dp0"
if defined TAIWU_GAME_DIR (
  set "GAME=%TAIWU_GAME_DIR%"
) else (
  set "GAME=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu"
)
set "TARGET=%GAME%\Mod\GongfaFramework.test"
if not exist "%TARGET%" mkdir "%TARGET%"
robocopy "%ROOT%" "%TARGET%" Config.lua README.md CHANGELOG.md /R:1 /W:1
for %%D in (Plugins Schemas Definitions docs) do robocopy "%ROOT%%%D" "%TARGET%\%%D" /E /R:1 /W:1
if errorlevel 8 exit /b 1
echo Deployed to %TARGET%
