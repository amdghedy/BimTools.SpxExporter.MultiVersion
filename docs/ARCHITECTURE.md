# Multi-version architecture

This document explains how the BIM TOOLS SPX Exporter is structured to support
six Revit versions (2022, 2023, 2024, 2025, 2026, 2027) from a single source tree.

## The fundamental constraint: Revit 2024 split the .NET runtime

Autodesk Revit 2022 and 2023 are built on **.NET Framework 4.8** and use the
classic `mscorlib` / `System.Windows.Forms` assemblies. Revit 2025 (and all
later versions through 2027) are built on **.NET 8.0 Windows** and use the
CoreCLR runtime. The two runtimes have incompatible assembly binding models,
so a single DLL compiled against `net48` will not load into Revit 2025+ and
vice versa.

| Year | .NET runtime | MSBuild target framework | Nice3point.Revit.Api version |
|------|----------------|---------------------------|------------------------------|
| 2022 | .NET Framework 4.8 | `net48` | `2022.*` |
| 2023 | .NET Framework 4.8 | `net48` | `2023.*` |
| 2024 | .NET Framework 4.8 | `net48` | `2024.*` |
| 2025 | .NET 8.0 Windows | `net8.0-windows` | `2025.*` |
| 2026 | .NET 8.0 Windows | `net8.0-windows` | `2026.*` |
| 2027 | .NET 10.0 Windows | `net10.0-windows` | `2027.*` |

> **Important:** Revit 2027 shipped on .NET 10 (SDK `10.0.100`).
> The Revit 2025.5 and 2026.5 point releases also moved to .NET 10.
> This repo's 2025 and 2026 builds target .NET 8 (for the original 2025.0 / 2026.0 releases);
> if you're on 2025.5 or 2026.5, use the Revit 2027 build with per-user install.

A single `<TargetFrameworks>net48;net8.0-windows;net10.0-windows</TargetFrameworks>` project
**would** compile to all three runtimes, but the Revit API assembly loaded at
runtime changes per year - so we'd still need six different binding redirects,
six `.addin` manifests with distinct `AddInId` GUIDs, and six output directories.
A multi-targeted single project saves no real complexity and makes the CI
matrix harder to read.

> **Revit 2027 deployment note:** Revit 2027 also moved the machine-wide addins folder
> from `%ProgramData%\Autodesk\Revit\Addins\2027\` to
> `%ProgramFiles%\Autodesk\Revit\Addins\2027\` for improved security.
> The per-year csproj's `DeployToRevit` target and the `CopyToRevitAddins.ps1 -Machine`
> installer both handle this automatically.

## Why six separate projects

Each of the six projects in `src/BimTools.SpxExporter.RevitYYYY/`:

- Sets its own `<TargetFramework>` (`net48` or `net8.0-windows`).
- Sets `<DefineConstants>;REVIT_YEAR_YYYY</DefineConstants>` so the shared
  `AppPaths.RevitYear` constant resolves to the right year at compile time.
- References its own versioned `Nice3point.Revit.Api.RevitAPI` and
  `Nice3point.Revit.Api.RevitAPIUI` NuGet packages (e.g. `2025.*` for Revit 2025).
- Has its own unique `<AddInId>` GUID in the `.addin` manifest, so a user with
  multiple Revit versions installed won't get conflicts.
- Produces its own `BimTools.SpxExporter.RevitYYYY.dll` and `.addin` files.
- Lives in its own bin folder so the per-year installer can stage just the
  files for one year.
- Has its own `DeployToRevit` MSBuild target that auto-deploys into the matching
  Revit Addins folder after every successful Windows build.

All six projects **share the same C# source code** by importing
`src/BimTools.SpxExporter/BimTools.SpxExporter.Shared.props`, which
declares `<Compile Include>` globs over the 8 source folders:

```
src/
├── BimTools.SpxExporter/                     <-- ONE copy of all .cs/.xaml files
│   ├── BimTools.SpxExporter.Shared.props      <-- imported by every per-year project
│   ├── Commands/        (6 files)               <-- IExternalCommand implementations
│   ├── Engines/         (6 files)               <-- Export engines: SYNCHRO, IFC, etc.
│   ├── Infrastructure/  (7 files)               <-- AppPaths, IconFactory, RelayCommand
│   ├── Models/         (20 files)               <-- ExportContext, ExportSettings, etc.
│   ├── Services/       (22 files)               <-- BatchRunner, ModelOpenService, etc.
│   ├── Startup/         (1 file)                <-- SpxExporterApplication
│   ├── ViewModels/      (2 files)               <-- MainViewModel, BatchProgressViewModel
│   └── Views/           (4 files)               <-- MainWindow.xaml(.cs), ProgressWindow.xaml(.cs)
├── BimTools.SpxExporter.Revit2022/
│   ├── BimTools.SpxExporter.Revit2022.csproj  <-- imports ../Shared.props
│   └── BimTools.SpxExporter.Revit2022.addin
├── BimTools.SpxExporter.Revit2023/...
├── BimTools.SpxExporter.Revit2024/...         <-- preserves original 1.3.3 GUID
├── BimTools.SpxExporter.Revit2025/...
├── BimTools.SpxExporter.Revit2026/...
└── BimTools.SpxExporter.Revit2027/...
```

## Why `.Shared.props` and not `<Compile Include>` in each csproj

Stating the compile glob in a `.props` file means we never have to remember
to add a new `.cs` file to six separate project files when we add a class.
Adding `Services/NewService.cs` to the shared folder is enough - the next
build of every per-year project picks it up automatically.

## How `AppPaths.RevitYear` is injected per build

The original codebase had `AppPaths.RevitYear` hardcoded to 2024. For the
multi-version fork, the constant is selected at compile time via `#if`
directives that read MSBuild-defined constants:

