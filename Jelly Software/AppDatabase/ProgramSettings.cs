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
        /// Property 1
        /// Toggles console foreground color customization across the application.
        /// </summary>
        private bool _allowColors = true;
        public bool AllowColors 
        { 
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _allowColors;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _allowColors = value;
            }
        }

        /// <summary>
        /// Property 2
        /// Toggles console audio alert beeps (e.g. on error or critical initialization failure).
        /// </summary>
        private bool _allowBeep = true;
        public bool AllowBeep 
        { 
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _allowBeep;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _allowBeep = value;
            }
        }

        /// <summary>
        /// Property 3
        /// Toggles whether the initialization progress messages are displayed to the user during startup.
        /// </summary>
        private bool _allowShowInitializeProgress = true;
        public bool AllowShowInitializeProgress 
        { 
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _allowShowInitializeProgress;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _allowShowInitializeProgress = value;
            }
        }

        /// <summary>
        /// Specifies the file path for the program settings JSON configuration file.
        /// </summary>
        private static readonly string FilePath = "Database\\Settings\\ProgramSettings.json";

        /// <summary>
        /// Loads settings from the JSON configuration file, falling back to default values if missing or corrupt.
        /// </summary>
        public static ProgramSettings Load()
        {
            _AppDatabase.VerifyDatabaseDirectories();
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var settings = JsonSerializer.Deserialize<ProgramSettings>(json);
                    if (settings != null)
                        return settings;
                    return new ProgramSettings();
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
            _AppDatabase.VerifyDatabaseDirectories();
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(FilePath, json);
        }
    }
}
