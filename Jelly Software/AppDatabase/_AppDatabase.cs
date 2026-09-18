using System;
using System.Reflection;
using static Jelly_Software.Tools.ImdbService;

namespace Jelly_Software.AppSettings
{
    public class _AppDatabase
    {
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

            int settingsCount = 12; // Total settings tracked during initialization

            try
            {
                ProgramSettings = AppSettings.ProgramSettings.Load();

                // Dev Note: Colored output during startup steps
                preBuildTools.WriteLineColored($"[LOADED] (1/{settingsCount}) Allow Colors: {ProgramSettings.AllowColors}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (2/{settingsCount}) Allow Sound Beep: {ProgramSettings.AllowBeep}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (3/{settingsCount}) Show Initialization Progress: {ProgramSettings.AllowShowInitializeProgress}", ConsoleColor.Cyan, true);
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                preBuildTools.WriteLineColored($"[CRITICAL ERROR] Failed to load program settings:\n{ex.Message}", ConsoleColor.Red, true);
                return true;
            }

            try
            {
                ImdbServiceSettings = AppSettings.ImdbServiceSettings.Load();

                // Dev Note: Colored output during startup steps
                preBuildTools.WriteLineColored($"[LOADED] (4/{settingsCount}) Use Episode Release Year: {ImdbServiceSettings.UseEpisodeReleaseYear}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (5/{settingsCount}) Dash After Release Year: {ImdbServiceSettings.DashAfterReleaseYear}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (6/{settingsCount}) Allow Episode Name: {ImdbServiceSettings.AllowEpisodeName}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (7/{settingsCount}) Dash After Season Episode: {ImdbServiceSettings.DashAfterSeasonEpisode}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (8/{settingsCount}) Allow IMDb: {ImdbServiceSettings.AllowImdb}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (9/{settingsCount}) Dash Before IMDb: {ImdbServiceSettings.DashBeforeImdb}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (10/{settingsCount}) Allow Season Year: {ImdbServiceSettings.AllowSeasonYear}", ConsoleColor.Cyan, true);
                preBuildTools.WriteLineColored($"[LOADED] (11/{settingsCount}) Allow Episode Year: {ImdbServiceSettings.AllowEpisodeYear}", ConsoleColor.Cyan, true);
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                preBuildTools.WriteLineColored($"[CRITICAL ERROR] Failed to load IMDb service settings:\n{ex.Message}", ConsoleColor.Red, true);
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

                Console.Clear();

                preBuildTools.WriteLineGreen(TxtFile.WelcomeMessage);
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
                else if (KEY.Key == ConsoleKey.Enter)
                {
                    settingSwitch();
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
                preBuildTools.WriteLineColored($"\n'W' or '^' UP -- 'S' or 'v' DOWN -- 'A' or '<' Page Left -- 'D' or '>' Page Right\n'[ENTER]' or '[NUM PAD ENTER]' select -- '[ESCAPE]' or '[BACKSPACE]' Exit\n\n============================== SETTINGS ({settingsPage}/{totalSettingsPages}) ==============================\n", ConsoleColor.Cyan);

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

            void settingSwitch()
            {
                if (settingsPage == 1)
                {
                    if (settingsCursor == 1)
                        _AppDatabase.ProgramSettings.AllowColors = !_AppDatabase.ProgramSettings.AllowColors;
                    else if (settingsCursor == 2)
                        _AppDatabase.ProgramSettings.AllowBeep = !_AppDatabase.ProgramSettings.AllowBeep;
                    else if (settingsCursor == 3)
                        _AppDatabase.ProgramSettings.AllowShowInitializeProgress = !_AppDatabase.ProgramSettings.AllowShowInitializeProgress;

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
                    preBuildTools.WriteLineGreen($"{(_AppDatabase.ProgramSettings.AllowColors ? "Enabled " : "Disabled")} | Allow Colors in Output");

                    curser(2);
                    preBuildTools.WriteLineGreen($"{(_AppDatabase.ProgramSettings.AllowBeep ? "Enabled " : "Disabled")} | Allow Beep Sound Alerts");

                    curser(3);
                    preBuildTools.WriteLineGreen($"{(_AppDatabase.ProgramSettings.AllowShowInitializeProgress ? "Enabled " : "Disabled")} | Show Initialization Progress");
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
            if (!Directory.Exists("Database"))
                throw new DirectoryNotFoundException("Database directory not found.");
            if (!Directory.Exists("Database\\Settings"))
                throw new DirectoryNotFoundException("Database\\Settings directory not found.");

            if (!File.Exists("Database\\Settings\\ProgramSettings.json"))
                throw new FileNotFoundException("ProgramSettings.json not found in Database\\Settings.");
            if (!File.Exists("Database\\Settings\\ImdbServiceSettings.json"))
                throw new FileNotFoundException("ImdbServiceSettings.json not found in Database\\Settings.");
        }
    }
}
