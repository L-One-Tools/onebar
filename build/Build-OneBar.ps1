param(
    [switch]$Bootstrap,
    [switch]$InstallAfterBuild
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root 'src\OneBar\OneBar.csproj'
$Publish = Join-Path $Root 'artifacts\publish\win-x64'
$InstallerOut = Join-Path $Root 'artifacts\installer'
$Iss = Join-Path $Root 'installer\OneBar.iss'

function Refresh-Path {
    $machine = [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $user = [Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$machine;$user"
}

function Has-CompatibleSdk {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) { return $false }

    try {
        $sdks = & dotnet --list-sdks 2>$null
        foreach ($line in $sdks) {
            if ($line -match '^(\d+)\.') {
                if ([int]$Matches[1] -ge 8) { return $true }
            }
        }
    }
    catch {}

    return $false
}

function Ensure-DotNet {
    if (Has-CompatibleSdk) { return }

    if (-not $Bootstrap) {
        throw '.NET 8 SDK was not detected. Run BUILD_AND_PACKAGE.cmd again or install Microsoft .NET 8 SDK.'
    }

    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw 'winget was not detected. Install Microsoft .NET 8 SDK manually, then run the build again.'
    }

    Write-Host '[Bootstrap] Installing .NET 8 SDK...' -ForegroundColor Cyan
    & winget install --id Microsoft.DotNet.SDK.8 --exact --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        throw '.NET 8 SDK installation failed.'
    }

    Refresh-Path

    if (-not (Has-CompatibleSdk)) {
        throw '.NET SDK was installed, but this terminal still cannot detect it. Close this window and run BUILD_INSTALL_AND_RUN.cmd again.'
    }
}

function Find-InnoCompiler {
    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
    $programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)

    $known = @(
        (Join-Path $programFilesX86 'Inno Setup 6\ISCC.exe'),
        (Join-Path $programFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )

    foreach ($path in $known) {
        if ($path -and (Test-Path $path)) {
            return $path
        }
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    return $null
}

function Ensure-Inno {
    $compiler = Find-InnoCompiler
    if ($compiler) {
        return $compiler
    }

    if (-not $Bootstrap) {
        throw 'Inno Setup 6 was not detected. Install it, then run the build again.'
    }

    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw 'winget was not detected. Install Inno Setup 6 manually, then run the build again.'
    }

    Write-Host '[Bootstrap] Installing Inno Setup...' -ForegroundColor Cyan
    & winget install --id JRSoftware.InnoSetup --exact --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        throw 'Inno Setup installation failed.'
    }

    Refresh-Path

    $compiler = Find-InnoCompiler
    if (-not $compiler) {
        throw 'Inno Setup was installed, but ISCC.exe is still not visible. Close this window and run BUILD_INSTALL_AND_RUN.cmd again.'
    }

    return $compiler
}

Write-Host 'OneBar build root:' -ForegroundColor DarkGray
Write-Host "  $Root" -ForegroundColor DarkGray
Write-Host

Ensure-DotNet
$Inno = Ensure-Inno

Write-Host '[1/4] Cleaning artifacts...' -ForegroundColor Cyan
if (Test-Path $Publish) {
    Remove-Item $Publish -Recurse -Force
}
if (Test-Path $InstallerOut) {
    Remove-Item $InstallerOut -Recurse -Force
}
New-Item -ItemType Directory -Path $Publish -Force | Out-Null
New-Item -ItemType Directory -Path $InstallerOut -Force | Out-Null

Write-Host '[2/4] Publishing OneBar.exe...' -ForegroundColor Cyan
& dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $Publish

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

$exe = Join-Path $Publish 'OneBar.exe'
if (-not (Test-Path $exe)) {
    throw 'OneBar.exe was not generated.'
}

$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if ($versionInfo.ProductName -ne 'OneBar') {
    throw 'The generated EXE does not have the expected OneBar product identity.'
}

Write-Host "      Product: $($versionInfo.ProductName) $($versionInfo.ProductVersion)" -ForegroundColor DarkGray
Write-Host "      EXE:     $exe" -ForegroundColor DarkGray

Write-Host '[3/4] Building installer...' -ForegroundColor Cyan
& $Inno $Iss
if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup compile failed.'
}

$setup = Get-ChildItem $InstallerOut -Filter 'OneBar_Setup_*.exe' |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $setup) {
    throw 'The OneBar installer EXE was not generated.'
}

Write-Host '[4/4] Complete.' -ForegroundColor Green
Write-Host "OneBar.exe : $exe"
Write-Host "Installer  : $($setup.FullName)"

if ($InstallAfterBuild) {
    Write-Host '[Install] Starting OneBar installer...' -ForegroundColor Cyan
    Start-Process -FilePath $setup.FullName -Wait
}
else {
    Start-Process explorer.exe -ArgumentList ('"{0}"' -f $InstallerOut)
}