```csharp
// src/BimTools.SpxExporter/Infrastructure/AppPaths.cs
#if REVIT_YEAR_2022
    public const int RevitYear = 2022;
#elif REVIT_YEAR_2023
    public const int RevitYear = 2023;
#elif REVIT_YEAR_2024
    public const int RevitYear = 2024;
#elif REVIT_YEAR_2025
    public const int RevitYear = 2025;
#elif REVIT_YEAR_2026
    public const int RevitYear = 2026;
#elif REVIT_YEAR_2027
    public const int RevitYear = 2027;
#else
    public const int RevitYear = 2024;  // fallback for standalone editor
#endif
```

Each per-year `.csproj` defines its own constant:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);REVIT_YEAR_2025</DefineConstants>
</PropertyGroup>
```

This means `AppPaths.RevitYear` is a **compile-time constant** baked into the
DLL - no runtime config file needed. The same source compiles to six different
binaries that all "know" which Revit year they target.

Everything that depends on the year (Addins folder resolution, SYNCHRO manifest
search, About dialog text, file-naming templates, version-mismatch warnings)
reads from this single constant - so the rest of the source code is **identical**
across all six builds.

## Why per-year `.addin` manifests

Revit's add-in loader refuses to register an add-in whose `<AddInId>` GUID
matches one already loaded. If all six years shared a single manifest, a user
with multiple Revit versions installed would get collisions when they switched
years. The generator script (`scripts/generate_per_version_files.py`) assigns
each year a stable, distinct GUID:

| Year | AddInId GUID |
|------|--------------|
| 2022 | `8f3a1c52-8d47-4b0e-9a65-2c7e1d4f8b32` |
| 2023 | `9f3a1c52-8d47-4b0e-9a65-2c7e1d4f8b33` |
| 2024 | `6f3a1c52-8d47-4b0e-9a65-2c7e1d4f8b31` (original upstream GUID, preserved) |
| 2025 | `af3a1c52-8d47-4b0e-9a65-2c7e1d4f8b35` |
| 2026 | `bf3a1c52-8d47-4b0e-9a65-2c7e1d4f8b36` |
| 2027 | `cf3a1c52-8d47-4b0e-9a65-2c7e1d4f8b37` |

The 2024 GUID is intentionally preserved from the upstream repo so existing
installs upgrade cleanly without orphaning their old `<AddInId>` registration.

## Why per-year release zips

End users typically have exactly one Revit version installed. Handing them
one zip with one DLL and one manifest means:

- No guessing which DLL to load.
- No version-mismatch crashes.
- The included `CopyToRevitAddins.ps1` installer knows exactly where to put
  files because the year is encoded in every file name.

## CI matrix design

The GitHub Actions workflow in `.github/workflows/build-multi-version.yml`
declares a matrix of six jobs that run in parallel:

```yaml
matrix:
  include:
    - revit_year: 2022
      framework:  net48
      nice3point: 2022.*
    - revit_year: 2023
      framework:  net48
      nice3point: 2023.*
    - revit_year: 2024
      framework:  net48
      nice3point: 2024.*
    - revit_year: 2025
      framework:  net8.0-windows
      nice3point: 2025.*
    - revit_year: 2026
      framework:  net8.0-windows
      nice3point: 2026.*
    - revit_year: 2027
      framework:  net8.0-windows
      nice3point: 2027.*
