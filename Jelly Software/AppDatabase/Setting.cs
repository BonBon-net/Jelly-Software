using System.Reflection;
using System.Text.Json;
using static Jelly_Software.Tools.ImdbService;

namespace Jelly_Software.AppSettings
{
    public class Setting
    {
        public bool IsProgramInitialized = false;

        /// <summary>
        /// Specifies the directory where the program's database and settings files are stored.
        /// </summary>
        public static readonly string DatabaseDirectory = "Database";

        /// <summary>
        /// Specifies the file path for the program settings JSON configuration file.
        /// </summary>
        public static readonly string FilePath = $"{DatabaseDirectory}\\Settings.json";

        /// <summary>
        /// A list of PropertyInfo arrays, each representing the public properties of a settings class. This is used to dynamically access and manipulate settings during runtime.
        /// </summary>
        private PropertyInfo[] properties = [];
        public Setting()
        {
            // Retrieves all public properties (both instance and static)
            properties = typeof(Setting).GetProperties();
        }

        /// <summary>
        /// Property 1
        /// Toggles console foreground color customization across the application.
        /// </summary>
        private protected static bool _allowColors = true;
        public bool AllowColors
        {
            get
            {
                VerifyDatabaseDirectories();
                return _allowColors;
            }
            set
            {
                VerifyDatabaseDirectories();
                _allowColors = value;
            }
        }

        /// <summary>
        /// Property 2
        /// Toggles console audio alert beeps (e.g. on error or critical initialization failure).
        /// </summary>
        private protected static bool _allowBeep = true;
        public bool AllowBeep
        {
            get
            {
                VerifyDatabaseDirectories();
                return _allowBeep;
            }
            set
            {
                VerifyDatabaseDirectories();
                _allowBeep = value;
            }
        }

        /// <summary>
        /// Property 3
        /// Toggles whether the initialization progress messages are displayed to the user during startup.
        /// </summary>
        private protected static bool _allowShowInitializeProgress = true;
        public bool AllowShowInitializeProgress
        {
            get
            {
                VerifyDatabaseDirectories();
                return _allowShowInitializeProgress;
            }
            set
            {
                VerifyDatabaseDirectories();
                _allowShowInitializeProgress = value;
            }
        }

        /// <summary>
        /// Property 4
        /// Specifies the timer value for initialization progress messages.
        /// </summary>
        private protected static readonly int _DefaultInitializeProgressTimer = 15000; // Default to 15 seconds
        private protected static int _initializeProgressTimer = _DefaultInitializeProgressTimer;
        public int InitializeProgressTimer
        {
            get
            {
                VerifyDatabaseDirectories();
                return _initializeProgressTimer;
            }
            set
            {
                VerifyDatabaseDirectories();
                _initializeProgressTimer = value;
            }
        }

        /// <summary>
        /// Property 5
        /// Specifies the application background color.
        /// </summary>
        private protected static ConsoleColor _backgroundColor = ConsoleColor.Black;
        public ConsoleColor BackgroundColor
        {
            get
            {
                VerifyDatabaseDirectories();
                return _backgroundColor;
            }
            set
            {
                VerifyDatabaseDirectories();
                _backgroundColor = value;
            }
        }
        /// <summary>
        /// Property 6
        /// Toggles whether a dash is placed before the release year.
        /// </summary>
        private protected static bool? _DashBeforeReleaseYear = null;
        public bool? DashBeforeReleaseYear
        {
            get
            {
                VerifyDatabaseDirectories();
                return _DashBeforeReleaseYear;
            }
            set
            {
                VerifyDatabaseDirectories();
                _DashBeforeReleaseYear = value;
            }
        }

        /// <summary>
        /// Property 7
        /// Toggles whether episode years are displayed.
        /// </summary>
        private protected static bool? _allowEpisodeYear = null;
        public bool? AllowEpisodeYear
        {
            get
            {
                VerifyDatabaseDirectories();
                return _allowEpisodeYear;
            }
            set
            {
                VerifyDatabaseDirectories();
                _allowEpisodeYear = value;
            }
        }

        /// <summary>
        /// Property 8
        /// Toggles whether to use the episode's release year.
        /// </summary>
        private protected static bool? _useEpisodeReleaseYear = null;
        public bool? UseEpisodeReleaseYear
        {
            get
            {
                VerifyDatabaseDirectories();
                return _useEpisodeReleaseYear;
            }
            set
            {
                VerifyDatabaseDirectories();
                _useEpisodeReleaseYear = value;
            }
        }

