@echo off
setlocal
cd /d "%~dp0"

echo =====================================================
echo OneBar Desktop v1.0 - C#/.NET Build and Package
echo =====================================================
echo.
echo This build produces a real OneBar.exe and installer.
echo If .NET 8 SDK or Inno Setup is missing, winget may ask
echo Windows for permission to install build dependencies.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Build-OneBar.ps1" -Bootstrap
if errorlevel 1 (
  echo.
  echo Build failed. See the error above.
  pause
  exit /b 1
)

echo.
echo Build complete.
pause
