@echo off
setlocal
cd /d "%~dp0"

set MSBUILD="C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

:MENU
cls
echo ===================================
echo SnakeBiteGZ Task Runner
echo ===================================
echo 1. Build Debug
echo 2. Build Release
echo 3. Run Tests
echo 4. Bump Minor Version
echo 5. Exit
echo ===================================
set /p choice="Select an option (1-5): "

if "%choice%"=="1" goto BUILD_DEBUG
if "%choice%"=="2" goto BUILD_RELEASE
if "%choice%"=="3" goto RUN_TESTS
if "%choice%"=="4" goto BUMP_MINOR_VERSION
if "%choice%"=="5" goto EOF

echo Invalid choice.
pause
goto MENU

:BUILD_DEBUG
cls
echo Building Debug Configuration...
%MSBUILD% "SnakeBite.sln" /t:Rebuild /p:Configuration=Debug
echo.
pause
goto MENU

:BUILD_RELEASE
cls
echo Building Release Configuration...
%MSBUILD% "SnakeBite.sln" /t:Rebuild /p:Configuration=Release
echo.
pause
goto MENU

:RUN_TESTS
cls
echo Building Test Project and Running Tests...
%MSBUILD% "SnakeBite.sln" /t:SnakeBite_Tests /p:Configuration=Debug
if %ERRORLEVEL% neq 0 (
    echo.
    echo Build failed! Tests will not be run.
    pause
    goto MENU
)
echo.
echo Executing TestRunner...
"SnakeBite.Tests\bin\Debug\TestRunner.exe"
echo.
pause
goto MENU

:BUMP_MINOR_VERSION
cls
echo Bumping Minor Version...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$files = @('.\SnakeBite\Properties\AssemblyInfo.cs', '.\makebite\Properties\AssemblyInfo.cs'); $content = Get-Content $files[0] -Raw; $m = [regex]::Match($content, 'AssemblyVersion\(\""(.*?)\""\)'); if (!$m.Success) { Write-Host 'Error: Could not find version in ' $files[0]; pause; exit }; $version = $m.Groups[1].Value; Write-Host 'Current Version:' $version; Write-Host ''; $newVer = Read-Host 'Enter new version string (e.g. 0.1.0.0)'; if ([string]::IsNullOrWhiteSpace($newVer)) { Write-Host 'Aborted.'; exit }; foreach ($f in $files) { (Get-Content $f -Raw -Encoding UTF8) -replace 'AssemblyVersion\(\"".*?\""\)', ('AssemblyVersion(\""' + $newVer + '\"")') -replace 'AssemblyFileVersion\(\"".*?\""\)', ('AssemblyFileVersion(\""' + $newVer + '\"")') | Set-Content $f -Encoding UTF8 }; Write-Host ''; Write-Host 'Version successfully bumped to' $newVer;"
echo.
pause
goto MENU

:EOF
endlocal
