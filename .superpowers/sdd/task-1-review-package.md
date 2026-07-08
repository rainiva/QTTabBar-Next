# Task 1 Review Package

## Commit List
ba7bdd3 fix(register): tighten vm bootstrap and regasm discovery
8e0c49f fix(register): add vm-friendly environment bootstrap

## Diff Stat
 Register/Register.bat                  | 165 ++++++++++++++++++++++++++++-----
 Tests/RegisterScriptVmCompat.Tests.ps1 |  45 +++++++++
 2 files changed, 185 insertions(+), 25 deletions(-)

## Full Diff
```diff
diff --git a/Register/Register.bat b/Register/Register.bat
index 65cee7f..453fa3a 100644
--- a/Register/Register.bat
+++ b/Register/Register.bat
@@ -1,47 +1,162 @@
-echo off
+@echo off
+setlocal
 
-rem call VCVarsQueryRegistry.bat 32bit 64bit
-call "D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit
-cd ..\QTTabBar\bin\%1
+set "SCRIPT_DIR=%~dp0"
+set "REPO_ROOT=%SCRIPT_DIR%.."
+set "REGISTER_CONFIGURATION=%~1"
+if "%REGISTER_CONFIGURATION%"=="" set "REGISTER_CONFIGURATION=Release"
+
+set "REGISTER_ENV_SOURCE="
+set "REGISTER_GACUTIL="
+set "REGISTER_REGASM32="
+set "REGISTER_REGASM64="
+
+call :ensure_registration_environment
+if errorlevel 1 goto :fail_no_environment
+
+if /i "%QT_TABBAR_REGISTER_VALIDATE_ONLY%"=="1" (
+    echo Register environment ready via %REGISTER_ENV_SOURCE%
+    exit /b 0
+)
+
+cd /d "%REPO_ROOT%\QTTabBar\bin\%REGISTER_CONFIGURATION%"
 IF EXIST QTTabBar.dll (
-    gacutil /if QTTabBar.dll
-    call %FrameworkDir32%\%FrameworkVersion32%\regasm.exe QTTabBar.dll
-    if not "%FrameworkDir64%"=="" (
-        call %FrameworkDir64%\%FrameworkVersion64%\regasm.exe QTTabBar.dll
+    "%REGISTER_GACUTIL%" /if QTTabBar.dll
+    call "%REGISTER_REGASM32%" QTTabBar.dll
+    if defined REGISTER_REGASM64 (
+        call "%REGISTER_REGASM64%" QTTabBar.dll
     )
 )
-cd ..\..\
 
-cd ..\QTPluginLib\bin\%1
+cd /d "%REPO_ROOT%\QTPluginLib\bin\%REGISTER_CONFIGURATION%"
 IF EXIST QTPluginLib.dll (
-    gacutil /if QTPluginLib.dll
+    "%REGISTER_GACUTIL%" /if QTPluginLib.dll
 )
 
-cd ..\..\..\BandObjectLib\bin\%1
+cd /d "%REPO_ROOT%\BandObjectLib\bin\%REGISTER_CONFIGURATION%"
 IF EXIST BandObjectLib.dll (
-    gacutil /if BandObjectLib.dll
+    "%REGISTER_GACUTIL%" /if BandObjectLib.dll
 )
 IF EXIST Interop.SHDocVw.dll (
-    gacutil /if Interop.SHDocVw.dll
+    "%REGISTER_GACUTIL%" /if Interop.SHDocVw.dll
 )
 
-cd ..\..\..\QTHookLib\bin\%1
+cd /d "%REPO_ROOT%\QTHookLib\bin\%REGISTER_CONFIGURATION%"
 REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:32
 REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:64
 
-cd ..\..\..\Register
+cd /d "%SCRIPT_DIR%"
+start taskmgr
+exit /b 0
 
-rem taskkill /f /im explorer.exe
-rem start explorer.exe
+:ensure_registration_environment
+call :has_usable_environment
+if not errorlevel 1 (
+    set "REGISTER_ENV_SOURCE=current-shell"
+    exit /b 0
+)
 
-rem timeout /nobreak /t 5
+call :try_modern_vs
+if not errorlevel 1 exit /b 0
 
-rem cmd.exe /c start taskmgr
-start taskmgr
-exit 0
-rem start cmd.exe
+call :try_vs2010
+if not errorlevel 1 exit /b 0
+
+exit /b 1
+
+:has_usable_environment
+set "REGISTER_GACUTIL="
+for /f "delims=" %%I in ('where gacutil.exe 2^>nul') do if not defined REGISTER_GACUTIL set "REGISTER_GACUTIL=%%~fI"
+if not defined REGISTER_GACUTIL exit /b 1
+
+set "REGISTER_REGASM32="
+if defined FrameworkDir32 if defined FrameworkVersion32 if exist "%FrameworkDir32%%FrameworkVersion32%\regasm.exe" (
+    set "REGISTER_REGASM32=%FrameworkDir32%%FrameworkVersion32%\regasm.exe"
+)
+if not defined REGISTER_REGASM32 for /f "delims=" %%I in ('where /R "%SystemRoot%\Microsoft.NET\Framework" regasm.exe 2^>nul') do if not defined REGISTER_REGASM32 set "REGISTER_REGASM32=%%~fI"
+if not defined REGISTER_REGASM32 exit /b 1
+
+set "REGISTER_REGASM64="
+if defined FrameworkDir64 if defined FrameworkVersion64 if exist "%FrameworkDir64%%FrameworkVersion64%\regasm.exe" (
+    set "REGISTER_REGASM64=%FrameworkDir64%%FrameworkVersion64%\regasm.exe"
+)
+
+exit /b 0
+
+:try_modern_vs
+set "VSWHERE_EXE="
+set "VS_INSTALL="
+
+if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" set "VSWHERE_EXE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
+if not defined VSWHERE_EXE if exist "%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe" set "VSWHERE_EXE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
+if not defined VSWHERE_EXE exit /b 1
+
+for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do if not defined VS_INSTALL set "VS_INSTALL=%%~I"
+if not defined VS_INSTALL (
+    for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -property installationPath`) do if not defined VS_INSTALL set "VS_INSTALL=%%~I"
+)
+if not defined VS_INSTALL exit /b 1
 
