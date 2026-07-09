@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "REGISTER_CONFIGURATION=%~1"
if "%REGISTER_CONFIGURATION%"=="" set "REGISTER_CONFIGURATION=Release"

set "REGISTER_ENV_SOURCE="
set "REGISTER_GACUTIL="
set "REGISTER_REGASM32="
set "REGISTER_REGASM64="

call :ensure_registration_environment
if errorlevel 1 goto :fail_no_environment

if /i "%QT_TABBAR_REGISTER_VALIDATE_ONLY%"=="1" (
    echo Register environment ready via %REGISTER_ENV_SOURCE%
    exit /b 0
)

cd /d "%REPO_ROOT%\QTTabBar\bin\%REGISTER_CONFIGURATION%"
IF EXIST QTTabBar.dll (
    "%REGISTER_GACUTIL%" /if QTTabBar.dll
    call "%REGISTER_REGASM32%" QTTabBar.dll
    if defined REGISTER_REGASM64 (
        call "%REGISTER_REGASM64%" QTTabBar.dll
    )
)

cd /d "%REPO_ROOT%\QTPluginLib\bin\%REGISTER_CONFIGURATION%"
IF EXIST QTPluginLib.dll (
    "%REGISTER_GACUTIL%" /if QTPluginLib.dll
)

cd /d "%REPO_ROOT%\BandObjectLib\bin\%REGISTER_CONFIGURATION%"
IF EXIST BandObjectLib.dll (
    "%REGISTER_GACUTIL%" /if BandObjectLib.dll
)

cd /d "%REPO_ROOT%\QTHookLib\bin\%REGISTER_CONFIGURATION%"
REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:32
REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:64

cd /d "%SCRIPT_DIR%"
start taskmgr
exit /b 0

:ensure_registration_environment
if /i "%QT_TABBAR_REGISTER_FORCE_NO_ENV%"=="1" exit /b 1

call :has_usable_environment
if not errorlevel 1 (
    set "REGISTER_ENV_SOURCE=current-shell"
    exit /b 0
)

call :try_modern_vs
if not errorlevel 1 exit /b 0

call :try_vs2010
if not errorlevel 1 exit /b 0

exit /b 1

:has_usable_environment
set "REGISTER_GACUTIL="
for /f "delims=" %%I in ('where gacutil.exe 2^>nul') do if not defined REGISTER_GACUTIL set "REGISTER_GACUTIL=%%~fI"
if not defined REGISTER_GACUTIL exit /b 1

set "REGISTER_REGASM32="
if defined FrameworkDir32 if defined FrameworkVersion32 if exist "%FrameworkDir32%%FrameworkVersion32%\regasm.exe" (
    set "REGISTER_REGASM32=%FrameworkDir32%%FrameworkVersion32%\regasm.exe"
)
if not defined REGISTER_REGASM32 for /f "delims=" %%I in ('where /R "%SystemRoot%\Microsoft.NET\Framework" regasm.exe 2^>nul') do if not defined REGISTER_REGASM32 set "REGISTER_REGASM32=%%~fI"
if not defined REGISTER_REGASM32 exit /b 1

set "REGISTER_REGASM64="
if defined FrameworkDir64 if defined FrameworkVersion64 if exist "%FrameworkDir64%%FrameworkVersion64%\regasm.exe" (
    set "REGISTER_REGASM64=%FrameworkDir64%%FrameworkVersion64%\regasm.exe"
)

exit /b 0

:try_modern_vs
set "VSWHERE_EXE="
set "VS_INSTALL="

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" set "VSWHERE_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not defined VSWHERE_EXE if exist "%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe" set "VSWHERE_EXE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if not defined VSWHERE_EXE exit /b 1

for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do if not defined VS_INSTALL set "VS_INSTALL=%%~I"
if not defined VS_INSTALL (
    for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -property installationPath`) do if not defined VS_INSTALL set "VS_INSTALL=%%~I"
)
if not defined VS_INSTALL exit /b 1

if exist "%VS_INSTALL%\Common7\Tools\VsDevCmd.bat" (
    call "%VS_INSTALL%\Common7\Tools\VsDevCmd.bat" -arch=x86 >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vswhere-vsdevcmd"
        exit /b 0
    )
)

if exist "%VS_INSTALL%\VC\Auxiliary\Build\vcvarsall.bat" (
    call "%VS_INSTALL%\VC\Auxiliary\Build\vcvarsall.bat" x86 >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vswhere-vcvarsall"
        exit /b 0
    )
)

exit /b 1

:try_vs2010
if defined VS100COMNTOOLS if exist "%VS100COMNTOOLS%VCVarsQueryRegistry.bat" (
    call "%VS100COMNTOOLS%VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-comntools"
        exit /b 0
    )
)

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" (
    call "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-programfilesx86"
        exit /b 0
    )
)

if exist "%ProgramFiles%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" (
    call "%ProgramFiles%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-programfiles"
        exit /b 0
    )
)

if exist "D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat" (
    call "D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
    call :has_usable_environment
    if not errorlevel 1 (
        set "REGISTER_ENV_SOURCE=vs2010-legacy-fixed-path"
        exit /b 0
    )
)

exit /b 1

:fail_no_environment
echo Register environment bootstrap failed.
echo Install Visual Studio Build Tools with C++ tools or rerun from a Developer Command Prompt.
exit /b 1
