#!/usr/bin/env python3
"""
generate_per_version_files.py
-----------------------------
Generates the 6 per-year .csproj and .addin manifest files for the
BimTools.SpxExporter multi-version solution.

Revit version matrix:
    2022 -> net48,           Nice3point.Revit.Api 2022.*
    2023 -> net48,           Nice3point.Revit.Api 2023.*
    2024 -> net48,           Nice3point.Revit.Api 2024.*  (original target)
    2025 -> net8.0-windows,  Nice3point.Revit.Api 2025.*
    2026 -> net8.0-windows,  Nice3point.Revit.Api 2026.*
    2027 -> net10.0-windows, Nice3point.Revit.Api 2027.*  (Revit 2027 requires .NET 10)

Note: Revit 2027 also moved the machine-wide addins folder from
%ProgramData%\\Autodesk\\Revit\\Addins\\2027 to
%ProgramFiles%\\Autodesk\\Revit\\Addins\\2027. The generated csproj reflects
this so the post-build DeployToRevit target writes to the right place.

Each generated .csproj imports ../BimTools.SpxExporter/BimTools.SpxExporter.Shared.props
so all source files live in exactly one place.
"""
from pathlib import Path
from textwrap import dedent

BASE = Path("/home/z/my-project/download/BimTools.SpxExporter")

# (year, target_framework, nice3point_version, original_addin_id_guid)
# Note: Revit 2027 requires .NET 10. Revit 2025 and 2026 still target .NET 8 by default
# (Revit 2025.5 and 2026.5 also moved to .NET 10, but the original releases of 2025/2026
# used .NET 8 - keeping net8 here covers the majority of installs).
VERSIONS = [
    (2022, "net48",            "2022.*", "8f3a1c52-8d47-4b0e-9a65-2c7e1d4f8b32"),
    (2023, "net48",            "2023.*", "9f3a1c52-8d47-4b0e-9a65-2c7e1d4f8b33"),
    (2024, "net48",            "2024.*", "6f3a1c52-8d47-4b0e-9a65-2c7e1d4f8b31"),  # original GUID preserved
    (2025, "net8.0-windows",   "2025.*", "af3a1c52-8d47-4b0e-9a65-2c7e1d4f8b35"),
    (2026, "net8.0-windows",   "2026.*", "bf3a1c52-8d47-4b0e-9a65-2c7e1d4f8b36"),
    (2027, "net10.0-windows",  "2027.*", "cf3a1c52-8d47-4b0e-9a65-2c7e1d4f8b37"),  # Revit 2027 = .NET 10
]

CSPROJ_TEMPLATE = dedent("""\
    <?xml version="1.0" encoding="utf-8"?>
    <!--
      Auto-generated per-year project file. Do NOT edit by hand - regenerate via
      /scripts/generate_per_version_files.py.

      Target: Autodesk Revit {year}
      .NET target framework: {framework}
      Nice3point.Revit.Api version: {nice3point_version}
      Add-in ID (stable per year): {addin_guid}
    -->
    <Project Sdk="Microsoft.NET.Sdk">

      <PropertyGroup>
        <TargetFramework>{framework}</TargetFramework>
        <RootNamespace>BimTools.SpxExporter</RootNamespace>
        <AssemblyName>BimTools.SpxExporter.Revit{year}</AssemblyName>
        <LangVersion>10.0</LangVersion>
        <Nullable>disable</Nullable>
        <Platforms>x64</Platforms>
        <PlatformTarget>x64</PlatformTarget>
        <IsPackable>false</IsPackable>
        <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
        <AssemblyVersion>1.3.3.0</AssemblyVersion>
        <FileVersion>1.3.3.0</FileVersion>
        <Company>BIM TOOLS</Company>
        <Product>BIM TOOLS SPX Exporter for Revit {year}</Product>
        <Description>BIM TOOLS SPX Exporter - batch export Revit models to SYNCHRO 4D (.spx). Built for Autodesk Revit {year}.</Description>
        <Copyright>Copyright (c) BIM TOOLS</Copyright>

        <!-- Per-year identity injected into AppPaths.cs -->
        <RevitYear>{year}</RevitYear>
        <RevitPlatform>{framework}</RevitPlatform>
        <DefineConstants>$(DefineConstants);REVIT_YEAR_{year}</DefineConstants>

        <OutputPath>bin\\$(Configuration)\\</OutputPath>
        <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
        <UseWPF>true</UseWPF>
        <UseWindowsForms>true</UseWindowsForms>
        <EnableWindowsTargeting>true</EnableWindowsTargeting>
        <DebugType>embedded</DebugType>

        <!-- Optional auto-deploy: tweak RevitInstallDir at the command line if Revit is not in the default location. -->
        <RevitInstallDir Condition="'$(RevitInstallDir)' == ''">C:\\Program Files\\Autodesk\\Revit {year}\\</RevitInstallDir>
        <!--
          Revit 2027+ moved the machine-wide addins folder from %ProgramData% to %ProgramFiles%.
          Older Revit versions still expect %ProgramData%\\Autodesk\\Revit\\Addins\\YYYY\\.
        -->
        <RevitAddinsDir Condition="'$(RevitAddinsDir)' == ''">{revit_addins_dir}</RevitAddinsDir>
      </PropertyGroup>

      <!-- Shared sources - declared exactly once, in BimTools.SpxExporter.Shared.props -->
      <Import Project="..\\BimTools.SpxExporter\\BimTools.SpxExporter.Shared.props" />

      <!--
        Revit API references. Two strategies (mirrors the original 2024 project):

          1. If Revit {year} is installed locally at $(RevitInstallDir), reference the DLLs from there.
          2. Otherwise, restore Nice3point.Revit.Api NuGet packages (versioned per year) and use those.

        Nice3point.Revit.Api is a community-maintained NuGet feed that ships the same RevitAPI.dll
        and RevitAPIUI.dll found inside each year's Revit install, repackaged for use in CI without
        requiring a Revit license or install.
      -->
      <ItemGroup Condition="Exists('$(RevitInstallDir)RevitAPI.dll')">
        <Reference Include="RevitAPI">
          <HintPath>$(RevitInstallDir)RevitAPI.dll</HintPath>
          <Private>False</Private>
        </Reference>
        <Reference Include="RevitAPIUI">
          <HintPath>$(RevitInstallDir)RevitAPIUI.dll</HintPath>
          <Private>False</Private>
        </Reference>
      </ItemGroup>

      <ItemGroup Condition="!Exists('$(RevitInstallDir)RevitAPI.dll')">
        <PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="{nice3point_version}" ExcludeAssets="runtime" />
        <PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="{nice3point_version}" ExcludeAssets="runtime" />
      </ItemGroup>

      <!--
        Auto-deploy target: after every successful Windows build, copy the DLL and manifest
        into the Revit {year} add-ins folder so the next Revit start loads it. Skipped on
        non-Windows OSes (i.e. in Linux CI runners).
      -->
      <Target Name="DeployToRevit" AfterTargets="Build" Condition="'$(OS)' == 'Windows_NT'">
        <ItemGroup>
          <DeployBinaries Include="$(OutDir)BimTools.SpxExporter.Revit{year}.dll" />
          <DeployBinaries Include="$(OutDir)BimTools.SpxExporter.Revit{year}.pdb" Condition="Exists('$(OutDir)BimTools.SpxExporter.Revit{year}.pdb')" />
        </ItemGroup>
        <MakeDir Directories="$(RevitAddinsDir)BimTools.SpxExporter" ContinueOnError="true" />
        <Copy SourceFiles="@(DeployBinaries)" DestinationFolder="$(RevitAddinsDir)BimTools.SpxExporter" ContinueOnError="true" />
        <Copy SourceFiles="$(MSBuildProjectDirectory)\\BimTools.SpxExporter.Revit{year}.addin" DestinationFolder="$(RevitAddinsDir)" ContinueOnError="true" />
      </Target>

    </Project>
    """)

