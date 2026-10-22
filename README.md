# BIM TOOLS SPX Exporter (multi-version)

A batch export and model-inspection utility for **Autodesk Revit (2022-2027) and Bentley SYNCHRO 4D workflows**.

BIM TOOLS SPX Exporter helps BIM and VDC teams prepare Revit models for downstream SYNCHRO
workflows. It brings batch processing, export configuration, model inspection, progress
reporting, and logging together in a Windows desktop interface.

The application is built with C#, WPF, and the MVVM pattern. It integrates with the installed
SYNCHRO Revit export functionality rather than claiming to implement the proprietary SPX
format itself.

> **This repository** is a multi-version fork of the original
> [amdghedy/BimTools.SpxExporter](https://github.com/amdghedy/BimTools.SpxExporter),
> extended from a single Revit 2024 target to six Revit versions: **2022, 2023, 2024, 2025, 2026, 2027**.

---

## Supported Revit versions

This repository produces **six** independently-built add-in binaries, one for each supported Revit year:

| Build | Revit year | .NET target | Nice3point.Revit.Api version | Original GUID |
|-------|------------|-------------|------------------------------|---------------|
| `BimTools.SpxExporter.Revit2022` | 2022 | `net48` | `2022.*` | `8f3a1c52-...` |
| `BimTools.SpxExporter.Revit2023` | 2023 | `net48` | `2023.*` | `9f3a1c52-...` |
| `BimTools.SpxExporter.Revit2024` | 2024 | `net48` | `2024.*` | `6f3a1c52-...` (original, preserved) |
| `BimTools.SpxExporter.Revit2025` | 2025 | `net8.0-windows` | `2025.*` | `af3a1c52-...` |
| `BimTools.SpxExporter.Revit2026` | 2026 | `net8.0-windows` | `2026.*` | `bf3a1c52-...` |
| `BimTools.SpxExporter.Revit2027` | 2027 | `net8.0-windows` | `2027.*` | `cf3a1c52-...` |

Each binary has its own stable `<AddInId>` GUID so multiple Revit versions on the same machine
won't clash.

## Repository layout

```
BimTools.SpxExporter/
├── BimTools.SpxExporter.MultiVersion.sln      # Lists all 6 per-year projects
├── src/
│   ├── BimTools.SpxExporter/                   # Shared C# source (one copy, ~70 files)
│   │   ├── BimTools.SpxExporter.Shared.props  # Imported by every per-year project
│   │   ├── Commands/        (6 files)          # Revit IExternalCommand implementations
│   │   ├── Engines/         (6 files)          # Export engines: SYNCHRO, IFC, ElementData, etc.
│   │   ├── Infrastructure/  (7 files)          # AppPaths, IconFactory, RelayCommand, etc.
│   │   ├── Models/         (20 files)         # ExportContext, ExportSettings, ExportJobItem, etc.
│   │   ├── Services/       (22 files)          # BatchRunner, ModelOpenService, ViewResolverService, etc.
│   │   ├── Startup/         (1 file)           # SpxExporterApplication (IExternalApplication)
│   │   ├── ViewModels/      (2 files)          # MainViewModel, BatchProgressViewModel
│   │   └── Views/           (4 files)          # MainWindow.xaml(.cs), ProgressWindow.xaml(.cs)
│   ├── BimTools.SpxExporter.Revit2022/         # Per-year csproj + .addin
│   ├── BimTools.SpxExporter.Revit2023/
│   ├── BimTools.SpxExporter.Revit2024/         # Original target (GUID preserved)
│   ├── BimTools.SpxExporter.Revit2025/
│   ├── BimTools.SpxExporter.Revit2026/
│   └── BimTools.SpxExporter.Revit2027/
├── build/
│   └── CopyToRevitAddins.ps1                  # End-user installer script
├── .github/workflows/
│   └── build-multi-version.yml               # Matrix CI: build all 6, publish releases
├── scripts/
│   └── generate_per_version_files.py         # Regenerates per-year csproj/addin files
└── docs/
    ├── INSTALL.md
    └── ARCHITECTURE.md
```

## Key capabilities (inherited from the original tool)

- **Batch export workflow** — organize and process multiple Revit model files through a central
  interface, with optional journal-based dialog automation.
- **Active-model export** — initiate an export workflow for the model currently open in Revit.
- **Model inspection** — open many models and write a QA report: version, worksets, links,
  warnings, element counts.
- **Element data extraction** — prepare element-related information (ids, categories, levels,
  phases, parameters) as CSV for 4D linking.
- **Multiple export engines** — SYNCHRO add-in (direct SPX via the installed SYNCHRO exporter),
  IFC (built-in Revit exporter), Element Data CSV, and Model Inspection.
- **Progress feedback** — track batch-processing progress through a dedicated WPF progress window.
- **Logging and diagnostics** — maintain logs and provide access to the log folder.
- **Settings management** — centralize application settings and workflow configuration in
  `%AppData%\BimTools\SpxExporter\settings.json`.

## Building locally

You can build any single year with the .NET SDK appropriate for that year:

```powershell
# Revit 2022 / 2023 / 2024 (.NET Framework 4.8 - requires Visual Studio 2022 or Build Tools)
msbuild src/BimTools.SpxExporter.Revit2022/BimTools.SpxExporter.Revit2022.csproj /p:Configuration=Release /p:Platform=x64
msbuild src/BimTools.SpxExporter.Revit2023/BimTools.SpxExporter.Revit2023.csproj /p:Configuration=Release /p:Platform=x64
msbuild src/BimTools.SpxExporter.Revit2024/BimTools.SpxExporter.Revit2024.csproj /p:Configuration=Release /p:Platform=x64

# Revit 2025 / 2026 / 2027 (.NET 8)
dotnet build src/BimTools.SpxExporter.Revit2025/BimTools.SpxExporter.Revit2025.csproj -c Release
dotnet build src/BimTools.SpxExporter.Revit2026/BimTools.SpxExporter.Revit2026.csproj -c Release
dotnet build src/BimTools.SpxExporter.Revit2027/BimTools.SpxExporter.Revit2027.csproj -c Release
```

Or build all six at once with the solution file:

```powershell
msbuild BimTools.SpxExporter.MultiVersion.sln /p:Configuration=Release /p:Platform=x64
```

> Revit API references use the **Nice3point.Revit.Api** NuGet packages so the build
> does **not** require Revit to be installed. If Revit IS installed locally, the
> project auto-detects the DLLs at `C:\Program Files\Autodesk\Revit YYYY\` and
> references those instead (override with `-p:RevitInstallDir=...`).
>
> After every successful Windows build, a `DeployToRevit` MSBuild target copies
> the DLL + manifest into `%PROGRAMDATA%\Autodesk\Revit\Addins\YYYY\` automatically.
> (Skipped on Linux CI runners.)

## Installing into Revit

1. Download the matching `BimTools.SpxExporter.RevitYYYY.zip` from
   [Releases](../../releases).
2. Unzip it anywhere.
3. Open PowerShell in the unzipped folder and run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\CopyToRevitAddins.ps1
   ```

4. Restart Revit. A new **BIM TOOLS** ribbon tab will appear with the **SPX Export**
   panel containing the batch export, active export, inspector, element data, logs and
   about buttons.

The installer copies the DLL and `.addin` manifest into:

```
%APPDATA%\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter\
```

For all-users install (requires admin PowerShell):

```powershell
powershell -ExecutionPolicy Bypass -File .\CopyToRevitAddins.ps1 -Machine
```

Installs to: `%PROGRAMDATA%\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter\`

## Continuous integration

Pushing a tag matching `v*` (e.g. `v1.4.0`) triggers the GitHub Actions matrix workflow,
which:

1. Builds all six per-year projects on a Windows runner (in parallel).
2. Stages the DLL + PDB + manifest + installer script for each year.
3. Uploads six `BimTools.SpxExporter.RevitYYYY.zip` artifacts.
4. Publishes a GitHub Release attaching all six zips and generating release notes
   from merged PRs.

See [`.github/workflows/build-multi-version.yml`](.github/workflows/build-multi-version.yml).

## How the multi-version build works

The key insight is that **all 70 source files live in exactly one folder**
(`src/BimTools.SpxExporter/`). The six per-year projects in
`src/BimTools.SpxExporter.RevitYYYY/` are thin shells that:

1. Set `<TargetFramework>` to `net48` (Revit 2022-2024) or `net8.0-windows` (Revit 2025-2027).
2. Set `<DefineConstants>;REVIT_YEAR_YYYY</DefineConstants>` so `AppPaths.RevitYear` resolves to
   the correct year at compile time (no runtime config needed).
3. Reference the matching `Nice3point.Revit.Api.RevitAPI` and `RevitAPIUI` NuGet packages
   (versioned per year).
4. `<Import>` the shared `BimTools.SpxExporter.Shared.props` file, which globs all `.cs` and
   `.xaml` files into the compile.

For full details, see [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Version history

| Version | Notes |
|---------|-------|
| 1.3.3   | Current multi-version build target (this repository). Source is identical to upstream 1.3.3; only build configuration was added. |
| 1.3.0   | About dialog version string in upstream. |
| 1.0.0   | First multi-version release tag in this fork. |

## Contributing

1. Fork this repository.
2. Create a feature branch: `git checkout -b feature/your-feature`.
3. Make your changes in `src/BimTools.SpxExporter/` - **do not** copy source files into the
   per-year folders; they all share the same source via `<Import>`.
4. Build at least one Revit year locally and verify the export succeeds.
5. Open a pull request. The CI will build all six versions automatically.

If you're adding a new Revit year, see [docs/ARCHITECTURE.md > Adding a new Revit year](docs/ARCHITECTURE.md).

## License

MIT - see [LICENSE](LICENSE).

## Acknowledgments

- The original **BIM TOOLS SPX Exporter** by [@amdghedy](https://github.com/amdghedy) -
  [github.com/amdghedy/BimTools.SpxExporter](https://github.com/amdghedy/BimTools.SpxExporter)
- The **Nice3point.Revit.Api** NuGet packages by [Nice3point](https://github.com/Nice3point)
  make CI builds possible without a Revit install.
- Autodesk Revit and Bentley SYNCHRO are trademarks of their respective owners.