```

Each job:

1. Restores NuGet packages (Nice3point.Revit.Api).
2. Builds its project (`dotnet build` for net8.0, `msbuild` for net48).
3. Stages the DLL + PDB + manifest + installer into a per-year folder.
4. Zips it and uploads as a build artifact.

When the workflow was triggered by a tag push (`v*`), a separate `release` job
collects all six artifacts and attaches them to a GitHub Release.

The CI build explicitly forces the NuGet path by setting `-p:RevitInstallDir="C:\non-existent\"`
so it doesn't accidentally pick up a locally installed Revit (which would
produce binaries built against a different patch level than the Nice3point packages).

## Adding a new Revit year

When Autodesk ships Revit 2028:

1. Add an entry to the `VERSIONS` list in `scripts/generate_per_version_files.py`:
   ```python
   (2028, "net8.0-windows", "2028.*", "df3a1c52-8d47-4b0e-9a65-2c7e1d4f8b38"),
   ```
2. Add a new `#elif REVIT_YEAR_2028` branch in `src/BimTools.SpxExporter/Infrastructure/AppPaths.cs`.
3. Add a matching matrix entry to `.github/workflows/build-multi-version.yml`.
4. Add a new `<Project>` entry to `BimTools.SpxExporter.MultiVersion.sln`.
5. Run `python3 scripts/generate_per_version_files.py` to regenerate the
   `.csproj` and `.addin` files.
6. Commit and tag a new release.

## Removing a Revit year

If Autodesk deprecates a year (rare, but possible), do the inverse:

1. Remove the entry from `VERSIONS` in `scripts/generate_per_version_files.py`.
2. Delete `src/BimTools.SpxExporter.RevitYYYY/` and regenerate.
3. Update the matrix in `build-multi-version.yml` and the solution file.
4. Tag a new release; old releases remain available on GitHub.

## Relationship to the upstream repo

This multi-version fork is **source-identical** to the upstream
[amdghedy/BimTools.SpxExporter](https://github.com/amdghedy/BimTools.SpxExporter)
at version 1.3.3, with only the following minimal patches:

1. `Infrastructure/AppPaths.cs` - hardcoded `RevitYear = 2024` is now selected
   at compile time via `#if REVIT_YEAR_YYYY` directives.
2. `Services/SynchroCommandLocator.cs` - hardcoded "Revit 2024" string in the
   "no manifest found" error message now reads from `AppPaths.RevitYear`.
3. `Commands/AboutCommand.cs` - hardcoded "Revit 2024" string in the About
   dialog now reads from `AppPaths.RevitYear`.
4. `Views/MainWindow.xaml` - subtitle text "Batch-export Revit 2024 models"
   simplified to "Batch-export Revit models" (year shown in ribbon tab + dialog title).

All other 66 source files are byte-for-byte identical to upstream 1.3.3. This
makes it easy to merge future upstream changes by simply copying the new
source files over the shared `src/BimTools.SpxExporter/` folder.
