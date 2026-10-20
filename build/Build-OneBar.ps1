param([string]$IsccPath)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw '.NET 8 SDK is required.' }
if (-not $IsccPath) {
    $candidates = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe")
    $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $IsccPath -or -not (Test-Path -LiteralPath $IsccPath)) { throw 'Inno Setup 6 is required; specify -IsccPath.' }
$publish = Join-Path $root 'artifacts\publish\win-x64'
New-Item -ItemType Directory -Path $publish -Force | Out-Null
# Use a fresh source checkout; do not mix stale files into the publish folder.
if (Get-ChildItem -LiteralPath $publish -Force | Select-Object -First 1) { throw 'Publish folder is not empty. Use a fresh source checkout/output folder.' }
& dotnet publish (Join-Path $root 'src\OneBar\OneBar.csproj') -c Release -r win-x64 --self-contained true -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
# Debug symbols are not needed by end users and contain build paths.
Get-ChildItem -LiteralPath $publish -Filter '*.pdb' -File | Remove-Item -Force
& $IsccPath (Join-Path $root 'installer\OneBar.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
Write-Output (Join-Path $root 'artifacts\installer\OneBar_Setup_v1.1.0-local.2.exe')
