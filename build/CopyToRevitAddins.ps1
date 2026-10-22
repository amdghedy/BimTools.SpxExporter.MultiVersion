<#
.SYNOPSIS
    Copies the compiled BimTools.SpxExporter.RevitYYYY add-in into the local
    user's Revit Addins folder so it will be loaded on next Revit start.

.DESCRIPTION
    Bundled inside every per-year release zip so end users have a one-shot
    installer that requires zero configuration.

    Run from the unzipped folder:

        powershell -ExecutionPolicy Bypass -File .\CopyToRevitAddins.ps1

    The script detects the current Windows user's %APPDATA% path, resolves the
    matching Revit Addins folder (e.g. %APPDATA%\Autodesk\Revit\Addins\YYYY),
    and copies the DLL + .addin manifest there.

    Two install modes are supported:
      - PerUser (default):  %APPDATA%\Autodesk\Revit\Addins\YYYY\
      - PerMachine (-Machine): %PROGRAMDATA%\Autodesk\Revit\Addins\YYYY\

.PARAMETER Year
    Override the Revit year. By default the script reads it from the file name
    of the .addin that sits beside this script.

.PARAMETER Force
    Overwrite an existing installation without prompting.

.PARAMETER Machine
    Install to %PROGRAMDATA% (all users) instead of %APPDATA% (current user).
    Requires elevation when the destination folder needs creating.

.EXAMPLE
    .\CopyToRevitAddins.ps1
    Installs into the current user's Revit Addins folder.

.EXAMPLE
    .\CopyToRevitAddins.ps1 -Year 2025 -Force
    Force-install the 2025 build for the current user.

.EXAMPLE
    .\CopyToRevitAddins.ps1 -Machine
    Install for all users (requires admin).

.NOTES
    File:    CopyToRevitAddins.ps1
    Author:  BIM TOOLS
    License: MIT
#>

[CmdletBinding()]
param(
    [int]$Year,
    [switch]$Force,
    [switch]$Machine
)

$ErrorActionPreference = 'Stop'

function Get-AddinYearFromManifest {
    $addinFiles = Get-ChildItem -Path $PSScriptRoot -Filter '*.addin' -File
    if ($addinFiles.Count -eq 0) {
        throw "No .addin manifest found next to CopyToRevitAddins.ps1. Run this script from inside an unzipped release folder."
    }
    $addinFile = $addinFiles | Select-Object -First 1
    if ($addinFile.Name -match 'Revit(\d{4})') {
        return [int]$Matches[1]
    }
    throw "Could not infer Revit year from manifest file name '$($addinFile.Name)'. Pass -Year explicitly."
}

if (-not $Year) {
    $Year = Get-AddinYearFromManifest
}

$baseFolder = if ($Machine) {
    $env:ProgramData
} else {
    [Environment]::GetFolderPath('ApplicationData')
}

$revitAddins    = Join-Path $baseFolder "Autodesk\Revit\Addins\$Year"
$targetSubdir   = Join-Path $revitAddins 'BimTools.SpxExporter'

if (-not (Test-Path $revitAddins)) {
    Write-Error @"
Revit $Year Addins folder does not exist at:
    $revitAddins

This usually means Autodesk Revit $Year is not installed on this machine.
Install Revit $Year first, or pick a different year with -Year <YYYY>.
"@
    exit 1
}

if (-not (Test-Path $targetSubdir)) {
    New-Item -ItemType Directory -Path $targetSubdir -Force | Out-Null
}

$dll     = "BimTools.SpxExporter.Revit$Year.dll"
$pdb     = "BimTools.SpxExporter.Revit$Year.pdb"
$addin   = "BimTools.SpxExporter.Revit$Year.addin"

foreach ($file in @($dll, $addin, $pdb)) {
    $source = Join-Path $PSScriptRoot $file
    if (-not (Test-Path $source)) {
        # PDB is optional - skip silently if missing.
        if ($file -eq $pdb) { continue }
        Write-Error "Missing expected file: $source"
        exit 1
    }
    $dest = Join-Path $targetSubdir $file
    if ((Test-Path $dest) -and -not $Force) {
        $reply = Read-Host "Destination exists: $dest. Overwrite? [y/N]"
        if ($reply -notmatch '^[yY]') {
            Write-Host "Skipped $file."
            continue
        }
    }
    Copy-Item -Path $source -Destination $dest -Force
    Write-Host "Installed: $dest"
}

# The .addin manifest references the DLL via the relative path
#   BimTools.SpxExporter\BimTools.SpxExporter.RevitYYYY.dll
# so the manifest itself needs to sit in the Addins root (not in the subfolder).
$addinSource = Join-Path $PSScriptRoot $addin
$addinDest   = Join-Path $revitAddins $addin
if ((Test-Path $addinDest) -and -not $Force) {
    $reply = Read-Host "Destination exists: $addinDest. Overwrite? [y/N]"
    if ($reply -notmatch '^[yY]') {
        Write-Host "Skipped manifest (kept existing)."
    } else {
        Copy-Item -Path $addinSource -Destination $addinDest -Force
        Write-Host "Installed manifest: $addinDest"
    }
} else {
    Copy-Item -Path $addinSource -Destination $addinDest -Force
    Write-Host "Installed manifest: $addinDest"
}

Write-Host ""
Write-Host "Done. Restart Revit $Year and the 'BIM TOOLS' ribbon tab will appear." -ForegroundColor Green
Write-Host "Install mode: $(if ($Machine) { 'PerMachine (all users)' } else { 'PerUser (current user)' })" -ForegroundColor Cyan