-rem cmd cmd /k
+if exist "%VS_INSTALL%\Common7\Tools\VsDevCmd.bat" (
+    call "%VS_INSTALL%\Common7\Tools\VsDevCmd.bat" -arch=x86 >nul 2>nul
+    call :has_usable_environment
+    if not errorlevel 1 (
+        set "REGISTER_ENV_SOURCE=vswhere-vsdevcmd"
+        exit /b 0
+    )
+)
+
+if exist "%VS_INSTALL%\VC\Auxiliary\Build\vcvarsall.bat" (
+    call "%VS_INSTALL%\VC\Auxiliary\Build\vcvarsall.bat" x86 >nul 2>nul
+    call :has_usable_environment
+    if not errorlevel 1 (
+        set "REGISTER_ENV_SOURCE=vswhere-vcvarsall"
+        exit /b 0
+    )
+)
+
+exit /b 1
+
+:try_vs2010
+if defined VS100COMNTOOLS if exist "%VS100COMNTOOLS%VCVarsQueryRegistry.bat" (
+    call "%VS100COMNTOOLS%VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
+    call :has_usable_environment
+    if not errorlevel 1 (
+        set "REGISTER_ENV_SOURCE=vs2010-comntools"
+        exit /b 0
+    )
+)
+
+if exist "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" (
+    call "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
+    call :has_usable_environment
+    if not errorlevel 1 (
+        set "REGISTER_ENV_SOURCE=vs2010-programfilesx86"
+        exit /b 0
+    )
+)
+
+if exist "%ProgramFiles%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" (
+    call "%ProgramFiles%\Microsoft Visual Studio 10.0\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
+    call :has_usable_environment
+    if not errorlevel 1 (
+        set "REGISTER_ENV_SOURCE=vs2010-programfiles"
+        exit /b 0
+    )
+)
+
+if exist "D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat" (
+    call "D:\Visual Studio 2010 Ultimate\Common7\Tools\VCVarsQueryRegistry.bat" 32bit 64bit >nul 2>nul
+    call :has_usable_environment
+    if not errorlevel 1 (
+        set "REGISTER_ENV_SOURCE=vs2010-legacy-fixed-path"
+        exit /b 0
+    )
+)
 
-rem cmd /c start explorer.exe
+exit /b 1
 
+:fail_no_environment
+echo Register environment bootstrap failed.
+echo Install Visual Studio Build Tools with C++ tools or rerun from a Developer Command Prompt.
+exit /b 1
diff --git a/Tests/RegisterScriptVmCompat.Tests.ps1 b/Tests/RegisterScriptVmCompat.Tests.ps1
new file mode 100644
index 0000000..5298704
--- /dev/null
+++ b/Tests/RegisterScriptVmCompat.Tests.ps1
@@ -0,0 +1,45 @@
+Set-StrictMode -Version Latest
+$ErrorActionPreference = 'Stop'
+
+$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
+$registerScriptPath = Join-Path $repoRoot 'Register\Register.bat'
+$registerScript = [System.IO.File]::ReadAllText($registerScriptPath)
+
+function Assert-Contains([string]$Haystack, [string]$Needle, [string]$Message) {
+    if (-not $Haystack.Contains($Needle)) {
+        throw "$Message`nMissing: $Needle"
+    }
+}
+
+function Assert-NotContains([string]$Haystack, [string]$Needle, [string]$Message) {
+    if ($Haystack.Contains($Needle)) {
+        throw "$Message`nUnexpected: $Needle"
+    }
+}
+
+$requiredMarkers = @(
+    'set "SCRIPT_DIR=%~dp0"',
+    ':ensure_registration_environment',
+    ':has_usable_environment',
+    ':try_modern_vs',
+    ':try_vs2010',
+    ':fail_no_environment',
+    'QT_TABBAR_REGISTER_VALIDATE_ONLY',
+    'vswhere.exe',
+    'for /f "usebackq delims=" %%I in (`"%VSWHERE_EXE%" -latest -products * -property installationPath`) do if not defined VS_INSTALL',
+    'VS100COMNTOOLS',
+    'Register environment bootstrap failed.',
+    'Install Visual Studio Build Tools',
+    'REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:32',
+    'REG ADD HKLM\SOFTWARE\QTTabBar /v InstallPath /t REG_SZ /d "%cd%" /f /reg:64',
+    'start taskmgr',
+    'where /R "%SystemRoot%\Microsoft.NET\Framework" regasm.exe'
+)
+
+foreach ($marker in $requiredMarkers) {
+    Assert-Contains $registerScript $marker "Register\Register.bat must contain the VM-compat marker."
+}
+
+Assert-NotContains $registerScript 'where regasm.exe 2^>nul' 'Generic regasm.exe lookup must not remain, because it can pick the wrong architecture.'
+
+Write-Host 'RegisterScriptVmCompat static checks passed.'
```
