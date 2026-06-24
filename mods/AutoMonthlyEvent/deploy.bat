@echo off
echo ========================================
echo AutoMonthlyEvent MOD 部署脚本
echo ========================================
echo.

REM 设置游戏路径。可通过环境变量 TAIWU_GAME_DIR 覆盖。
if "%TAIWU_GAME_DIR%"=="" (
    set "GAME_DIR=A:\SteamLibrary\steamapps\common\The Scroll Of Taiwu"
) else (
    set "GAME_DIR=%TAIWU_GAME_DIR%"
)
set MODS_DIR=%GAME_DIR%\Mod\AutoMonthlyEvent

echo 检查游戏目录...
if not exist "%GAME_DIR%" (
    echo [错误] 游戏目录不存在: %GAME_DIR%
    echo 请修改deploy.bat中的GAME_DIR变量
    pause
    exit /b 1
)

echo 创建MOD目录...
if not exist "%MODS_DIR%" mkdir "%MODS_DIR%"
if not exist "%MODS_DIR%\Plugins" mkdir "%MODS_DIR%\Plugins"

echo.
echo 复制配置文件...
copy /Y "Config.lua" "%MODS_DIR%\Config.lua" >nul
if %errorlevel% equ 0 (
    echo [成功] Config.lua
) else (
    echo [失败] Config.lua
)

echo.
echo 复制后端插件...
if exist "Plugins\AutoMonthlyEvent.Backend\bin\Release\netstandard2.1\AutoMonthlyEvent.Backend.dll" (
    copy /Y "Plugins\AutoMonthlyEvent.Backend\bin\Release\netstandard2.1\AutoMonthlyEvent.Backend.dll" "%MODS_DIR%\Plugins\" >nul
    if %errorlevel% equ 0 (
        echo [成功] AutoMonthlyEvent.Backend.dll
    ) else (
        echo [失败] AutoMonthlyEvent.Backend.dll
    )
) else (
    echo [警告] 未找到编译后的Backend DLL，请先执行编译
)

echo.
echo 复制前端插件...
if exist "Plugins\AutoMonthlyEvent.Frontend\bin\Release\netstandard2.1\AutoMonthlyEvent.Frontend.dll" (
    copy /Y "Plugins\AutoMonthlyEvent.Frontend\bin\Release\netstandard2.1\AutoMonthlyEvent.Frontend.dll" "%MODS_DIR%\Plugins\" >nul
    if %errorlevel% equ 0 (
        echo [成功] AutoMonthlyEvent.Frontend.dll
    ) else (
        echo [失败] AutoMonthlyEvent.Frontend.dll
    )
) else (
    echo [警告] 未找到编译后的Frontend DLL，请先执行编译
)

echo.
echo ========================================
echo 部署完成！
echo MOD目录: %MODS_DIR%
echo ========================================
echo.
pause
