@echo off
setlocal
cd /d "%~dp0"

echo =====================================================
echo OneBar Desktop v1.0 - Build, Install and Run
echo =====================================================
echo.
echo This converts the prototype into a real OneBar.exe,
echo creates the installer, then opens the installer.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Build-OneBar.ps1" -Bootstrap -InstallAfterBuild
if errorlevel 1 (
  echo.
  echo Build/install failed. See the error above.
  pause
  exit /b 1
)

echo.
echo Finished.
pause
