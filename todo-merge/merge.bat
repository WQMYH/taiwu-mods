@echo off
chcp 65001 >nul
echo ========================================
echo   Tianji Creations Mod Merge Script
echo ========================================
echo.

set BASE_DIR=%~dp0
set OUTPUT_DIR=%BASE_DIR%merged\Tianji-Creations

echo [1/7] Creating output directories...
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%OUTPUT_DIR%\Plugins" mkdir "%OUTPUT_DIR%\Plugins"
if not exist "%OUTPUT_DIR%\TianDao" mkdir "%OUTPUT_DIR%\TianDao"
if not exist "%OUTPUT_DIR%\TianDao\themes" mkdir "%OUTPUT_DIR%\TianDao\themes"
echo [OK] Directories created
echo.

echo [2/7] Copying shared SDK and framework DLLs...
copy "%BASE_DIR%3737804824\Plugins\TianDao.Abstractions.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3737804824\Plugins\TianDao.Backend.Sdk.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3737804824\Plugins\TianDao.Frontend.Sdk.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3737804824\Plugins\MoFaBackend.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3737804824\Plugins\MoFaFrontEnd.dll" "%OUTPUT_DIR%\Plugins\" >nul
echo [OK] SDK and framework DLLs copied
echo.

echo [3/7] Copying feature module DLLs...
:: XiaoYao (逍遥游)
copy "%BASE_DIR%3746015837\Plugins\XiaoYao.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3746015837\Plugins\XiaoYao.Frontend.dll" "%OUTPUT_DIR%\Plugins\" >nul

:: KanPo (洞玄)
copy "%BASE_DIR%3746021415\Plugins\KanPoFrontEnd.dll" "%OUTPUT_DIR%\Plugins\" >nul

:: MakeMaster (斡旋造化)
copy "%BASE_DIR%3746022765\Plugins\MakeMaster.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3746022765\Plugins\MakeMaster.Frontend.dll" "%OUTPUT_DIR%\Plugins\" >nul

:: BreakMaster (无忧突破)
copy "%BASE_DIR%3746024435\Plugins\BreakMaster.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3746024435\Plugins\BreakMaster.Frontend.dll" "%OUTPUT_DIR%\Plugins\" >nul

:: QuantumMaster (气运之子)
copy "%BASE_DIR%3746071342\Plugins\QuantumMaster.dll" "%OUTPUT_DIR%\Plugins\" >nul
copy "%BASE_DIR%3746071342\Plugins\QuantumMaster.Frontend.dll" "%OUTPUT_DIR%\Plugins\" >nul
echo [OK] Feature module DLLs copied
echo.

echo [4/7] Copying cover image...
copy "%BASE_DIR%3737804824\幻梦蝶.png" "%OUTPUT_DIR%\" >nul
echo [OK] Cover image copied
echo.

echo [5/7] Copying MoFa (幻梦蝶) TianDao config...
copy "%BASE_DIR%3737804824\TianDao\schema.json" "%OUTPUT_DIR%\TianDao\" >nul
copy "%BASE_DIR%3737804824\TianDao\values.json" "%OUTPUT_DIR%\TianDao\" >nul
copy "%BASE_DIR%3737804824\TianDao\CN.json" "%OUTPUT_DIR%\TianDao\" >nul
copy "%BASE_DIR%3737804824\TianDao\CNH.json" "%OUTPUT_DIR%\TianDao\" >nul
copy "%BASE_DIR%3737804824\TianDao\EN.json" "%OUTPUT_DIR%\TianDao\" >nul
copy "%BASE_DIR%3737804824\TianDao\KO.json" "%OUTPUT_DIR%\TianDao\" >nul
xcopy "%BASE_DIR%3737804824\TianDao\themes" "%OUTPUT_DIR%\TianDao\themes\" /E /I /Y >nul
echo [OK] MoFa config copied
echo.

echo [6/7] Copying other mods' TianDao configs (subdirectories)...
:: Create subdirectories and copy configs
mkdir "%OUTPUT_DIR%\TianDao\XiaoYao" >nul 2>&1
copy "%BASE_DIR%3746015837\TianDao\XiaoYao\schema.json" "%OUTPUT_DIR%\TianDao\XiaoYao\" >nul
copy "%BASE_DIR%3746015837\TianDao\XiaoYao\values.json" "%OUTPUT_DIR%\TianDao\XiaoYao\" >nul
copy "%BASE_DIR%3746015837\TianDao\XiaoYao\CN.json" "%OUTPUT_DIR%\TianDao\XiaoYao\" >nul
copy "%BASE_DIR%3746015837\TianDao\XiaoYao\CNH.json" "%OUTPUT_DIR%\TianDao\XiaoYao\" >nul
copy "%BASE_DIR%3746015837\TianDao\XiaoYao\EN.json" "%OUTPUT_DIR%\TianDao\XiaoYao\" >nul
copy "%BASE_DIR%3746015837\TianDao\XiaoYao\KO.json" "%OUTPUT_DIR%\TianDao\XiaoYao\" >nul