ADDIN_TEMPLATE = dedent("""\
    <?xml version="1.0" encoding="utf-8"?>
    <!--
      Revit add-in manifest for Autodesk Revit {year}.
      Installation target:
        %PROGRAMDATA%\\Autodesk\\Revit\\Addins\\{year}\\BimTools.SpxExporter.Revit{year}.addin
      The matching DLL must sit at:
        %PROGRAMDATA%\\Autodesk\\Revit\\Addins\\{year}\\BimTools.SpxExporter\\BimTools.SpxExporter.Revit{year}.dll
      (or, for per-user install: %APPDATA%\\Autodesk\\Revit\\Addins\\{year}\\...)
    -->
    <RevitAddIns>
      <AddIn Type="Application">
        <Name>BIM TOOLS SPX Exporter (Revit {year})</Name>
        <Assembly>BimTools.SpxExporter\\BimTools.SpxExporter.Revit{year}.dll</Assembly>
        <AddInId>{addin_guid}</AddInId>
        <FullClassName>BimTools.SpxExporter.Startup.SpxExporterApplication</FullClassName>
        <VendorId>BIMT</VendorId>
        <VendorDescription>BIM TOOLS</VendorDescription>
      </AddIn>
    </RevitAddIns>
    """)

def main() -> None:
    for year, framework, nice3point_version, addin_guid in VERSIONS:
        proj_dir = BASE / "src" / f"BimTools.SpxExporter.Revit{year}"
        proj_dir.mkdir(parents=True, exist_ok=True)

        # .csproj
        csproj_path = proj_dir / f"BimTools.SpxExporter.Revit{year}.csproj"
        # Revit 2027+ moved machine-wide addins to ProgramFiles. Earlier versions still use ProgramData.
        revit_addins_dir = (
            f"$(ProgramFiles)\\Autodesk\\Revit\\Addins\\{year}\\"
            if year >= 2027
            else f"$(ProgramData)\\Autodesk\\Revit\\Addins\\{year}\\"
        )
        csproj_path.write_text(CSPROJ_TEMPLATE.format(
            year=year,
            framework=framework,
            nice3point_version=nice3point_version,
            addin_guid=addin_guid,
            revit_addins_dir=revit_addins_dir,
        ), encoding="utf-8")

        # .addin manifest
        addin_path = proj_dir / f"BimTools.SpxExporter.Revit{year}.addin"
        addin_path.write_text(ADDIN_TEMPLATE.format(
            year=year,
            addin_guid=addin_guid,
        ), encoding="utf-8")

        print(f"  generated: {csproj_path.relative_to(BASE)}")
        print(f"  generated: {addin_path.relative_to(BASE)}")

if __name__ == "__main__":
    main()