        /// <summary>
        /// Property 9
        /// Toggles whether a dash is placed after the release year.
        /// </summary>
        private protected static bool? _dashAfterReleaseYear = null;
        public bool? DashAfterReleaseYear
        {
            get
            {
                VerifyDatabaseDirectories();
                return _dashAfterReleaseYear;
            }
            set
            {
                VerifyDatabaseDirectories();
                _dashAfterReleaseYear = value;
            }
        }

        /// <summary>
        /// Property 10
        /// Toggles whether episode names are displayed.
        /// </summary>
        private protected static bool? _allowEpisodeName = null;
        public bool? AllowEpisodeName
        {
            get
            {
                VerifyDatabaseDirectories();
                return _allowEpisodeName;
            }
            set
            {
                VerifyDatabaseDirectories();
                _allowEpisodeName = value;
            }
        }

        /// <summary>
        /// Property 11
        /// Toggles whether a dash is placed after the season and episode numbers.
        /// </summary>
        private protected static bool? _dashAfterSeasonEpisode = null;
        public bool? DashAfterSeasonEpisode
        {
            get
            {
                VerifyDatabaseDirectories();
                return _dashAfterSeasonEpisode;
            }
            set
            {
                VerifyDatabaseDirectories();
                _dashAfterSeasonEpisode = value;
            }
        }

        /// <summary>
        /// Property 12
        /// Toggles whether IMDB information is displayed.
        /// </summary>
        private protected static bool? _allowImdb = null;
        public bool? AllowImdb
        {
            get
            {
                VerifyDatabaseDirectories();
                return _allowImdb;
            }
            set
            {
                VerifyDatabaseDirectories();
                _allowImdb = value;
            }
        }

        /// <summary>
        /// Property 13
        /// Toggles whether a dash is placed before IMDB information.
        /// </summary>
        private protected static bool? _dashBeforeImdb = null;
        public bool? DashBeforeImdb
        {
            get
            {
                VerifyDatabaseDirectories();
                return _dashBeforeImdb;
            }
            set
            {
                VerifyDatabaseDirectories();
                _dashBeforeImdb = value;
            }
        }

        /// <summary>
        /// Property 14
        /// Toggles whether season and year information is displayed.
        /// </summary>
        private protected static bool? _allowSeasonYear = null;
        public bool? AllowSeasonYear
        {
            get
            {
                VerifyDatabaseDirectories();
                return _allowSeasonYear;
            }
            set
            {
                VerifyDatabaseDirectories();
                _allowSeasonYear = value;
            }
        }

        /// <summary>
        /// Loads settings from the JSON configuration file, falling back to default values if missing or corrupt.
        /// </summary>
        public Setting Load()
        {
            VerifyDatabaseDirectories();
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var settings = JsonSerializer.Deserialize<Setting>(json);
                    if (settings != null)
                        return settings;
                    return new Setting();
                }
                catch
                {
                    // Dev Note: If JSON file is corrupt, fail gracefully to default settings without crashing
                    return new Setting();
                }
            }

