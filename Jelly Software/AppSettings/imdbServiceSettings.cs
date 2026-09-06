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
        private bool _useEpisodeReleaseYear = false;
        public bool UseEpisodeReleaseYear
        { 
            get
            {
                return _useEpisodeReleaseYear;
            }
            set
            {
                _useEpisodeReleaseYear = value;
            }
        }

        private bool _dashAfterReleaseYear = false;
        public bool DashAfterReleaseYear
        {
            get
            {
                return _dashAfterReleaseYear;
            }
            set
            {
                _dashAfterReleaseYear = value;
            }
        }

        private bool _allowEpisodeName = false;
        public bool AllowEpisodeName
        {
            get
            {
                return _allowEpisodeName;
            }
            set
            {
                _allowEpisodeName = value;
            }
        }

        private bool _dashAfterSeasonEpisode = false;
        public bool DashAfterSeasonEpisode
        {
            get
            {
                return _dashAfterSeasonEpisode;
            }
            set
            {
                _dashAfterSeasonEpisode = value;
            }
        }

        private bool _allowImdb = false;
        public bool AllowImdb
        {
            get
            {
                return _allowImdb;
            }
            set
            {
                _allowImdb = value;
            }
        }

        private bool _dashBeforeImdb = false;
        public bool DashBeforeImdb
        {
            get
            {
                return _dashBeforeImdb;
            }
            set
            {
                _dashBeforeImdb = value;
            }
        }

        private bool _allowSeasonYear = false;
        public bool AllowSeasonYear
        {
            get
            {
                return _allowSeasonYear;
            }
            set
            {
                _allowSeasonYear = value;
            }
        }

        private bool _allowEpisodeYear = false;
        public bool AllowEpisodeYear
        {
            get
            {
                return _allowEpisodeYear;
            }
            set
            {
                _allowEpisodeYear = value;
            }
        }

        private static readonly string FilePath = "settings\\ImdbServiceSettings.json";

        /// <summary>
        /// Loads settings from the JSON configuration file, falling back to default values if missing or corrupt.
        /// </summary>
        public static ImdbServiceSettings Load()
        {
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
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(FilePath, json);
        }
    }
}
