using Jelly_Software.AppDatabase.Settings;
using System;
using System.Reflection;
using static Jelly_Software.Tools.ImdbService;

namespace Jelly_Software.AppSettings
{
    public class _AppDatabase
    {
        public static readonly string DatabaseDirectory = "Database";
        public static readonly string SettingsDirectory = $"{DatabaseDirectory}\\Settings";

        /// <summary>
        /// A list of PropertyInfo arrays, each representing the public properties of a settings class. This is used to dynamically access and manipulate settings during runtime.
        /// </summary>
        private protected static List<PropertyInfo[]> properties = new();
        private _AppDatabase()
        {
            properties = new();
            // Retrieves all public properties (both instance and static)
            properties.Add(typeof(ProgramSettings).GetProperties());
            properties.Add(typeof(ImdbServiceSettings).GetProperties());
        }

        public static ProgramSettings ProgramSettings = null!;
        public static ImdbServiceSettings ImdbServiceSettings = null!;

        /// <summary>
        /// Loads application configuration and prints status updates to the console.
        /// </summary>
        /// <returns>True if a critical error occurs, otherwise False.</returns>
        public static bool LoadSettings()
        {
            new _AppDatabase();

            // Ensure the database directory exists before attempting to load settings
            if (!Directory.Exists("Database"))
                Directory.CreateDirectory("Database");

            // Ensure the settings directory exists before attempting to load settings
            if (!Directory.Exists("Database\\Settings"))
                Directory.CreateDirectory("Database\\Settings");

            int Count = 2; // Total settings tracked during initialization

            try
            {
                ProgramSettings = ProgramSettings.Load();

                // Dev Note: Colored output during startup steps
                preBuildTools.WriteLineColored($"[LOADED] (1/{Count}) Successfully loaded program settings", ConsoleColor.Cyan, true);
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                preBuildTools.WriteLineColored($"[CRITICAL ERROR] (1/{Count}) Failed to load program settings:\n{ex.Message}", ConsoleColor.DarkRed, true);
                return true;
            }

            try
            {
                ImdbServiceSettings = ImdbServiceSettings.Load();

                // Dev Note: Colored output during startup steps
                preBuildTools.WriteLineColored($"[LOADED] (2/{Count}) Successfully loaded IMDb service settings", ConsoleColor.Cyan, true);
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                preBuildTools.WriteLineColored($"[CRITICAL ERROR] (2/{Count}) Failed to load IMDb service settings:\n{ex.Message}", ConsoleColor.DarkRed, true);
                return true;
            }

            return false;
        }

