<#
 Builds the add-in for Revit 2022 - 2026 and (optionally) installs each build into
 %ProgramData%\Autodesk\Revit\Addins\<year>\.
 Usage:  .\Build-All.ps1                 # all versions, deploy
         .\Build-All.ps1 -Years 2024,2025 -NoDeploy
#>
param(
    [int[]]$Years = @(2022, 2023, 2024, 2025, 2026),
    [switch]$NoDeploy
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'BimTools.SpxExporter.csproj'
$failed = @()

foreach ($year in $Years) {
    Write-Host "=== Building for Revit $year ===" -ForegroundColor Cyan
    $deploy = if ($NoDeploy) { 'false' } else { 'true' }
    & dotnet build $project -c Release -p:RevitVersion=$year -p:DeployAfterBuild=$deploy
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build for Revit $year FAILED" -ForegroundColor Red
        $failed += $year
    }
}

if ($failed.Count -gt 0) {
    Write-Host ("Failed versions: " + ($failed -join ', ')) -ForegroundColor Red
    exit 1
}
Write-Host 'All builds succeeded. Output: bin\R<year>\' -ForegroundColor Green
