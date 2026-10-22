==========================================================================
BIM TOOLS SPX Exporter - Revit 2027 .NET 10 Patch Bundle
==========================================================================

Problem:
  The first CI build (v1.4.0 tag) succeeded for Revit 2022-2026 but
  FAILED for Revit 2027. Root cause: Autodesk shipped Revit 2027 on
  .NET 10, so the Nice3point.Revit.Api 2027.* NuGet package targets
  net10.0-windows7.0 - but our csproj declared net8.0-windows, so
  NuGet restore could not resolve a compatible dependency group.

Fix:
  1. Updated BimTools.SpxExporter.Revit2027.csproj to target net10.0-windows.
  2. Added 10.0.x to the .NET SDK versions installed by the GitHub Actions
     workflow.
  3. Updated the auto-deploy RevitAddinsDir for 2027 to use ProgramFiles
     (Revit 2027 moved the machine-wide addins folder from ProgramData
     to ProgramFiles for security).
  4. Updated CopyToRevitAddins.ps1 to know about the 2027 path change
     when the -Machine flag is used.
  5. Updated README.md, INSTALL.md, and ARCHITECTURE.md to reflect the
     .NET 10 requirement for Revit 2027.
  6. Updated scripts/generate_per_version_files.py so future
     regenerations produce the correct 2027 csproj.

Files in this bundle (overwrite the matching files in your local clone):
  README.md
  .github/workflows/build-multi-version.yml
  build/CopyToRevitAddins.ps1
  docs/ARCHITECTURE.md
  docs/INSTALL.md
  scripts/generate_per_version_files.py
  src/BimTools.SpxExporter.Revit2027/BimTools.SpxExporter.Revit2027.csproj

After copying these over your local clone, commit and re-tag:

    git add -A
    git commit -m "fix(2027): target net10.0-windows; Revit 2027 requires .NET 10 SDK

- BimTools.SpxExporter.Revit2027.csproj: net8.0-windows -> net10.0-windows
- workflow: add 10.0.x to setup-dotnet versions
- RevitAddinsDir for 2027: ProgramData -> ProgramFiles (Revit 2027 path change)
- CopyToRevitAddins.ps1: -Machine mode now uses ProgramFiles for year >= 2027
- docs: note .NET 10 requirement for Revit 2027"
    git tag v1.4.1
    git push origin main
    git push origin v1.4.1

The v1.4.1 tag push will trigger a new matrix build; all 6 jobs should
now succeed and a GitHub Release will be auto-published with 6 zips.