        public static void Settings(string input)
        {
            Console.CursorVisible = false;

            int settingsCursor = 1;
            int settingsPage = 1;

            // Updated count to account for AllowColors and AllowBeep
            int totalSettings = properties[settingsPage - 1].Length;
            int totalSettingsPages = properties.Count;
            while (true)
            {
                Console.BackgroundColor = _AppDatabase.ProgramSettings.BackgroundColor;
                Console.ForegroundColor = preBuildTools.EnsureContrast(ConsoleColor.Gray);
                Console.Clear();

                preBuildTools.WriteLineColored(TxtFile.WelcomeMessage, ConsoleColor.Green);
                preBuildTools.WriteColored("> ", ConsoleColor.Green);
                Console.WriteLine(input);

                writeSETTINGS();

                ConsoleKeyInfo KEY = Console.ReadKey();

                if (KEY.Key == ConsoleKey.W || KEY.Key == ConsoleKey.UpArrow)
                {
                    settingsCursor--;
                    if (settingsCursor < 1)
                        settingsCursor = totalSettings;
                }
                else if (KEY.Key == ConsoleKey.S || KEY.Key == ConsoleKey.DownArrow)
                {
                    settingsCursor++;
                    if (settingsCursor > totalSettings)
                        settingsCursor = 1;
                }
                else if (KEY.Key == ConsoleKey.A || KEY.Key == ConsoleKey.LeftArrow)
                {
                    settingsPage--;
                    settingsCursor = 1;
                    if (settingsPage < 1)
                        settingsPage = totalSettingsPages;
                    totalSettings = properties[settingsPage - 1].Length;
                }
                else if (KEY.Key == ConsoleKey.D || KEY.Key == ConsoleKey.RightArrow)
                {
                    settingsPage++;
                    settingsCursor = 1;
                    if (settingsPage > totalSettingsPages)
                        settingsPage = 1;
                    totalSettings = properties[settingsPage - 1].Length;
                }
                else if (KEY.Key == ConsoleKey.Enter || KEY.Key == ConsoleKey.N || KEY.Key == ConsoleKey.M)
                {
                    settingSwitch(KEY.Key);
                    totalSettings = properties[settingsPage - 1].Length;
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (_AppDatabase.ProgramSettings.AllowBeep)
                        Console.Beep();
                }
                else if (KEY.Key == ConsoleKey.Escape || KEY.Key == ConsoleKey.Backspace)
                {
                    if (KEY.Key == ConsoleKey.Escape)
                        Console.WriteLine("A");
                    break;
                }
            }

            Console.CursorVisible = true;

            void writeSETTINGS()
            {
                preBuildTools.WriteLineColored($"\n'W' or '^' UP -- 'S' or 'v' DOWN -- 'A' or '<' Page Left -- 'D' or '>' Page Right\n'[ENTER]' or '[NUM PAD ENTER]' select -- '[ESCAPE]' or '[BACKSPACE]' Exit\n'[N]' or '[M]' adjust value\n\n============================== SETTINGS ({settingsPage}/{totalSettingsPages}) ==============================\n", ConsoleColor.Cyan);

                writeSETTINGSPage();

                preBuildTools.WriteLineColored("\n============================================================================", ConsoleColor.Cyan);
            }

            void curser(int setting)
            {
                if (settingsCursor == setting)
                    preBuildTools.WriteColored(">> ", ConsoleColor.Green);
                else
                    Console.Write("   ");
            }

            void settingSwitch(ConsoleKey key)
            {
                if (settingsPage == 1)
                {
                    if (settingsCursor == 1)
                        _AppDatabase.ProgramSettings.AllowColors = !_AppDatabase.ProgramSettings.AllowColors;
                    else if (settingsCursor == 2)
                        _AppDatabase.ProgramSettings.AllowBeep = !_AppDatabase.ProgramSettings.AllowBeep;
                    else if (settingsCursor == 3)
                        _AppDatabase.ProgramSettings.AllowShowInitializeProgress = !_AppDatabase.ProgramSettings.AllowShowInitializeProgress;
                    else if (settingsCursor == 4 && key == ConsoleKey.M && _AppDatabase.ProgramSettings.InitializeProgressTimer <= (int.MaxValue - 15000))
                        _AppDatabase.ProgramSettings.InitializeProgressTimer += 5000;
                    else if (settingsCursor == 4 && key == ConsoleKey.N && _AppDatabase.ProgramSettings.InitializeProgressTimer >= 15000)
                        _AppDatabase.ProgramSettings.InitializeProgressTimer -= 5000;
                    else if (settingsCursor == 4 && key == ConsoleKey.Enter)
                        _AppDatabase.ProgramSettings.InitializeProgressTimer = 15000;
                    // Add this below the existing else if (settingsCursor == 4...) blocks
                    else if (settingsCursor == 5)
                    {
                        if (key == ConsoleKey.M)
                        {
                            int nextColor = (int)_AppDatabase.ProgramSettings.BackgroundColor + 1;
                            _AppDatabase.ProgramSettings.BackgroundColor = nextColor > 15 ? (ConsoleColor)0 : (ConsoleColor)nextColor;
                        }
                        else if (key == ConsoleKey.N)
                        {
                            int prevColor = (int)_AppDatabase.ProgramSettings.BackgroundColor - 1;
                            _AppDatabase.ProgramSettings.BackgroundColor = prevColor < 0 ? (ConsoleColor)15 : (ConsoleColor)prevColor;
                        }
                        else if (key == ConsoleKey.Enter)
                        {
                            _AppDatabase.ProgramSettings.BackgroundColor = ConsoleColor.Black;
                        }

                        _AppDatabase.ProgramSettings.Save();
                    }

                    _AppDatabase.ProgramSettings.Save();
                }
                else if (settingsPage == 2)
                {
                    if (settingsCursor == 1)
                    {
                        // 1: allowSeasonYear
                        if (_AppDatabase.ImdbServiceSettings.AllowSeasonYear == true)
                            _AppDatabase.ImdbServiceSettings.AllowSeasonYear = false;
                        else if (_AppDatabase.ImdbServiceSettings.AllowSeasonYear == false)
                            _AppDatabase.ImdbServiceSettings.AllowSeasonYear = null;
                        else if (_AppDatabase.ImdbServiceSettings.AllowSeasonYear == null)
                            _AppDatabase.ImdbServiceSettings.AllowSeasonYear = true;
                    }
                    else if (settingsCursor == 2)
                    {
                        // 2: allowEpisodeYear
                        if (_AppDatabase.ImdbServiceSettings.AllowEpisodeYear == true)
                            _AppDatabase.ImdbServiceSettings.AllowEpisodeYear = false;
                        else if (_AppDatabase.ImdbServiceSettings.AllowEpisodeYear == false)
                            _AppDatabase.ImdbServiceSettings.AllowEpisodeYear = null;
                        else if (_AppDatabase.ImdbServiceSettings.AllowEpisodeYear == null)
                            _AppDatabase.ImdbServiceSettings.AllowEpisodeYear = true;
                    }
                    else if (settingsCursor == 3 && _AppDatabase.ImdbServiceSettings.AllowEpisodeYear == true)
                    {
                        // 3: useEpisodeReleaseYear
                        if (_AppDatabase.ImdbServiceSettings.UseEpisodeReleaseYear == true)
                            _AppDatabase.ImdbServiceSettings.UseEpisodeReleaseYear = false;
                        else if (_AppDatabase.ImdbServiceSettings.UseEpisodeReleaseYear == false)
                            _AppDatabase.ImdbServiceSettings.UseEpisodeReleaseYear = null;
                        else if (_AppDatabase.ImdbServiceSettings.UseEpisodeReleaseYear == null)
                            _AppDatabase.ImdbServiceSettings.UseEpisodeReleaseYear = true;

                    }
                    else if (settingsCursor == 4 && _AppDatabase.ImdbServiceSettings.AllowEpisodeYear == true)
                    {
                        // 4: dashBeforeReleaseYear
                        if (_AppDatabase.ImdbServiceSettings.DashBeforeReleaseYear == true)
                            _AppDatabase.ImdbServiceSettings.DashBeforeReleaseYear = false;
                        else if (_AppDatabase.ImdbServiceSettings.DashBeforeReleaseYear == false)
                            _AppDatabase.ImdbServiceSettings.DashBeforeReleaseYear = null;
                        else if (_AppDatabase.ImdbServiceSettings.DashBeforeReleaseYear == null)
                            _AppDatabase.ImdbServiceSettings.DashBeforeReleaseYear = true;
                    }
                    else if (settingsCursor == 5 && _AppDatabase.ImdbServiceSettings.AllowEpisodeYear == true)
                    {
                        // 5: dashAfterReleaseYear
                        if (_AppDatabase.ImdbServiceSettings.DashAfterReleaseYear == true)
                            _AppDatabase.ImdbServiceSettings.DashAfterReleaseYear = false;
                        else if (_AppDatabase.ImdbServiceSettings.DashAfterReleaseYear == false)
                            _AppDatabase.ImdbServiceSettings.DashAfterReleaseYear = null;
                        else if (_AppDatabase.ImdbServiceSettings.DashAfterReleaseYear == null)
                            _AppDatabase.ImdbServiceSettings.DashAfterReleaseYear = true;
                    }
                    else if (settingsCursor == 6)
                    {
                        // 6: allowEpisodeName
                        if (_AppDatabase.ImdbServiceSettings.AllowEpisodeName == true)
                            _AppDatabase.ImdbServiceSettings.AllowEpisodeName = false;
                        else if (_AppDatabase.ImdbServiceSettings.AllowEpisodeName == false)
                            _AppDatabase.ImdbServiceSettings.AllowEpisodeName = null;
                        else if (_AppDatabase.ImdbServiceSettings.AllowEpisodeName == null)
                            _AppDatabase.ImdbServiceSettings.AllowEpisodeName = true;
                    }
                    else if (settingsCursor == 7 && _AppDatabase.ImdbServiceSettings.AllowEpisodeName == true)
                    {
                        // 7: dashAfterSeasonEpisode
                        if (_AppDatabase.ImdbServiceSettings.DashAfterSeasonEpisode == true)
                            _AppDatabase.ImdbServiceSettings.DashAfterSeasonEpisode = false;
                        else if (_AppDatabase.ImdbServiceSettings.DashAfterSeasonEpisode == false)
                            _AppDatabase.ImdbServiceSettings.DashAfterSeasonEpisode = null;
                        else if (_AppDatabase.ImdbServiceSettings.DashAfterSeasonEpisode == null)
                            _AppDatabase.ImdbServiceSettings.DashAfterSeasonEpisode = true;
                    }
                    else if (settingsCursor == 8)
                    {
                        // 8: allowImdb
                        if (_AppDatabase.ImdbServiceSettings.AllowImdb == true)
                            _AppDatabase.ImdbServiceSettings.AllowImdb = false;
                        else if (_AppDatabase.ImdbServiceSettings.AllowImdb == false)
                            _AppDatabase.ImdbServiceSettings.AllowImdb = null;
                        else if (_AppDatabase.ImdbServiceSettings.AllowImdb == null)
                            _AppDatabase.ImdbServiceSettings.AllowImdb = true;
                    }
                    else if (settingsCursor == 9 && _AppDatabase.ImdbServiceSettings.AllowImdb == true)
                    {
                        // 9: dashBeforeImdb
                        if (_AppDatabase.ImdbServiceSettings.DashBeforeImdb == true)
                            _AppDatabase.ImdbServiceSettings.DashBeforeImdb = false;
                        else if (_AppDatabase.ImdbServiceSettings.DashBeforeImdb == false)
                            _AppDatabase.ImdbServiceSettings.DashBeforeImdb = null;
                        else if (_AppDatabase.ImdbServiceSettings.DashBeforeImdb == null)
                            _AppDatabase.ImdbServiceSettings.DashBeforeImdb = true;
                    }

                    _AppDatabase.ImdbServiceSettings.Save();
                }
            }

            void writeSETTINGSPage()
            {
                if (settingsPage == 1)
                {
                    curser(1);
                    preBuildTools.WriteLineColored($"{(_AppDatabase.ProgramSettings.AllowColors ? "Enabled " : "Disabled")} | Allow Colors in Output", ConsoleColor.Green);

                    curser(2);
                    preBuildTools.WriteLineColored($"{(_AppDatabase.ProgramSettings.AllowBeep ? "Enabled " : "Disabled")} | Allow Beep Sound Alerts", ConsoleColor.Green);

                    curser(3);
                    preBuildTools.WriteLineColored($"{(_AppDatabase.ProgramSettings.AllowShowInitializeProgress ? "Enabled " : "Disabled")} | Show Initialization Progress", ConsoleColor.Green);

                    curser(4);
                    TimeSpan time = TimeSpan.FromMilliseconds(_AppDatabase.ProgramSettings.InitializeProgressTimer);
                    List<string> timeParts = new List<string>();

                    if (time.Hours > 0) timeParts.Add($"{time.Hours}H");
                    if (time.Minutes > 0) timeParts.Add($"{time.Minutes}M");
                    timeParts.Add($"{time.Seconds}S");

                    string formattedTime = string.Join(" ", timeParts);

                    // PadRight(10) ensures the string is always 10 characters wide before the " |"
                    preBuildTools.WriteLineColored($"{formattedTime.PadRight(9)}| Initialization Progress Timer", ConsoleColor.Green);

                    // Add this below curser(4) block
                    curser(5);
                    string bgColorName = _AppDatabase.ProgramSettings.BackgroundColor.ToString();
                    preBuildTools.WriteLineColored($"{bgColorName.PadRight(9)}| Application Background Color", ConsoleColor.Green);
                }
                else if (settingsPage == 2)
                {
                    // Helper functions to handle text, color, and spacing dynamically
                    string GetStatus(bool? setting) => setting switch { true => "Enabled     ", false => "Disabled    ", null => "Unselected  " };
                    string GetReason(bool? setting) => setting switch { true => "Enabled", false => "Disabled", null => "Unselected" };
                    ConsoleColor GetColor(bool? setting) => setting switch { true => ConsoleColor.Green, false => ConsoleColor.DarkGray, null => ConsoleColor.Cyan };

                    // 1: allowSeasonYear
                    curser(1);
                    var allowSeasonYear = _AppDatabase.ImdbServiceSettings.AllowSeasonYear;
                    preBuildTools.WriteColored(GetStatus(allowSeasonYear), GetColor(allowSeasonYear));
                    preBuildTools.WriteLineColored("| Allow Season Year", ConsoleColor.Green);

                    // 2: allowEpisodeYear
                    curser(2);
                    var allowEpYear = _AppDatabase.ImdbServiceSettings.AllowEpisodeYear;
                    preBuildTools.WriteColored(GetStatus(allowEpYear), GetColor(allowEpYear));
                    preBuildTools.WriteLineColored("| Allow Use Of Episode Year", ConsoleColor.Green);

                    // 3: useEpisodeReleaseYear
                    curser(3);
                    if (allowEpYear == true)
                    {
                        var useEpRelease = _AppDatabase.ImdbServiceSettings.UseEpisodeReleaseYear;
                        preBuildTools.WriteColored(GetStatus(useEpRelease), GetColor(useEpRelease));
                        preBuildTools.WriteLineColored("| Use Episode Release Year", ConsoleColor.Green);
                    }
                    else
                    {
                        preBuildTools.WriteColored("UNAVAILABLE ", ConsoleColor.Red);
                        preBuildTools.WriteLineColored($"| Use Episode Release Year (Disabled because Allow Episode Year is '{GetReason(allowEpYear)}')", ConsoleColor.Green);
                    }

                    // 4: dashBeforeReleaseYear
                    curser(4);
                    if (allowEpYear == true)
                    {
                        var dashBeforeRelease = _AppDatabase.ImdbServiceSettings.DashBeforeReleaseYear;
                        preBuildTools.WriteColored(GetStatus(dashBeforeRelease), GetColor(dashBeforeRelease));
                        preBuildTools.WriteLineColored("| Dash Before Release Year", ConsoleColor.Green);
                    }
                    else
                    {
                        preBuildTools.WriteColored("UNAVAILABLE ", ConsoleColor.Red);
                        preBuildTools.WriteLineColored($"| Dash Before Release Year (Disabled because Allow Episode Year is '{GetReason(allowEpYear)}')", ConsoleColor.Green);
                    }

                    // 5: dashAfterReleaseYear
                    curser(5);
                    if (allowEpYear == true)
                    {
                        var dashAfterRelease = _AppDatabase.ImdbServiceSettings.DashAfterReleaseYear;
                        preBuildTools.WriteColored(GetStatus(dashAfterRelease), GetColor(dashAfterRelease));
                        preBuildTools.WriteLineColored("| Dash After Release Year", ConsoleColor.Green);
                    }
                    else
                    {
                        preBuildTools.WriteColored("UNAVAILABLE ", ConsoleColor.Red);
                        preBuildTools.WriteLineColored($"| Dash After Release Year (Disabled because Allow Episode Year is '{GetReason(allowEpYear)}')", ConsoleColor.Green);
                    }

                    // 6: allowEpisodeName
                    curser(6);
                    var allowEpName = _AppDatabase.ImdbServiceSettings.AllowEpisodeName;
                    preBuildTools.WriteColored(GetStatus(allowEpName), GetColor(allowEpName));
                    preBuildTools.WriteLineColored("| Allow Episode Name", ConsoleColor.Green);

                    // 7: dashAfterSeasonEpisode
                    curser(7);
                    if (allowEpName == true)
                    {
                        var dashAfterSeason = _AppDatabase.ImdbServiceSettings.DashAfterSeasonEpisode;
                        preBuildTools.WriteColored(GetStatus(dashAfterSeason), GetColor(dashAfterSeason));
                        preBuildTools.WriteLineColored("| Dash After Season Episode", ConsoleColor.Green);
                    }
                    else
                    {
                        preBuildTools.WriteColored("UNAVAILABLE ", ConsoleColor.Red);
                        preBuildTools.WriteLineColored($"| Dash After Season Episode (Disabled because Allow Episode Name is '{GetReason(allowEpName)}')", ConsoleColor.Green);
                    }

                    // 8: allowImdb
                    curser(8);
                    var allowImdb = _AppDatabase.ImdbServiceSettings.AllowImdb;
                    preBuildTools.WriteColored(GetStatus(allowImdb), GetColor(allowImdb));
                    preBuildTools.WriteLineColored("| Allow IMDB", ConsoleColor.Green);

                    // 9: dashBeforeImdb
                    curser(9);
                    if (allowImdb == true)
                    {
                        var dashBeforeImdb = _AppDatabase.ImdbServiceSettings.DashBeforeImdb;
                        preBuildTools.WriteColored(GetStatus(dashBeforeImdb), GetColor(dashBeforeImdb));
                        preBuildTools.WriteLineColored("| Dash Before IMDB", ConsoleColor.Green);
                    }
                    else
                    {
                        preBuildTools.WriteColored("UNAVAILABLE ", ConsoleColor.Red);
                        preBuildTools.WriteLineColored($"| Dash Before IMDB (Disabled because Allow IMDB is '{GetReason(allowImdb)}')", ConsoleColor.Green);
                    }
                }
            }
        }

        /// <summary>
        /// Verifies the existence of required database directories and files. Throws exceptions if any are missing.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static void VerifyDatabaseDirectories()
        {
            // Check if the database and settings directories exist
            if (!Directory.Exists(_AppDatabase.DatabaseDirectory))
                throw new DirectoryNotFoundException("Database directory not found.");
            if (!Directory.Exists(_AppDatabase.SettingsDirectory))
                throw new DirectoryNotFoundException("Settings directory not found.");

            // Check if the required settings files exist
            if (!File.Exists(ProgramSettings.FilePath))
                throw new FileNotFoundException($"{ProgramSettings.FilePath.Split('\\').Last()} not found in {ProgramSettings.FilePath}");
            if (!File.Exists(ImdbServiceSettings.FilePath))
                throw new FileNotFoundException($"{ImdbServiceSettings.FilePath.Split('\\').Last()} not found in {ImdbServiceSettings.FilePath}");
        }
    }
}
