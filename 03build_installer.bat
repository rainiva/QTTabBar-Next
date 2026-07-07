@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Tools\BuildInstaller.ps1" %*
exit /b %ERRORLEVEL%