mkdir "%OUTPUT_DIR%\TianDao\MakeMaster" >nul 2>&1
copy "%BASE_DIR%3746022765\TianDao\MakeMaster\schema.json" "%OUTPUT_DIR%\TianDao\MakeMaster\" >nul
copy "%BASE_DIR%3746022765\TianDao\MakeMaster\values.json" "%OUTPUT_DIR%\TianDao\MakeMaster\" >nul
copy "%BASE_DIR%3746022765\TianDao\MakeMaster\CN.json" "%OUTPUT_DIR%\TianDao\MakeMaster\" >nul
copy "%BASE_DIR%3746022765\TianDao\MakeMaster\CNH.json" "%OUTPUT_DIR%\TianDao\MakeMaster\" >nul
copy "%BASE_DIR%3746022765\TianDao\MakeMaster\EN.json" "%OUTPUT_DIR%\TianDao\MakeMaster\" >nul
copy "%BASE_DIR%3746022765\TianDao\MakeMaster\KO.json" "%OUTPUT_DIR%\TianDao\MakeMaster\" >nul

mkdir "%OUTPUT_DIR%\TianDao\BreakMaster" >nul 2>&1
xcopy "%BASE_DIR%3746024435\TianDao\BreakMaster" "%OUTPUT_DIR%\TianDao\BreakMaster\" /E /I /Y >nul

mkdir "%OUTPUT_DIR%\TianDao\CombatMaster" >nul 2>&1
xcopy "%BASE_DIR%3746071342\TianDao\CombatMaster" "%OUTPUT_DIR%\TianDao\CombatMaster\" /E /I /Y >nul

mkdir "%OUTPUT_DIR%\TianDao\QuantumMaster" >nul 2>&1
xcopy "%BASE_DIR%3746071342\TianDao\QuantumMaster" "%OUTPUT_DIR%\TianDao\QuantumMaster\" /E /I /Y >nul

:: KanPo config is in root directory, needs special handling
mkdir "%OUTPUT_DIR%\TianDao\KanPo" >nul 2>&1
copy "%BASE_DIR%3746021415\CN.json" "%OUTPUT_DIR%\TianDao\KanPo\CN.json" >nul
copy "%BASE_DIR%3746021415\CNH.json" "%OUTPUT_DIR%\TianDao\KanPo\CNH.json" >nul
copy "%BASE_DIR%3746021415\EN.json" "%OUTPUT_DIR%\TianDao\KanPo\EN.json" >nul
copy "%BASE_DIR%3746021415\KO.json" "%OUTPUT_DIR%\TianDao\KanPo\KO.json" >nul
copy "%BASE_DIR%3746021415\schema.json" "%OUTPUT_DIR%\TianDao\KanPo\schema.json" >nul
echo [OK] Other mods' configs copied
echo.

echo [7/7] Generating unified Config.lua...
(
echo return {
echo     Title = "Tianji Creations",
echo     Source = 1,
echo     Version = "1.0.0",
echo     Author = "ErSan",
echo     Description = "【 Tianji Creations 】\n\nUnified mod package combining all Tianji series features.\n\n★ ———— ★\n\n[Usage]\n\nPress ^<F11^> in-game to open the MoFa interface.\nAll feature configurations are available in the interface.\n\n★ ———— ★\n\nIncluded Features:\n - MoFa (幻梦蝶) - UI Framework and Config Hosting\n - XiaoYao (逍遥游) - Game Start/Map/Village Features\n - KanPo (洞玄) - Charm View/Insight Features\n - MakeMaster (斡旋造化) - Crafting Features\n - BreakMaster (无忧突破) - Breakthrough Features\n - QuantumMaster (气运之子) - Probability Control Features\n\n★ ———— ★\n\nThank you for your support!",
echo     FrontendPlugins = {
echo         [1] = "MoFaFrontEnd.dll",
echo         [2] = "XiaoYao.Frontend.dll",
echo         [3] = "KanPoFrontEnd.dll",
echo         [4] = "MakeMaster.Frontend.dll",
echo         [5] = "BreakMaster.Frontend.dll",
echo         [6] = "QuantumMaster.Frontend.dll",
echo     },
echo     BackendPlugins = {
echo         [1] = "MoFaBackend.dll",
echo         [2] = "XiaoYao.dll",
echo         [3] = "MakeMaster.dll",
echo         [4] = "BreakMaster.dll",
echo         [5] = "QuantumMaster.dll",
echo     },
echo     GameVersion = "1.0.10-test-gm",
echo     FileId = 0,
echo     DefaultSettings = { },
echo     ChangeConfig = false,
echo     HasArchive = false,
echo     NeedRestartWhenSettingChanged = false,
echo     Cover = "幻梦蝶.png",
echo     WorkshopCover = "幻梦蝶.png",
echo     TagList = {
echo         [1] = "Arts",
echo         [2] = "Frameworks",
echo         [3] = "Modifications",
echo         [4] = "Compatible Mods",
echo     },
echo     Visibility = 0,
echo }
) > "%OUTPUT_DIR%\Config.lua"
echo [OK] Config.lua generated
echo.

echo ========================================
echo   Merge Complete!
echo ========================================
echo.
echo Output Directory: %OUTPUT_DIR%
echo.
echo Next Steps:
echo 1. Check Tianji-Creations directory structure
echo 2. May need to manually merge schema.json files
echo 3. Deploy to game mod directory for testing
echo.
pause
