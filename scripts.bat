@echo off
setlocal
cd /d "%~dp0"

set MSBUILD="C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

:MENU
cls
echo ===================================
echo SnakeBiteGZ Release Builder
echo ===================================
echo 1. Build Debug
echo 2. Build Release
echo 3. Build Release Package (Recommended)
echo 4. Run Tests
echo 5. Bump Version
echo 6. Exit
echo ===================================
set /p choice="Select an option (1-6): "

if "%choice%"=="1" goto BUILD_DEBUG
if "%choice%"=="2" goto BUILD_RELEASE
if "%choice%"=="3" goto BUILD_RELEASE_PACKAGE
if "%choice%"=="4" goto RUN_TESTS
if "%choice%"=="5" goto BUMP_VERSION
if "%choice%"=="6" goto EOF

echo Invalid choice.
pause
goto MENU

:BUILD_DEBUG
cls
echo Building Debug...
%MSBUILD% "SnakeBite.sln" /t:Rebuild /p:Configuration=Debug /v:minimal
pause
goto MENU

:BUILD_RELEASE
cls
echo Building Release...
%MSBUILD% "SnakeBite.sln" /t:Rebuild /p:Configuration=Release /v:minimal
pause
goto MENU

:BUILD_RELEASE_PACKAGE
cls
echo === Building Release Package ===

:: Build first
%MSBUILD% "SnakeBite.sln" /t:Rebuild /p:Configuration=Release /v:minimal
if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    goto MENU
)

:: Get version from AssemblyInfo
for /f "tokens=2 delims=()" %%a in ('findstr "AssemblyVersion" "SnakeBite\Properties\AssemblyInfo.cs"') do (
    set VERSION=%%a
)
set VERSION=%VERSION:"=%
echo Detected version: %VERSION%

:: Create release folder
set RELEASE_FOLDER=SnakeBiteGZ_v%VERSION%
set ZIP_NAME=SnakeBiteGZv%VERSION%.zip

if exist "dist\%RELEASE_FOLDER%" rmdir /s /q "dist\%RELEASE_FOLDER%"
if exist "dist\%ZIP_NAME%" del "dist\%ZIP_NAME%"

mkdir "dist\%RELEASE_FOLDER%"

echo.
echo Copying files to dist\%RELEASE_FOLDER%...

xcopy /y "SnakeBite\bin\Release\SnakeBiteGZ.exe"          "dist\%RELEASE_FOLDER%\"
xcopy /y "SnakeBite\bin\Release\SnakeBiteGZ.exe.config"  "dist\%RELEASE_FOLDER%\"
xcopy /y "makebite\bin\Release\MakeBiteGZ.exe"           "dist\%RELEASE_FOLDER%\"
xcopy /y "makebite\bin\Release\MakeBiteGZ.exe.config"    "dist\%RELEASE_FOLDER%\"

xcopy /y "SnakeBite\bin\Release\*.dll"                    "dist\%RELEASE_FOLDER%\" 2>nul
xcopy /y "SnakeBite\bin\Release\*.txt"                    "dist\%RELEASE_FOLDER%\" 2>nul
xcopy /y "SnakeBite\bin\Release\GzsTool.exe"             "dist\%RELEASE_FOLDER%\" 2>nul
xcopy /y "SnakeBite\bin\Release\GzsTool.exe.config"      "dist\%RELEASE_FOLDER%\" 2>nul

copy "README.md"                                          "dist\%RELEASE_FOLDER%\"
copy "ChangeLog.txt"                                      "dist\%RELEASE_FOLDER%\" 2>nul

echo.
echo Creating zip: %ZIP_NAME%
powershell -NoProfile -Command "Compress-Archive -Path 'dist\%RELEASE_FOLDER%' -DestinationPath 'dist\%ZIP_NAME%' -Force"

echo.
echo Done! Release package created:
echo    dist\%ZIP_NAME%
echo.
echo Folder also available at: dist\%RELEASE_FOLDER%
pause
goto MENU

:RUN_TESTS
cls
echo Running tests...
%MSBUILD% "SnakeBite.sln" /t:SnakeBite_Tests /p:Configuration=Debug
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    goto MENU
)
"SnakeBite.Tests\bin\Debug\TestRunner.exe"
pause
goto MENU

:BUMP_VERSION
cls
echo Bumping Version...
powershell -NoProfile -ExecutionPolicy Bypass -File "bump-version.ps1"
echo.
pause
goto MENU

:EOF
endlocal