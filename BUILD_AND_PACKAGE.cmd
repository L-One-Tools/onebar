@echo off
setlocal
cd /d "%~dp0"

echo =====================================================
echo OneBar Desktop 1.1.0-local.2 - C#/.NET Build and Package
echo =====================================================
echo.
echo This build produces a real OneBar.exe and installer.
echo Install .NET 8 SDK and Inno Setup 6 before building.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Build-OneBar.ps1"
if errorlevel 1 (
  echo.
  echo Build failed. See the error above.
  pause
  exit /b 1
)

echo.
echo Build complete.
pause
