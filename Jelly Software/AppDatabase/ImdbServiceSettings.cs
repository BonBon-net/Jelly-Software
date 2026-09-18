using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jelly_Software.AppSettings
{
    public class ImdbServiceSettings
    {
        /// <summary>
        /// Property 1
        /// Toggles whether a dash is placed before the release year.
        /// </summary>
        private bool? _DashBeforeReleaseYear = null;
        public bool? DashBeforeReleaseYear
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _DashBeforeReleaseYear;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _DashBeforeReleaseYear = value;
            }
        }

        /// <summary>
        /// Property 2
        /// Toggles whether episode years are displayed.
        /// </summary>
        private bool? _allowEpisodeYear = null;
        public bool? AllowEpisodeYear
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _allowEpisodeYear;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _allowEpisodeYear = value;
            }
        }

        /// <summary>
        /// Property 3
        /// Toggles whether to use the episode's release year.
        /// </summary>
        private bool? _useEpisodeReleaseYear = null;
        public bool? UseEpisodeReleaseYear
        { 
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _useEpisodeReleaseYear;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _useEpisodeReleaseYear = value;
            }
        }

        /// <summary>
        /// Property 4
        /// Toggles whether a dash is placed after the release year.
        /// </summary>
        private bool? _dashAfterReleaseYear = null;
        public bool? DashAfterReleaseYear
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _dashAfterReleaseYear;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _dashAfterReleaseYear = value;
            }
        }

        /// <summary>
        /// Property 5
        /// Toggles whether episode names are displayed.
        /// </summary>
        private bool? _allowEpisodeName = null;
        public bool? AllowEpisodeName
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _allowEpisodeName;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _allowEpisodeName = value;
            }
        }

        /// <summary>
        /// Property 6
        /// Toggles whether a dash is placed after the season and episode numbers.
        /// </summary>
        private bool? _dashAfterSeasonEpisode = null;
        public bool? DashAfterSeasonEpisode
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _dashAfterSeasonEpisode;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _dashAfterSeasonEpisode = value;
            }
        }

        /// <summary>
        /// Property 7
        /// Toggles whether IMDB information is displayed.
        /// </summary>
        private bool? _allowImdb = null;
        public bool? AllowImdb
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _allowImdb;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _allowImdb = value;
            }
        }

        /// <summary>
        /// Property 8
        /// Toggles whether a dash is placed before IMDB information.
        /// </summary>
        private bool? _dashBeforeImdb = null;
        public bool? DashBeforeImdb
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _dashBeforeImdb;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _dashBeforeImdb = value;
            }
        }

        /// <summary>
        /// Property 9
        /// Toggles whether season and year information is displayed.
        /// </summary>
        private bool? _allowSeasonYear = null;
        public bool? AllowSeasonYear
        {
            get
            {
                _AppDatabase.VerifyDatabaseDirectories();
                return _allowSeasonYear;
            }
            set
            {
                _AppDatabase.VerifyDatabaseDirectories();
                _allowSeasonYear = value;
            }
        }

        /// <summary>
        /// Specifies the file path for the IMDB service settings JSON configuration file.
        /// </summary>
        private static readonly string FilePath = "Database\\Settings\\ImdbServiceSettings.json";

        /// <summary>
        /// Loads settings from the JSON configuration file, falling back to default values if missing or corrupt.
        /// </summary>
        public static ImdbServiceSettings Load()
        {
            _AppDatabase.VerifyDatabaseDirectories();
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var settings = JsonSerializer.Deserialize<ImdbServiceSettings>(json);
                    if (settings != null)
                        return settings;
                    return new ImdbServiceSettings();
                }
                catch
                {
                    // Dev Note: If JSON file is corrupt, fail gracefully to default settings without crashing
                    return new ImdbServiceSettings();
                }
            }

            // Create and persist default settings on initial run
            var defaultSettings = new ImdbServiceSettings();
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
