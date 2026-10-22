using System;
using System.IO;

namespace BimTools.SpxExporter.Infrastructure
{
    internal static class AppPaths
    {
        public const string TabName = "BIM TOOLS";
        public const string PanelName = "SPX Export";
        public const string DialogTitle = "BIM TOOLS - SPX Exporter";

        /// <summary>
        /// The Revit year this build targets (e.g. 2022, 2023, 2024, 2025, 2026, 2027).
        /// Injected at compile time by each per-year .csproj via &lt;DefineConstants&gt;.
        /// Falls back to 2024 only when the constant is missing (e.g. when the shared
        /// source is compiled standalone for editor tooling).
        /// </summary>
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
        public const int RevitYear = 2024;
#endif

        public static string DataFolder
        {
            get
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "BimTools",
                    "SpxExporter");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string LogFolder
        {
            get
            {
                string path = Path.Combine(DataFolder, "Logs");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string PresetsFolder
        {
            get
            {
                string path = Path.Combine(DataFolder, "Presets");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(DataFolder, "settings.json"); }
        }
    }
}
