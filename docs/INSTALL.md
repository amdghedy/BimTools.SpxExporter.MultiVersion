# Installing BIM TOOLS SPX Exporter into Revit

This document walks through installing a prebuilt release of
BIM TOOLS SPX Exporter into a local Revit installation.

For build instructions, see the [main README](../README.md).
For the multi-version architecture, see [ARCHITECTURE.md](ARCHITECTURE.md).

## Prerequisites

- A supported Autodesk Revit installation: **2022, 2023, 2024, 2025, 2026, or 2027**.
- For Revit 2022 / 2023 / 2024: nothing else required (uses .NET Framework 4.8 built into Windows).
- For Revit 2025 / 2026 / 2027: the .NET 8 Desktop Runtime (Revit installers usually include it).
- Local Administrator privileges are **not** required for per-user install (default mode).
- PowerShell 5.1 or later (built into Windows 10/11).

## Step 1 - Download the right release

1. Go to the repository's [Releases page](../../releases).
2. Find the release that matches your Revit year.
3. Download `BimTools.SpxExporter.RevitYYYY.zip` where `YYYY` matches your
   installed Revit (e.g. `BimTools.SpxExporter.Revit2025.zip`).

> Never mix years - the DLL built for Revit 2025 will not load into Revit 2023
> because the RevitAPI assembly is versioned per year.

## Step 2 - Unzip

Unzip the downloaded archive anywhere convenient, e.g. your Downloads folder.
The folder will contain:

| File | Purpose |
|------|---------|
| `BimTools.SpxExporter.RevitYYYY.dll` | The compiled add-in |
| `BimTools.SpxExporter.RevitYYYY.pdb` | Debug symbols (optional, used for stack traces) |
| `BimTools.SpxExporter.RevitYYYY.addin` | The Revit manifest pointing at the DLL |
| `CopyToRevitAddins.ps1` | One-shot installer |
| `README.md` | Project info |
| `LICENSE` | MIT license |

## Step 3 - Run the installer (per-user, default)

Open PowerShell **as your normal Windows user**:

```powershell
cd "$env:USERPROFILE\Downloads\BimTools.SpxExporter.RevitYYYY"
powershell -ExecutionPolicy Bypass -File .\CopyToRevitAddins.ps1
```

The installer will:

1. Read the year from the `.addin` file name.
2. Resolve `%APPDATA%\Autodesk\Revit\Addins\YYYY\`.
3. Create `%APPDATA%\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter\` if it doesn't exist.
4. Copy the DLL (and PDB if present) into that subfolder.
5. Copy the `.addin` manifest into the Addins root (so its relative `BimTools.SpxExporter\...dll` path resolves).
6. Prompt before overwriting an existing install (use `-Force` to skip).

## Step 3b - Run the installer (per-machine, all users)

If you want the add-in available to every user on the machine, open an
**elevated** PowerShell (Run as Administrator):

```powershell
cd "$env:USERPROFILE\Downloads\BimTools.SpxExporter.RevitYYYY"
powershell -ExecutionPolicy Bypass -File .\CopyToRevitAddins.ps1 -Machine
```

Installs to: `%PROGRAMDATA%\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter\`

If you see an error like *"Revit YYYY Addins folder does not exist"*, it means
Revit for that year is not installed on the machine.

## Step 4 - Restart Revit and verify

1. Close any open Revit sessions.
2. Start Revit YYYY.
3. Open any project file.
4. Look for a new ribbon tab named **BIM TOOLS** with a panel labelled
   **SPX Export** containing six buttons:
   - **Batch SPX Export** (large blue button)
   - **Export Active**
   - **Model Inspector**
   - **Element Data**
   - **Open Logs**
   - **About**

If the ribbon does not appear, check Revit's journal file for load errors:

```
%LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit YYYY\Journals\journal.XXXX.txt
```

Search for `BimTools` in the journal; a `FileNotFound` or `TypeLoadException`
entry will tell you which assembly Revit could not resolve.

## Step 5 - Using the tool

### Batch SPX Export

1. Click **Batch SPX Export**.
2. In the dialog, drag-drop or browse for one or more Revit model files (`.rvt`).
3. Pick the export engine (SYNCHRO add-in, IFC, Element Data, or Model Inspection).
4. Configure worksets, view selection, file naming, and overwrite policy.
5. Click **Start**. A progress window opens showing each file's status.
6. When done, click **Open Logs** to inspect the run.

### Export Active

Re-runs the last-used batch settings against the currently open Revit model.
Useful for re-exporting after a fix without re-opening the batch dialog.

### Model Inspector

Opens each model in the list, scans it for version mismatches, broken links,
workset configuration, warnings, and element counts, then writes a CSV report
beside each file.

### Element Data

Dumps element ids, categories, levels, phases, and parameter values to CSV
for 4D linking workflows.

## Troubleshooting

### Ribbon doesn't appear after install

- Verify the files actually landed in `%APPDATA%\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter\`.
- Verify the `.addin` file's `<Assembly>` element is a **relative path** of
  the form `BimTools.SpxExporter\BimTools.SpxExporter.RevitYYYY.dll`. The installer
  script preserves this automatically.
- Check the journal file for `ReflectionTypeLoadException`.
- If you have both per-user AND per-machine installs for the same year, delete one -
  Revit may refuse to load both.

### "No SYNCHRO exporter add-in manifest was found for Revit YYYY"

This means the Bentley SYNCHRO 4D Revit exporter add-in isn't installed.
Install SYNCHRO 4D on the same machine, restart Revit, click the SYNCHRO export
button once (so it registers in the journal), then click **Scan journals** in the
SPX Exporter dialog.

### Export produces an empty result

The export pipeline only processes elements that:

- Have a non-null `Category` with `HasMaterialQuantities == true`.
- Return a non-null `GeometryElement` from `Element.get_Geometry()`.

Elements like annotation tags, view templates, or families with no instance
geometry will be silently skipped. Open the Revit 3D view and confirm visible
elements exist before exporting.

### "Model was saved in Revit YYYY and cannot be opened in Revit YYYY"

Revit cannot open files saved by a newer version. The error message tells you
both versions. Either upgrade your Revit, or open the file in the newer Revit
and save it down.

### Uninstalling

Delete the `BimTools.SpxExporter` folder AND the matching `.addin` file from
the matching Revit Addins folder, then restart Revit:

```powershell
# Per-user:
Remove-Item "$env:APPDATA\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter" -Recurse -Force
Remove-Item "$env:APPDATA\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter.RevitYYYY.addin" -Force

# Per-machine (admin):
Remove-Item "$env:ProgramData\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter" -Recurse -Force
Remove-Item "$env:ProgramData\Autodesk\Revit\Addins\YYYY\BimTools.SpxExporter.RevitYYYY.addin" -Force
```

Settings and logs at `%APPDATA%\BimTools\SpxExporter\` are not removed by the
uninstaller - delete them manually if you want a clean slate.
