using System;
using System.IO;
using System.Text.Json;

namespace Jelly_Software.AppSettings
{
    /// <summary>
    /// Holds global application settings loaded from and saved to a local JSON file.
    /// </summary>
    public class ProgramSettings
    {
        // Dev Note: Must be public properties with { get; set; } for System.Text.Json serialization

        /// <summary>
        /// Toggles console foreground color customization across the application.
        /// </summary>
        public static bool AllowColors { get; set; } = true;

        /// <summary>
        /// Toggles console audio alert beeps (e.g. on error or critical initialization failure).
        /// </summary>
        public static bool AllowBeep { get; set; } = true;

        private static readonly string FilePath = "ProgramSettings.json";

        /// <summary>
        /// Loads settings from the JSON configuration file, falling back to default values if missing or corrupt.
        /// </summary>
        public static ProgramSettings Load()
        {
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<ProgramSettings>(json) ?? new ProgramSettings();
                }
                catch
                {
                    // Dev Note: If JSON file is corrupt, fail gracefully to default settings without crashing
                    return new ProgramSettings();
                }
            }

            // Create and persist default settings on initial run
            var defaultSettings = new ProgramSettings();
            defaultSettings.Save();
            return defaultSettings;
        }

        /// <summary>
        /// Persists current setting values to the JSON file on disk.
        /// </summary>
        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(FilePath, json);
        }
    }
}