            // Create and persist default settings on initial run
            var defaultSettings = new Setting();
            defaultSettings.Save();
            return defaultSettings;
        }

        /// <summary>
        /// Persists current setting values to the JSON file on disk.
        /// </summary>
        public void Save()
        {
            VerifyDatabaseDirectories();
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(FilePath, json);
        }

        public void Settings(string input)
        {
            Console.CursorVisible = false;

            int totalSettings = properties.Length;
            int pageSize = 10;

            // Calculate total pages
            int totalSettingsPages = totalSettings == 0 ? 1 : (totalSettings + pageSize - 1) / pageSize;

            int settingsCursor = 1;
            int settingsPage = 1;

            // Helper to determine the number of items rendered on a given page
            int GetMaxItemsForPage(int page)
            {
                if (page < totalSettingsPages)
                    return pageSize;

                int remainder = totalSettings % pageSize;
                return (remainder == 0 && totalSettings > 0) ? pageSize : remainder;
            }

            while (true)
            {
                // Re-evaluate page bounds in case properties changed dynamically
                totalSettings = properties.Length;
                totalSettingsPages = totalSettings == 0 ? 1 : (totalSettings + pageSize - 1) / pageSize;

                // Ensure settingsPage stays within valid bounds
                if (settingsPage > totalSettingsPages) settingsPage = totalSettingsPages;
                if (settingsPage < 1) settingsPage = 1;

                int currentMaxItems = GetMaxItemsForPage(settingsPage);

                // Render screen
                Console.BackgroundColor = BackgroundColor;
                Console.ForegroundColor = PreBuildTools.EnsureContrast(ConsoleColor.Green);
                Console.Clear();

                PreBuildTools.Write($"{TxtFile.WelcomeMessage}\n> ", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                PreBuildTools.WriteLine(input, PreBuildTools.Setting.AllowColors);

                writeSETTINGS();

                ConsoleKeyInfo KEY = Console.ReadKey();

                // UP / W Navigation (Wraps around within the CURRENT page)
                if (KEY.Key == ConsoleKey.W || KEY.Key == ConsoleKey.UpArrow)
                {
                    settingsCursor--;
                    if (settingsCursor < 1)
                    {
                        settingsCursor = currentMaxItems; // Jump to bottom of current page
                    }
                }
                // DOWN / S Navigation (Wraps around within the CURRENT page)
                else if (KEY.Key == ConsoleKey.S || KEY.Key == ConsoleKey.DownArrow)
                {
                    settingsCursor++;
                    if (settingsCursor > currentMaxItems)
                    {
                        settingsCursor = 1; // Jump back to top of current page
                    }
                }
                // LEFT / A Navigation (Previous Page)
                else if (KEY.Key == ConsoleKey.A || KEY.Key == ConsoleKey.LeftArrow)
                {
                    settingsPage--;
                    if (settingsPage < 1)
                    {
                        settingsPage = totalSettingsPages;
                    }

                    // Clamp cursor to the new page's max items
                    int newPageMax = GetMaxItemsForPage(settingsPage);
                    if (settingsCursor > newPageMax)
                    {
                        settingsCursor = newPageMax;
                    }
                }
                // RIGHT / D Navigation (Next Page)
                else if (KEY.Key == ConsoleKey.D || KEY.Key == ConsoleKey.RightArrow)
                {
                    settingsPage++;
                    if (settingsPage > totalSettingsPages)
                    {
                        settingsPage = 1;
                    }

                    // Clamp cursor to the new page's max items
                    int newPageMax = GetMaxItemsForPage(settingsPage);
                    if (settingsCursor > newPageMax)
                    {
                        settingsCursor = newPageMax;
                    }
                }
                // SELECT / SWITCH
                else if (KEY.Key == ConsoleKey.Enter || KEY.Key == ConsoleKey.N || KEY.Key == ConsoleKey.M)
                {
                    settingSwitch(KEY.Key);

                    if (AllowBeep)
                        Console.Beep();
                }
                // EXIT
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
                PreBuildTools.WriteLine($"\n'W' or '^' UP -- 'S' or 'v' DOWN -- 'A' or '<' Page Left -- 'D' or '>' Page Right\n'[ENTER]' or '[NUM PAD ENTER]' select -- '[ESCAPE]' or '[BACKSPACE]' Exit\n'[N]' or '[M]' adjust value or [ENTER] for default value\n\n============================== SETTINGS ({settingsPage}/{totalSettingsPages}) ==============================\n", ConsoleColor.Cyan, PreBuildTools.Setting.AllowColors);

                writeSETTINGSPage();

                PreBuildTools.WriteLine("\n============================================================================", ConsoleColor.Cyan, PreBuildTools.Setting.AllowColors);
            }

            void curser(int setting)
            {
                if (settingsCursor == setting)
                    PreBuildTools.Write(">> ".PadRight(3), ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                else
                    PreBuildTools.Write("".PadRight(3), ConsoleColor.White, PreBuildTools.Setting.AllowColors);
            }

            void settingSwitch(ConsoleKey key)
            {
                if (settingsPage == 1)
                {
                    if (settingsCursor == 1)
                        AllowColors = !AllowColors;
                    else if (settingsCursor == 2)
                        AllowBeep = !AllowBeep;
                    else if (settingsCursor == 3)
                        AllowShowInitializeProgress = !AllowShowInitializeProgress;
                    else if (settingsCursor == 4 && key == ConsoleKey.M && InitializeProgressTimer <= (int.MaxValue - (_DefaultInitializeProgressTimer + 35000)))
                        InitializeProgressTimer += 5000;
                    else if (settingsCursor == 4 && key == ConsoleKey.N && InitializeProgressTimer >= _DefaultInitializeProgressTimer)
                        InitializeProgressTimer -= 5000;
                    else if (settingsCursor == 4 && key == ConsoleKey.Enter)
                        InitializeProgressTimer = _DefaultInitializeProgressTimer;
                    // Add this below the existing else if (settingsCursor == 4...) blocks
                    else if (settingsCursor == 5)
                    {
                        if (key == ConsoleKey.M)
                        {
                            int nextColor = (int)BackgroundColor + 1;
                            BackgroundColor = nextColor > 15 ? (ConsoleColor)0 : (ConsoleColor)nextColor;
                        }
                        else if (key == ConsoleKey.N)
                        {
                            int prevColor = (int)BackgroundColor - 1;
                            BackgroundColor = prevColor < 0 ? (ConsoleColor)15 : (ConsoleColor)prevColor;
                        }
                        else if (key == ConsoleKey.Enter)
                        {
                            BackgroundColor = ConsoleColor.Black;
                        }
                    }
                    else if (settingsCursor == 6)
                    {
                        // 1: allowSeasonYear
                        if (AllowSeasonYear == true)
                            AllowSeasonYear = false;
                        else if (AllowSeasonYear == false)
                            AllowSeasonYear = null;
                        else if (AllowSeasonYear == null)
                            AllowSeasonYear = true;
                    }
                    else if (settingsCursor == 7)
                    {
                        // 2: allowEpisodeYear
                        if (AllowEpisodeYear == true)
                        {
                            AllowEpisodeYear = false;
                            reset();
                        }
                        else if (AllowEpisodeYear == false)
                        {
                            AllowEpisodeYear = null;
                            reset();
                        }
                        else if (AllowEpisodeYear == null)
                        {
                            AllowEpisodeYear = true;
                        }

                        void reset()
                        {
                            UseEpisodeReleaseYear = null;
                            DashBeforeReleaseYear = null;
                            DashAfterReleaseYear = null;
                        }
                    }
                    else if (settingsCursor == 8 && AllowEpisodeYear == true)
                    {
                        // 3: useEpisodeReleaseYear
                        if (UseEpisodeReleaseYear == true)
                        {
                            UseEpisodeReleaseYear = false;
                        }
                        else if (UseEpisodeReleaseYear == false)
                        {
                            UseEpisodeReleaseYear = null;
                        }
                        else if (UseEpisodeReleaseYear == null)
                            UseEpisodeReleaseYear = true;

                    }
                    else if (settingsCursor == 9 && AllowEpisodeYear == true)
                    {
                        // 4: dashBeforeReleaseYear
                        if (DashBeforeReleaseYear == true)
                            DashBeforeReleaseYear = false;
                        else if (DashBeforeReleaseYear == false)
                            DashBeforeReleaseYear = null;
                        else if (DashBeforeReleaseYear == null)
                            DashBeforeReleaseYear = true;
                    }
                    else if (settingsCursor == 10 && AllowEpisodeYear == true)
                    {
                        // 5: dashAfterReleaseYear
                        if (DashAfterReleaseYear == true)
                            DashAfterReleaseYear = false;
                        else if (DashAfterReleaseYear == false)
                            DashAfterReleaseYear = null;
                        else if (DashAfterReleaseYear == null)
                            DashAfterReleaseYear = true;
                    }
                }
                else if (settingsPage == 2)
                {
                    if (settingsCursor == 1)
                    {
                        // 6: allowEpisodeName
                        if (AllowEpisodeName == true)
                        {
                            AllowEpisodeName = false;
                            DashAfterSeasonEpisode = null;
                        }
                        else if (AllowEpisodeName == false)
                        {
                            AllowEpisodeName = null;
                            DashAfterSeasonEpisode = null;
                        }
                        else if (AllowEpisodeName == null)
                            AllowEpisodeName = true;
                    }
                    else if (settingsCursor == 2 && AllowEpisodeName == true)
                    {
                        // 7: dashAfterSeasonEpisode
                        if (DashAfterSeasonEpisode == true)
                            DashAfterSeasonEpisode = false;
                        else if (DashAfterSeasonEpisode == false)
                            DashAfterSeasonEpisode = null;
                        else if (DashAfterSeasonEpisode == null)
                            DashAfterSeasonEpisode = true;
                    }
                    else if (settingsCursor == 3)
                    {
                        // 8: allowImdb
                        if (AllowImdb == true)
                            AllowImdb = false;
                        else if (AllowImdb == false)
                            AllowImdb = null;
                        else if (AllowImdb == null)
                            AllowImdb = true;
                    }
                    else if (settingsCursor == 4)
                    {
                        // 9: dashBeforeImdb
                        if (DashBeforeImdb == true)
                            DashBeforeImdb = false;
                        else if (DashBeforeImdb == false)
                            DashBeforeImdb = null;
                        else if (DashBeforeImdb == null)
                            DashBeforeImdb = true;
                    }
                }

                Save();
            }

            void writeSETTINGSPage()
            {
                // Helper functions to handle text, color, and spacing dynamically
                int maxStatusLength = 12; // Adjust this value based on the longest status string you expect
                string GetStatus(bool? setting) => setting switch { true => "Enabled".PadRight(maxStatusLength), false => "Disabled".PadRight(maxStatusLength), null => "Unselected".PadRight(maxStatusLength) };
                string GetReason(bool? setting) => setting switch { true => "Enabled", false => "Disabled", null => "Unselected" };
                ConsoleColor GetColor(bool? setting) => setting switch { true => ConsoleColor.Green, false => ConsoleColor.DarkGray, null => ConsoleColor.Cyan };

                if (settingsPage == 1)
                {
                    curser(1);
                    PreBuildTools.Write(GetStatus(AllowColors), GetColor(AllowColors), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Allow Colors in Output", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    curser(2);
                    PreBuildTools.Write(GetStatus(AllowBeep), GetColor(AllowBeep), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Allow Beep Sound Alerts", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    curser(3);
                    PreBuildTools.Write(GetStatus(AllowShowInitializeProgress), GetColor(AllowShowInitializeProgress), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Show Initialization Progress", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    curser(4);
                    TimeSpan time = TimeSpan.FromMilliseconds(InitializeProgressTimer);
                    List<string> timeParts = new List<string>();

                    if (time.Hours > 0) timeParts.Add($"{time.Hours}h");
                    if (time.Minutes > 0) timeParts.Add($"{time.Minutes}m");
                    timeParts.Add($"{time.Seconds}s");

                    string formattedTime = string.Join(" ", timeParts);

                    // PadRight(10) ensures the string is always 10 characters wide before the " |"
                    PreBuildTools.Write(formattedTime.PadRight(maxStatusLength), ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Initialization Progress Timer", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    // Add this below curser(4) block
                    curser(5);
                    string bgColorName = BackgroundColor.ToString();
                    PreBuildTools.Write(bgColorName.PadRight(maxStatusLength), ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Application Background Color", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    // 1: allowSeasonYear
                    Console.WriteLine();
                    curser(6);
                    var allowSeasonYear = AllowSeasonYear;
                    PreBuildTools.Write(GetStatus(allowSeasonYear), GetColor(allowSeasonYear), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Allow use of release year in season folder's naming", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    // 2: allowEpisodeYear
                    curser(7);
                    var allowEpYear = AllowEpisodeYear;
                    PreBuildTools.Write(GetStatus(allowEpYear), GetColor(allowEpYear), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Allow use of release year in episode file's naming", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    // 3: useEpisodeReleaseYear
                    curser(8);
                    if (allowEpYear == true)
                    {
                        var useEpRelease = UseEpisodeReleaseYear;
                        PreBuildTools.Write(GetStatus(useEpRelease), GetColor(useEpRelease), PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine($"| Use Episode Release Year in episode file's naming ({useEpRelease switch { true => "Using Episode Year", false => "Using Tv Show Year", null => "Noting Selected" }})", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }
                    else
                    {
                        PreBuildTools.Write("UNAVAILABLE".PadRight(maxStatusLength), ConsoleColor.Red, PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine($"| Use Episode Release Year (Disabled because Allow Episode Year is '{GetReason(allowEpYear)}')", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }

                    // 4: dashBeforeReleaseYear
                    curser(9);
                    if (allowEpYear == true)
                    {
                        var dashBeforeRelease = DashBeforeReleaseYear;
                        PreBuildTools.Write(GetStatus(dashBeforeRelease), GetColor(dashBeforeRelease), PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine("| Use dash before release year in episode file's naming", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }
                    else
                    {
                        PreBuildTools.Write("UNAVAILABLE".PadRight(12), ConsoleColor.Red, PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine($"| Use dash before release year in episode file's naming (Disabled because Allow Episode Year is '{GetReason(allowEpYear)}')", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }

                    // 5: dashAfterReleaseYear
                    curser(10);
                    if (allowEpYear == true)
                    {
                        var dashAfterRelease = DashAfterReleaseYear;
                        PreBuildTools.Write(GetStatus(dashAfterRelease), GetColor(dashAfterRelease), PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine("| Use dash after release year in episode file's naming", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }
                    else
                    {
                        PreBuildTools.Write("UNAVAILABLE".PadRight(maxStatusLength), ConsoleColor.Red, PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine($"| Use dash after release year in episode file's naming (Disabled because Allow Episode Year is '{GetReason(allowEpYear)}')", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }
                }
                else if (settingsPage == 2)
                {
                    // 6: allowEpisodeName
                    curser(1);
                    var allowEpName = AllowEpisodeName;
                    PreBuildTools.Write(GetStatus(allowEpName), GetColor(allowEpName), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Allow episode name in episode file's naming", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    // 7: dashAfterSeasonEpisode
                    curser(2);
                    if (allowEpName == true)
                    {
                        var dashAfterSeason = DashAfterSeasonEpisode;
                        PreBuildTools.Write(GetStatus(dashAfterSeason), GetColor(dashAfterSeason), PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine("| Use a dash after the SxxExx format in the file name.", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }
                    else
                    {
                        PreBuildTools.Write("UNAVAILABLE".PadRight(maxStatusLength), ConsoleColor.Red, PreBuildTools.Setting.AllowColors);
                        PreBuildTools.WriteLine($"| Use a dash after the SxxExx format in the file name. (Disabled because Allow Episode Name is '{GetReason(allowEpName)}')", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                    }

                    // 8: allowImdb
                    curser(3);
                    var allowImdb = AllowImdb;
                    PreBuildTools.Write(GetStatus(allowImdb), GetColor(allowImdb), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Allow IMDB ('[imdbid-]')", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                    // 9: dashBeforeImdb
                    curser(4);
                    var dashBeforeImdb = DashBeforeImdb;
                    PreBuildTools.Write(GetStatus(dashBeforeImdb), GetColor(dashBeforeImdb), PreBuildTools.Setting.AllowColors);
                    PreBuildTools.WriteLine("| Use dash before IMDB in episode file's naming", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);
                }
            }
        }

        /// <summary>
        /// Verifies the existence of required database directories and files. Throws exceptions if any are missing.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public void VerifyDatabaseDirectories()
        {
            // Ensure the database directory exists
            if (!Directory.Exists(DatabaseDirectory))
                Directory.CreateDirectory(DatabaseDirectory); // Create the database directory if it doesn't exist

            // Ensure the settings file exists
            if (!File.Exists(FilePath))
            {
                File.Create(FilePath).Close(); // Create the settings file if it doesn't exist
                Save(); // Save default settings to the newly created file
            }
        }

        /// <summary>
        /// Creates an ImageData object from the specified image file path. The image is read as a byte array and converted to a Base64 string.
        /// </summary>
        /// <param name="inputImagePath"></param>
        /// <returns></returns>
        public static ImageData MakeImageData(string inputImagePath)
        {
            // Read image file as byte array
            byte[] imageBytes = File.ReadAllBytes(inputImagePath);

            // Convert byte array to Base64 string
            string base64String = Convert.ToBase64String(imageBytes);

            // Create and return the object
            return new ImageData
            {
                FileName = Path.GetFileName(inputImagePath),
                ImageBase64 = base64String
            };
        }

        /// <summary>
        /// Represents image data with a filename and its Base64-encoded string representation.
        /// </summary>
        public class ImageData
        {
            public string FileName { get; set; } = default!;
            public string ImageBase64 { get; set; } = default!;
        }
    }
}
