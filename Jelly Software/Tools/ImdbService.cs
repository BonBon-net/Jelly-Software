using Jelly_Software.AppSettings;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jelly_Software.Tools
{
    public class ImdbService
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly string OperationCancelled = "\n\nOperation cancelled by the user.";
        private static bool _RateLimitHit = false;
        private static bool _InvalidOperation = false;

        public static async Task TVShowMain()
        {
            while (true)
            {
                try
                {
                    _RateLimitHit = false;

                    if (_InvalidOperation)
                    {
                        Ending();
                        _InvalidOperation = false;
                    }

                    PreBuildTools.WriteLine(TxtFile.WelcomeMessage, ConsoleColor.Green);
                    PreBuildTools.Write("> ", ConsoleColor.Green);

                    string folderPath = Console.ReadLine() ?? throw new NullReferenceException();

                    if (folderPath.ToLower() == "help")
                    {
                        _InvalidOperation = true;
                        Help();
                        continue; // Prevents falling through to Ending()
                    }
                    else if (folderPath.ToLower() == "settings" || folderPath.ToLower() == "setting")
                    {
                        _InvalidOperation = true;
                        PreBuildTools.Setting.Settings(folderPath);
                        continue; // Prevents falling through to Ending()
                    }
                    else if (folderPath.ToLower() == "break" || folderPath.ToLower() == "stop" || folderPath.ToLower() == "exit")
                    {
                        PreBuildTools.WriteLine("Exiting the application...", ConsoleColor.Green);
                        break;
                    }
                    else
                    {
                        if (!Directory.Exists(folderPath))
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (PreBuildTools.Setting.AllowBeep)
                                Console.Beep();
                            PreBuildTools.WriteLine("Error: The folder path does not exist.\nPlease check the path and try again.", ConsoleColor.Red);
                            Ending();
                            continue;
                        }
                        if (!Directory.GetDirectories(folderPath).Any(d => Regex.IsMatch(d.Split("\\").Last(), @"(?i)(?:season|series|s)\s*\d+")))
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (PreBuildTools.Setting.AllowBeep)
                                Console.Beep();
                            PreBuildTools.WriteLine("[ERROR] No valid season folders found.\nExpected folder format: 'Season 01', 'Series 1', or 'S01'.", ConsoleColor.Red);
                            Ending();
                            continue;
                        }
                        if (!Directory.GetDirectories(folderPath)
                            .Where(d => Regex.IsMatch(d.Split("\\").Last(), @"(?i)(?:season|series|s)\s*\d+"))
                            .SelectMany(d => Directory.GetFiles(d))
                            .Any(f =>
                            {
                                string ext = f.Split('.').Last().ToLower();
                                return ext == "mkv" || ext == "mp4" || ext == "avi";
                            }))
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (PreBuildTools.Setting.AllowBeep)
                                Console.Beep();
                            PreBuildTools.WriteLine("[ERROR] No valid episode files (.mkv, .mp4, .avi) found inside the season folders.", ConsoleColor.Red);
                            Ending();
                            continue;
                        }

                        folderPath = $"{PreBuildTools.GoToParentDirectory(folderPath)}\\{PreBuildTools.SanitizeString(folderPath.Split("\\").Last())}";

                        string tvShowFolderName = folderPath.Split("\\").Last();
                        string tvShowName = GetTvShowFolderTvShowName(tvShowFolderName);
                        string releaseYear = GetTvShowFolderReleaseYear(tvShowFolderName);
                        string imdbId = GetTvShowFolderImdbId(tvShowFolderName);

                        ShowMediaMetadata showMetadata = null!;
                        ShowMediaMetadata initialShowMetadata = null!;
                        bool needsManualSelection = false;

                        bool isPlaceholderImdb = imdbId.Equals("tt0000000", StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(imdbId, @"^tt0{7,}$");

                        if (isPlaceholderImdb)
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (PreBuildTools.Setting.AllowBeep)
                                Console.Beep();
                            PreBuildTools.WriteLine($"\n[WARNING] Placeholder IMDb ID '{imdbId}' detected. Ignoring IMDb ID and searching database...", ConsoleColor.Yellow);
                            needsManualSelection = true;
                        }
                        else
                        {
                            try
                            {
                                initialShowMetadata = await GetShowAsync(imdbId, tvShowFolderName);

                                int.TryParse(releaseYear, out int folderYearInt);
                                bool titleMatches = initialShowMetadata.ShowTitle.Equals(tvShowName, StringComparison.OrdinalIgnoreCase);
                                bool yearMatches = folderYearInt == 0 || initialShowMetadata.ShowYear == folderYearInt;

                                if (!titleMatches || !yearMatches)
                                {
                                    // Dev Note: Check dev configuration before issuing sound alert
                                    if (PreBuildTools.Setting.AllowBeep)
                                        Console.Beep();
                                    PreBuildTools.WriteLine($"\n[WARNING] Folder name '{tvShowName} ({releaseYear})' doesn't perfectly match fetched data: '{initialShowMetadata.ShowTitle} ({initialShowMetadata.ShowYear})'.", ConsoleColor.Yellow);
                                    needsManualSelection = true;
                                }
                                else
                                {
                                    showMetadata = initialShowMetadata;
                                }
                            }
                            catch
                            {
                                // Dev Note: Check dev configuration before issuing sound alert
                                if (PreBuildTools.Setting.AllowBeep)
                                    Console.Beep();
                                PreBuildTools.WriteLine($"\n[WARNING] Could not automatically pull exact match for IMDb ID: {imdbId}.", ConsoleColor.Yellow);
                                needsManualSelection = true;
                            }
                        }

                        if (needsManualSelection)
                        {
                            var searchResults = new List<SearchResult>();

                            if (initialShowMetadata != null && !isPlaceholderImdb)
                            {
                                searchResults.Add(new SearchResult
                                {
                                    Title = initialShowMetadata.ShowTitle,
                                    Year = initialShowMetadata.ShowYear,
                                    ImdbId = initialShowMetadata.ShowImdbId,
                                    Type = "Scripted"
                                });
                            }

                            string cleanTvShowName = Regex.Replace(tvShowName, @"\s*\(\d{4}\)|\s*\[.*?\]", "").Trim();
                            PreBuildTools.WriteLine($"Searching database for variations of '{cleanTvShowName}'...", ConsoleColor.Green);

                            var rawSearchResults = await SearchTvMazeMultipleAsync(cleanTvShowName);

                            foreach (var res in rawSearchResults)
                            {
                                if (res.Type.Equals("TV Movie", StringComparison.OrdinalIgnoreCase) || res.Type.Equals("Movie", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Dev Note: Check dev configuration before issuing sound alert
                                    if (PreBuildTools.Setting.AllowBeep)
                                        Console.Beep();
                                    PreBuildTools.WriteLine($"\n[WARNING] '{res.Title} ({res.Year})' is classified as a movie ({res.Type}), skipping from TV show selection list.", ConsoleColor.Yellow);
                                }
                                else
                                {
                                    if (!searchResults.Any(s => s.ImdbId.Equals(res.ImdbId, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        searchResults.Add(res);
                                    }
                                }
                            }

                            if (searchResults.Count == 0)
                            {
                                // Dev Note: Check dev configuration before issuing sound alert
                                if (PreBuildTools.Setting.AllowBeep)
                                    Console.Beep();
                                PreBuildTools.WriteLine("No alternative TV shows found on TVMaze.", ConsoleColor.Green);
                                string[] manualQ = new string[] { "Would you like to manually enter a correct IMDb ID?", "Cancel operation" };
                                char[] manualA = new char[] { 'Y', 'N' };
                                bool wantManual = PreBuildTools.GetUserConfirmation(manualQ, manualA, new string[] { });

                                if (wantManual)
                                {
                                    PreBuildTools.Write("Enter the correct IMDb ID (e.g., tt0182576) > ", ConsoleColor.Green);

                                    string manualImdb = Console.ReadLine()?.Trim() ?? string.Empty;

                                    if (!string.IsNullOrEmpty(manualImdb) && manualImdb.StartsWith("tt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        imdbId = manualImdb;
                                        showMetadata = await GetShowAsync(imdbId, tvShowFolderName);
                                    }
                                    else
                                    {
                                        // Dev Note: Check dev configuration before issuing sound alert
                                        if (PreBuildTools.Setting.AllowBeep)
                                            Console.Beep();

                                        PreBuildTools.WriteLine("Invalid IMDb ID format. Canceling operation.", ConsoleColor.Red);
                                        Ending();
                                        continue;
                                    }
                                }
                                else
                                {
                                    // Dev Note: Check dev configuration before issuing sound alert
                                    if (PreBuildTools.Setting.AllowBeep)
                                        Console.Beep();

                                    PreBuildTools.WriteLine(OperationCancelled, ConsoleColor.Green);
                                    Ending();
                                    continue;
                                }
                            }
                            else
                            {
                                // Dev Note: Check dev configuration before issuing sound alert
                                if (PreBuildTools.Setting.AllowBeep)
                                    Console.Beep();

                                PreBuildTools.WriteLine("\nFound multiple possibilities. Please confirm which series this is:", ConsoleColor.Green);
                                for (int i = 0; i < searchResults.Count; i++)
                                {
                                    PreBuildTools.WriteLine($"  [{i + 1}] {searchResults[i].Title} ({searchResults[i].Year}) - IMDb ID: {searchResults[i].ImdbId}", ConsoleColor.Green);
                                }
                                PreBuildTools.WriteLine("  [0] None of these (Cancel)", ConsoleColor.Green);

                                int selectedIdx = -1;
                                while (true)
                                {
                                    PreBuildTools.Write("\nEnter the number of the correct show > ", ConsoleColor.Green);

                                    string choice = Console.ReadLine()?.Trim() ?? string.Empty;

                                    if (int.TryParse(choice, out selectedIdx) && selectedIdx >= 0 && selectedIdx <= searchResults.Count)
                                    {
                                        break;
                                    }

                                    // Dev Note: Check dev configuration before issuing sound alert
                                    if (PreBuildTools.Setting.AllowBeep)
                                        Console.Beep();

                                    PreBuildTools.WriteLine("Invalid input. Please enter a valid number from the list.", ConsoleColor.Red);
                                }

                                if (selectedIdx == 0)
                                {
                                    PreBuildTools.WriteLine(OperationCancelled, ConsoleColor.Green);
                                    Ending();
                                    continue;
                                }

                                var selectedShow = searchResults[selectedIdx - 1];
                                string newImdbId = selectedShow.ImdbId;

                                if (isPlaceholderImdb && !string.IsNullOrEmpty(newImdbId))
                                {
                                    Console.WriteLine();
                                    string[] repQ = new string[] { $"Would you like to replace the current IMDb ID ({imdbId}) with the new IMDb ID ({newImdbId})?", "Keep current / Cancel" };
                                    char[] repA = new char[] { 'Y', 'N' };
                                    bool replaceImdb = PreBuildTools.GetUserConfirmation(repQ, repA, new string[] { });

                                    if (replaceImdb == true)
                                    {
                                        imdbId = newImdbId;
                                    }
                                    else
                                    {
                                        PreBuildTools.WriteLine(OperationCancelled, ConsoleColor.Green);
                                        Ending();
                                        continue;
                                    }
                                }
                                else if (!isPlaceholderImdb && !imdbId.Equals(newImdbId, StringComparison.OrdinalIgnoreCase))
                                {
                                    imdbId = newImdbId;
                                }

                                showMetadata = await GetShowAsync(imdbId, tvShowFolderName);
                            }
                        }

                        showMetadata!.FolderPath = folderPath;

                        bool hasFileImdbIds = CheckAndApplyFileImdbIds(showMetadata);

                        if (hasFileImdbIds)
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (PreBuildTools.Setting.AllowBeep)
                                Console.Beep();

                            PreBuildTools.WriteLine("\n[INFO] Found IMDb IDs in video file names and mapped them to corresponding episodes.", ConsoleColor.Green);
                        }

                        Console.WriteLine();
                        string[] confirmQ = new string[] { $"Is this the correct TV Show: {showMetadata.ShowTitle} ({showMetadata.ShowYear})?", "Cancel operation" };
                        char[] confirmA = new char[] { 'Y', 'N' };
                        bool confirmShow = PreBuildTools.GetUserConfirmation(confirmQ, confirmA, new string[] { });

                        if (confirmShow)
                        {
                            PreBuildTools.WriteLine($"\nFetching metadata for IMDb ID: {imdbId} and folder: {tvShowFolderName}...\n", ConsoleColor.Green);

                            showMetadata.FolderPath = folderPath;
                            if (_RateLimitHit)
                            {
                                PreBuildTools.WriteLine($"\n\n\n\nShow Title: {showMetadata.ShowTitle}", ConsoleColor.Green);
                            }
                            else
                            {
                                PreBuildTools.WriteLine($"\nShow Title: {showMetadata.ShowTitle}", ConsoleColor.Green);
                            }

                            foreach (var season in showMetadata.Seasons)
                            {
                                PreBuildTools.WriteLine($"\n         Season {season.SeasonNumber} ({season.SeasonYear}):", ConsoleColor.DarkCyan);
                                foreach (var episode in season.Episodes)
                                {
                                    PreBuildTools.WriteLine($"  Episode {episode.EpisodeNumber}: {episode.EpisodeTitle} ({episode.EpisodeYear}) - IMDb ID: {episode.EpisodeImdbId}", ConsoleColor.Green);
                                }
                            }

                            PreBuildTools.WriteLine($"\n\n Show Year: {showMetadata.ShowYear}", ConsoleColor.Green);
                            PreBuildTools.WriteLine($" IMDb ID: {showMetadata.ShowImdbId}", ConsoleColor.Green);
                            PreBuildTools.WriteLine($" Folder Path: {showMetadata.FolderName}", ConsoleColor.Green);
                            PreBuildTools.WriteLine(" Seasons and Episodes:", ConsoleColor.Green);

                            PreBuildTools.WriteLine("\n Metadata fetched successfully!", ConsoleColor.Green);
                            Console.WriteLine();

                            string safeShowTitle = string.Concat(showMetadata.ShowTitle.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
                            string expectedFolderName = $"{safeShowTitle} ({showMetadata.ShowYear}) [imdbid-{showMetadata.ShowImdbId}]";

                            if (tvShowFolderName != expectedFolderName)
                            {
                                bool? renameFolder = null;
                                if (PreBuildTools.Setting.AllowImdb == null)
                                {
                                    Console.WriteLine();
                                    string[] folderQ = new string[] { $"Rename parent folder from '{tvShowFolderName}' to '{expectedFolderName}'?", "Keep current folder name" };
                                    char[] folderA = new char[] { 'Y', 'N' };
                                    renameFolder = PreBuildTools.GetUserConfirmation(folderQ, folderA, new string[] { });
                                }
                                else renameFolder = PreBuildTools.Setting.AllowImdb;

                                if (renameFolder == true)
                                {
                                    string newParentDirectory = $"{PreBuildTools.GoToParentDirectory(showMetadata.FolderPath)}\\{expectedFolderName}";
                                    PreBuildTools.MoveFileSystemItem(showMetadata.FolderPath, newParentDirectory);
                                    showMetadata.FolderPath = newParentDirectory;
                                    tvShowFolderName = expectedFolderName;
                                    PreBuildTools.WriteLine($"Parent folder renamed successfully to: {expectedFolderName}", ConsoleColor.Green);
                                }
                            }

                            bool? dashBeforeReleaseYear = PreBuildTools.Setting.DashBeforeReleaseYear,
                                allowEpisodeYear = PreBuildTools.Setting.AllowEpisodeYear,
                                useEpisodeReleaseYear = PreBuildTools.Setting.UseEpisodeReleaseYear,
                                dashAfterReleaseYear = PreBuildTools.Setting.DashAfterReleaseYear,
                                allowEpisodeName = PreBuildTools.Setting.AllowEpisodeName,
                                dashAfterSeasonEpisode = PreBuildTools.Setting.DashAfterSeasonEpisode,
                                allowImdb = PreBuildTools.Setting.AllowImdb,
                                dashBeforeImdb = PreBuildTools.Setting.DashBeforeImdb,
                                allowSeasonYear = PreBuildTools.Setting.AllowSeasonYear,
                                EditFiles = !((allowSeasonYear == null && allowEpisodeYear == null) && (allowEpisodeYear == true && useEpisodeReleaseYear == null)
                            && (allowEpisodeYear == true && dashBeforeReleaseYear == null) && (allowEpisodeYear == true && dashAfterReleaseYear == null)
                            && (allowEpisodeName == null) && (dashAfterSeasonEpisode == null) && (allowImdb == null) && (dashBeforeImdb == null));

                            string[] question;
                            char[] charAnswers;

                            if (EditFiles == false)
                            {
                                Console.WriteLine();
                                question = new string[2] { "Would you like to rename the TV show files?", "Cancel operation" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                EditFiles = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                            }

                            if (EditFiles == true)
                            {
                                string[] warnings;

                                // curser 1
                                if (allowSeasonYear == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, add (Release Year) for season folders", "No, don't add (Release Year) for season folders" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    allowSeasonYear = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 2
                                if (allowEpisodeYear == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, add (Release Year) for episode naming", "No, don't add (Release Year) for episode naming" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    allowEpisodeYear = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 3
                                if (allowEpisodeYear == true && useEpisodeReleaseYear == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, use episode release year in naming", "No, use TV show release year in naming" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    useEpisodeReleaseYear = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 4
                                if (allowEpisodeYear == true && dashBeforeReleaseYear == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, add dash '-' before (Release Year)", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    dashBeforeReleaseYear = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 5
                                if (allowEpisodeYear == true && dashAfterReleaseYear == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, add dash '-' between (Release Year) & SxxExx", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    dashAfterReleaseYear = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 6
                                if (allowEpisodeName == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Include episode names", "Exclude episode names" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    allowEpisodeName = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 7
                                if (allowEpisodeName == true && dashAfterSeasonEpisode == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, add dash '-' between SxxExx & Episode Name", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    dashAfterSeasonEpisode = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 8
                                if (allowImdb == null)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Use IMDb IDs in file names", "Don't use IMDb IDs in file names" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    warnings = new string[] { "The IMDb ID is not automated due to anti-bot restrictions", "Existing IMDb IDs in file names will be preserved and used" };
                                    allowImdb = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                // curser 9
                                if (dashBeforeImdb == null || (dashBeforeImdb == false && hasFileImdbIds))
                                {
                                    Console.WriteLine();
                                    if (hasFileImdbIds)
                                        PreBuildTools.WriteLine("IMDb IDs found in files. Would you like to add a dash '-' between Episode Name & IMDb ID?", ConsoleColor.DarkMagenta);

                                    question = new string[2] { "Yes, add dash '-' between Episode Name & IMDb ID", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    dashBeforeImdb = PreBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                Console.WriteLine();
                                question = new string[2] { "Continue renaming", "Cancel renaming" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { "This is your last chance before all files are renamed!" };
                                bool lastChance = PreBuildTools.GetUserConfirmation(question, charAnswers, warnings);

                                if (lastChance)
                                {
                                    ChanceFilesName(showMetadata, EditFiles, dashBeforeReleaseYear, allowEpisodeYear, useEpisodeReleaseYear, dashAfterReleaseYear, allowEpisodeName, dashAfterSeasonEpisode, allowImdb, dashBeforeImdb, allowSeasonYear);
                                }
                                else
                                {
                                    PreBuildTools.WriteLine(OperationCancelled, ConsoleColor.Green);
                                    Ending();
                                    continue;
                                }
                            }
                            else
                            {
                                PreBuildTools.WriteLine(OperationCancelled, ConsoleColor.Green);
                                Ending();
                                continue;
                            }
                        }
                        else
                        {
                            PreBuildTools.WriteLine(OperationCancelled, ConsoleColor.Green);
                            Ending();
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (PreBuildTools.Setting.AllowBeep)
                        Console.Beep();

                    PreBuildTools.WriteLine($"\n\nError: {ex.Message}", ConsoleColor.Red);
                    PreBuildTools.WriteLine("Please try again or type 'Help' for more information.", ConsoleColor.Red);
                }

                Ending();
                continue;
            }

            void Ending()
            {
                // Dev Note: Check dev configuration before issuing sound alert
                if (PreBuildTools.Setting.AllowBeep)
                    Console.Beep();

                PreBuildTools.Write("\n\n\nPress any key to continue...", ConsoleColor.Green);
                Console.ReadKey();
                Console.Clear();
            }
        }

        private class SearchResult
        {
            public string Title { get; set; } = string.Empty;
            public int Year { get; set; }
            public string ImdbId { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }

        private static async Task<List<SearchResult>> SearchTvMazeAsync(string showName)
        {
            var results = new List<SearchResult>();
            string url = $"https://api.tvmaze.com/search/shows?q={Uri.EscapeDataString(showName)}";

            HttpResponseMessage response = await GetWithRateLimitRetryAsync(url);
            if (!response.IsSuccessStatusCode) return results;

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("show", out var showNode))
                {
                    var result = new SearchResult
                    {
                        Title = showNode.TryGetProperty("name", out var n) ? (n.GetString() ?? "Unknown") : "Unknown"
                    };

                    if (showNode.TryGetProperty("type", out var typeNode) && typeNode.ValueKind == JsonValueKind.String)
                    {
                        result.Type = typeNode.GetString() ?? string.Empty;
                    }

                    if (showNode.TryGetProperty("premiered", out var p) && p.ValueKind == JsonValueKind.String)
                    {
                        if (DateTime.TryParse(p.GetString(), out DateTime date))
                            result.Year = date.Year;
                    }

                    if (showNode.TryGetProperty("externals", out var ext) &&
                        ext.TryGetProperty("imdb", out var imdbNode) &&
                        imdbNode.ValueKind == JsonValueKind.String)
                    {
                        result.ImdbId = imdbNode.GetString() ?? string.Empty;
                    }

                    if (!string.IsNullOrEmpty(result.ImdbId))
                        results.Add(result);
                }
            }

            return results;
        }

        private static async Task<List<SearchResult>> SearchTvMazeMultipleAsync(string showName)
        {
            var allResults = new List<SearchResult>();
            var queriesToTry = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            queriesToTry.Add(showName);

            if (showName.EndsWith("s", StringComparison.OrdinalIgnoreCase) && showName.Length > 3)
            {
                queriesToTry.Add(showName.Substring(0, showName.Length - 1));
            }

            if (showName.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
            {
                queriesToTry.Add(showName.Substring(4).Trim());
            }

            foreach (var q in queriesToTry)
            {
                var partialResults = await SearchTvMazeAsync(q);
                foreach (var res in partialResults)
                {
                    if (!allResults.Any(r => r.ImdbId.Equals(res.ImdbId, StringComparison.OrdinalIgnoreCase)))
                    {
                        allResults.Add(res);
                    }
                }
            }

            return allResults.OrderBy(r => r.Year).ToList();
        }

        private static void ChanceFilesName(ShowMediaMetadata showMetadata, bool? EditFiles, bool? dashBeforeReleaseYear, bool? allowEpisodeYear, bool? useEpisodeReleaseYear, bool? dashAfterReleaseYear, bool? allowEpisodeName, bool? dashAfterSeasonEpisode, bool? allowImdb, bool? dashBeforeImdb, bool? allowSeasonYear)
        {
            // 1. Verify primary season year configuration exists
            if (allowSeasonYear == null)
                throw new InvalidOperationException("Configuration missing: 'allow Season Year' cannot be null.");

            // 2. Verify primary episode year configuration exists
            else if (allowEpisodeYear == null)
                throw new InvalidOperationException("Configuration missing: 'allow Episode Year' cannot be null.");

            // 3. Verify sub-setting exists when episode year is enabled
            else if (allowEpisodeYear == true && useEpisodeReleaseYear == null)
                throw new InvalidOperationException("Configuration missing: 'use Episode Release Year' must be set when 'allow Episode Year' is enabled.");

            // 4. Verify formatting rules exist when episode year is enabled
            else if (allowEpisodeYear == true && dashBeforeReleaseYear == null)
                throw new InvalidOperationException("Configuration missing: 'dash Before Release Year' must be set when 'allow Episode Year' is enabled.");

            // 5. Verify formatting rules exist when episode year is enabled
            else if (allowEpisodeYear == true && dashAfterReleaseYear == null)
                throw new InvalidOperationException("Configuration missing: 'dash After Release Year' must be set when 'allow Episode Year' is enabled.");

            // 6. Verify primary episode name configuration exists
            else if (allowEpisodeName == null)
                throw new InvalidOperationException("Configuration missing: 'allow Episode Name' cannot be null.");

            // 7. Prevent execution if episode name is enabled but missing its dependent logic/settings
            else if (allowEpisodeName == true && dashAfterSeasonEpisode == null)
                throw new InvalidOperationException("Incomplete configuration: 'allow Episode Name' is enabled, but its required sub-settings are missing or not implemented.");

            // 8. Verify primary IMDb configuration exists
            else if (allowImdb == null)
                throw new InvalidOperationException("Configuration missing: 'allow IMDb' cannot be null.");

            // 9. Prevent execution if IMDb is enabled but missing its dependent logic/settings
            else if (allowImdb == true)
                throw new InvalidOperationException("Incomplete configuration: 'allow IMDb' is enabled, but its required sub-settings are missing or not implemented.");

            List<FileInfo> files = new List<FileInfo>();
            List<DirectoryInfo> directories = new List<DirectoryInfo>();
            List<string> errorMessages = new List<string>();

            Console.WriteLine();
            PreBuildTools.WriteLine("Renaming files and folders...", ConsoleColor.Green);

            directories.AddRange(new DirectoryInfo(showMetadata.FolderPath).GetDirectories());
            List<(string FolderName, int SeasonNum)> seasonFoldersList = new List<(string, int)>();

            for (int i = 0; i < directories.Count;)
            {
                string folderName = directories[i].Name;
                if (folderName.Equals("Unused Episodes", StringComparison.OrdinalIgnoreCase))
                {
                    directories.RemoveAt(i);
                    continue;
                }

                Match seasonMatch = Regex.Match(folderName, @"(?i)(?:season|series|s)\s*(\d+)");
                if (seasonMatch.Success)
                {
                    int parsedSeasonNum = int.Parse(seasonMatch.Groups[1].Value);
                    int metaSeasonIndex = parsedSeasonNum - 1;

                    if (metaSeasonIndex < 0 || metaSeasonIndex >= showMetadata.Seasons.Count)
                    {
                        // Dev Note: Check dev configuration before issuing sound alert
                        if (PreBuildTools.Setting.AllowBeep)
                            Console.Beep();

                        errorMessages.Add($"[ERROR] Metadata for Season {parsedSeasonNum} not found. Skipping folder.");
                        PreBuildTools.WriteLine($"\n{errorMessages.Last()}", ConsoleColor.Red);
                        directories.RemoveAt(i);
                        continue;
                    }

                    string seasonFolderFormat = $"Season {parsedSeasonNum:D2}";
                    if (allowSeasonYear == true)
                        seasonFolderFormat += $" ({showMetadata.Seasons[metaSeasonIndex].SeasonYear})";
                    seasonFoldersList.Add((seasonFolderFormat, parsedSeasonNum));

                    if (directories[i].FullName.Split("\\").Last() != seasonFolderFormat)
                    {
                        PreBuildTools.MoveFileSystemItem(directories[i].FullName, $"{PreBuildTools.GoToParentDirectory(directories[i].FullName)}\\{seasonFolderFormat}");
                    }

                    i++;
                }
                else
                {
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (PreBuildTools.Setting.AllowBeep)
                        Console.Beep();

                    PreBuildTools.WriteLine($"Warning: Folder name '{folderName}' does not match season format. Skipping.", ConsoleColor.Yellow);
                    directories.RemoveAt(i);
                    continue;
                }
            }

            string unusedFolderPath = $"{showMetadata.FolderPath}\\Unused Episodes";

            for (int i = 0; i < seasonFoldersList.Count; i++)
            {
                List<FileInfo> seasonFiles = new List<FileInfo>();
                string currentFolderName = seasonFoldersList[i].FolderName;
                int currentSeasonNum = seasonFoldersList[i].SeasonNum;
                int metaSeasonIndex = currentSeasonNum - 1;

                seasonFiles.AddRange(new DirectoryInfo($"{showMetadata.FolderPath}\\{currentFolderName}").GetFiles());
                var episodeGroups = new Dictionary<string, List<(FileInfo OriginalFile, string Extension, int EpNum, string ExtractedImdbId)>>();

                PreBuildTools.WriteLine($"\n         Processing files in '{currentFolderName}'...", ConsoleColor.DarkCyan);
                for (int j = 0; j < seasonFiles.Count; j++)
                {
                    string fileName = seasonFiles[j].Name;
                    string fileExtension = fileName.Split('.').Last().ToLower();

                    if (fileExtension != "mkv" && fileExtension != "mp4" && fileExtension != "avi")
                        continue;

                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                    Match fullMatch = Regex.Match(nameWithoutExt, @"(?i)(?:s|season\s*)?(\d+)(?:[ .\-x]+|(?:e|episode\s*|x))(\d+)(?:(?:(?:-|x|e|episode\s*)(?:e|episode\s*)?|[ .]+(?:e|episode\s*|x))(\d+))?");
                    Match epOnlyMatch = Regex.Match(nameWithoutExt, @"(?i)(?:^|[ \-])(?:e|episode\s*)?(\d+)(?:(?:(?:-|x|e|episode\s*)(?:e|episode\s*)?|[ .]+(?:e|episode\s*|x))(\d+))?\b");
                    Match imdbMatch = Regex.Match(fileName, @"(?i)(tt\d{7,10})");

                    string extractedImdbId = imdbMatch.Success ? imdbMatch.Groups[1].Value.ToLower() : string.Empty;

                    bool isMatched = false;
                    int seasonNum = 0, episode1Num = 0, episode2Num = 0;
                    bool hasMultiPart = false;
                    if (fullMatch.Success)
                    {
                        seasonNum = int.Parse(fullMatch.Groups[1].Value);
                        episode1Num = int.Parse(fullMatch.Groups[2].Value);
                        if (fullMatch.Groups[3].Success)
                        {
                            hasMultiPart = true;
                            episode2Num = int.Parse(fullMatch.Groups[3].Value);
                        }
                        isMatched = true;
                    }
                    else if (epOnlyMatch.Success)
                    {
                        seasonNum = currentSeasonNum;
                        episode1Num = int.Parse(epOnlyMatch.Groups[1].Value);
                        if (epOnlyMatch.Groups[2].Success)
                        {
                            hasMultiPart = true;
                            episode2Num = int.Parse(epOnlyMatch.Groups[2].Value);
                        }
                        isMatched = true;
                    }

                    if (isMatched)
                    {
                        string formattedEpisodeString = $"S{seasonNum:D2}E{episode1Num:D2}";
                        if (hasMultiPart)
                            formattedEpisodeString += $"-E{episode2Num:D2}";
                        if (!episodeGroups.ContainsKey(formattedEpisodeString))
                            episodeGroups[formattedEpisodeString] = new List<(FileInfo, string, int, string)>();
                        episodeGroups[formattedEpisodeString].Add((seasonFiles[j], fileExtension, episode1Num, extractedImdbId));
                    }
                }

                foreach (var group in episodeGroups)
                {
                    string epString = group.Key;
                    var filesInGroup = group.Value;
                    int epNum = filesInGroup[0].EpNum;
                    string extractedImdbId = filesInGroup[0].ExtractedImdbId;

                    int epIndex = epNum - 1;
                    if (epIndex < 0 || epIndex >= showMetadata.Seasons[metaSeasonIndex].Episodes.Count)
                    {
                        // Dev Note: Check dev configuration before issuing sound alert
                        if (PreBuildTools.Setting.AllowBeep)
                            Console.Beep();

                        errorMessages.Add($"[ERROR] Skipping '{epString}': Metadata only has {showMetadata.Seasons[metaSeasonIndex].Episodes.Count} episodes for Season {currentSeasonNum}.");
                        PreBuildTools.WriteLine($"\n{errorMessages.Last()}", ConsoleColor.Red);
                        continue;
                    }

                    string baseNewName = showMetadata.ShowTitle;
                    if (allowEpisodeYear == true)
                    {
                        if (useEpisodeReleaseYear == true)
                        {
                            if (dashAfterReleaseYear == true)
                                baseNewName += $" ({showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeYear}) -";
                            else
                                baseNewName += $" ({showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeYear})";
                        }
                        else
                        {
                            if (dashAfterReleaseYear == true)
                                baseNewName += $" ({showMetadata.ShowYear}) -";
                            else
                                baseNewName += $" ({showMetadata.ShowYear})";
                        }
                    }

                    baseNewName += $" {epString}";
                    if (allowEpisodeName == true)
                    {
                        if (dashAfterSeasonEpisode == true)
                            baseNewName += " -";
                        string rawTitle = showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeTitle;
                        string cleanTitle = Regex.Replace(rawTitle, @"[<>:""/\\|?*]", string.Empty);
                        cleanTitle = Regex.Replace(cleanTitle, @"\s{2,}", " ");
                        baseNewName += $" {cleanTitle}";
                    }

                    if (allowImdb == true)
                    {
                        string apiImdbId = showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeImdbId;
                        string finalImdbId = !string.IsNullOrEmpty(apiImdbId) ? apiImdbId : extractedImdbId;

                        if (!string.IsNullOrEmpty(finalImdbId))
                        {
                            if (dashBeforeImdb == true)
                                baseNewName += $" - [imdbid-{finalImdbId}]";
                            else
                                baseNewName += $" [imdbid-{finalImdbId}]";
                        }
                        else
                        {
                            if (dashBeforeImdb == true)
                                baseNewName += " - [imdbid-]";
                            else
                                baseNewName += $" [imdbid-]";
                        }
                    }
                    else if (!string.IsNullOrEmpty(extractedImdbId))
                    {
                        if (dashBeforeImdb == true)
                            baseNewName += $" - [imdbid-{extractedImdbId}]";
                        else
                            baseNewName += $" [imdbid-{extractedImdbId}]";
                    }

                    if (filesInGroup.Count == 1)
                    {
                        var fileData = filesInGroup[0];
                        string finalName = $"{baseNewName}.{fileData.Extension}";

                        if (fileData.OriginalFile.Name != finalName)
                        {
                            PreBuildTools.WriteLine($"Renaming '{fileData.OriginalFile.Name}' to '{finalName}'", ConsoleColor.Green);
                            PreBuildTools.MoveFileSystemItem(fileData.OriginalFile.FullName, $"{PreBuildTools.GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                        }
                    }
                    else
                    {
                        // Dev Note: Check dev configuration before issuing sound alert
                        if (PreBuildTools.Setting.AllowBeep)
                            Console.Beep();

                        PreBuildTools.WriteLine($"\n[ATTENTION] Multiple files detected for {epString}:", ConsoleColor.Yellow);

                        for (int k = 0; k < filesInGroup.Count; k++)
                            PreBuildTools.WriteLine($"  [{k + 1}] {filesInGroup[k].OriginalFile.Name}", ConsoleColor.Green);

                        PreBuildTools.WriteLine("  [A] Keep ALL (Rename as Part 1, Part 2...)", ConsoleColor.Green);
                        PreBuildTools.WriteLine("  [S] Skip ALL (Moves everything to Unused folder)\n", ConsoleColor.Green);

                        string choice;
                        bool isFirstAttempt = true;
                        while (true)
                        {
                            if (!isFirstAttempt)
                            {
                                PreBuildTools.ClearConsoleLines(2);
                            }

                            PreBuildTools.Write("Which one do you want to keep? (Enter number - '1', '2', 'A', or 'S') > ", ConsoleColor.Green);

                            choice = Console.ReadLine()?.Trim().ToUpper() ?? string.Empty;

                            if (choice == "A" || choice == "S")
                                break;
                            if (int.TryParse(choice, out int selectedNum) && selectedNum >= 1 && selectedNum <= filesInGroup.Count)
                                break;

                            // Dev Note: Check dev configuration before issuing sound alert
                            if (PreBuildTools.Setting.AllowBeep)
                                Console.Beep();

                            PreBuildTools.WriteLine("Invalid input. Please enter a valid number from the list ('1', '2', 'A', or 'S').", ConsoleColor.Red);
                            isFirstAttempt = false;
                        }

                        if (choice == "A")
                        {
                            for (int k = 0; k < filesInGroup.Count; k++)
                            {
                                var fileData = filesInGroup[k];
                                string finalName = $"{baseNewName} - Part {k + 1}.{fileData.Extension}";
                                PreBuildTools.MoveFileSystemItem(fileData.OriginalFile.FullName, $"{PreBuildTools.GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                            }
                        }
                        else if (choice == "S")
                        {
                            if (!Directory.Exists(unusedFolderPath)) Directory.CreateDirectory(unusedFolderPath);

                            for (int k = 0; k < filesInGroup.Count; k++)
                            {
                                var fileData = filesInGroup[k];
                                string unusedDestination = $"{unusedFolderPath}\\{fileData.OriginalFile.Name}";
                                PreBuildTools.MoveFileSystemItem(fileData.OriginalFile.FullName, unusedDestination);
                            }
                            PreBuildTools.WriteLine($"Moved all files for {epString} to 'Unused Episodes'.", ConsoleColor.Green);
                        }
                        else
                        {
                            if (!Directory.Exists(unusedFolderPath)) Directory.CreateDirectory(unusedFolderPath);
                            int selectedIdx = int.Parse(choice) - 1;

                            for (int k = 0; k < filesInGroup.Count; k++)
                            {
                                var fileData = filesInGroup[k];
                                if (k == selectedIdx)
                                {
                                    string finalName = $"{baseNewName}.{fileData.Extension}";
                                    PreBuildTools.MoveFileSystemItem(fileData.OriginalFile.FullName, $"{PreBuildTools.GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                                }
                                else
                                {
                                    string unusedDestination = $"{unusedFolderPath}\\{fileData.OriginalFile.Name}";
                                    PreBuildTools.MoveFileSystemItem(fileData.OriginalFile.FullName, unusedDestination);
                                }
                            }

                            PreBuildTools.WriteLine($"Kept option {choice}. Moved remaining files to 'Unused Episodes'.", ConsoleColor.Green);
                        }
                    }
                }
            }

            if (errorMessages.Count > 0)
            {
                Console.WriteLine("\n\n");
                for (int i = 0; i < errorMessages.Count; i++)
                    PreBuildTools.WriteLine($"{i + 1}) {errorMessages[i]}", ConsoleColor.Red);

                // Dev Note: Check dev configuration before issuing sound alert
                if (PreBuildTools.Setting.AllowBeep)
                    Console.Beep();

                PreBuildTools.WriteLine($"\n\nTotal Errors: {errorMessages.Count.ToString("N0", new CultureInfo("de-DE"))}", ConsoleColor.Red);
            }
        }

        private static async Task<ShowMediaMetadata> GetShowAsync(string imdbId, string folderPath = "")
        {
            var show = new ShowMediaMetadata
            {
                ShowImdbId = imdbId,
            };

            string lookupUrl = $"https://api.tvmaze.com/lookup/shows?imdb={imdbId}";
            HttpResponseMessage showResponse = await GetWithRateLimitRetryAsync(lookupUrl);

            if (!showResponse.IsSuccessStatusCode)
            {
                // Dev Note: Check dev configuration before issuing sound alert
                if (PreBuildTools.Setting.AllowBeep)
                    Console.Beep();

                if (showResponse.StatusCode == HttpStatusCode.NotFound)
                    throw new ArgumentException($"No show found for IMDb ID: {imdbId}. Please check the IMDb ID and try again.");
                else
                    throw new ArgumentException($"Failed to fetch data from TVMaze. Status Code: {showResponse.StatusCode}");
            }

            string showJson = await showResponse.Content.ReadAsStringAsync();
            using var showDoc = JsonDocument.Parse(showJson);
            var root = showDoc.RootElement;

            if (root.TryGetProperty("name", out var nameProp))
                show.ShowTitle = nameProp.GetString() ?? string.Empty;

            if (!root.TryGetProperty("id", out var idProp))
                return show;

            int showId = idProp.GetInt32();

            string episodesUrl = $"https://api.tvmaze.com/shows/{showId}/episodes";
            HttpResponseMessage episodesResponse = await GetWithRateLimitRetryAsync(episodesUrl);

            if (!episodesResponse.IsSuccessStatusCode)
                return show;

            string episodesJson = await episodesResponse.Content.ReadAsStringAsync();
            using var episodesDoc = JsonDocument.Parse(episodesJson);
            var episodesArray = episodesDoc.RootElement;

            if (episodesArray.ValueKind != JsonValueKind.Array)
                return show;

            var seasonMap = new Dictionary<int, ShowMediaMetadata.SeasonMediaMetadata>();

            foreach (var ep in episodesArray.EnumerateArray())
            {
                Task.Delay(35).Wait(); // Delay to avoid hitting rate limits

                int tvmazeEpId = ep.GetProperty("id").GetInt32();
                int seasonNum = ep.GetProperty("season").GetInt32();
                int epNum = ep.GetProperty("number").GetInt32();
                string epTitle = ep.GetProperty("name").GetString() ?? string.Empty;

                int epYear = default;
                if (ep.TryGetProperty("airdate", out var airdateProp) &&
                    DateTime.TryParse(airdateProp.GetString(), out var airdate))
                {
                    epYear = airdate.Year;
                }

                string epImdbId = await GetEpisodeImdbIdAsync(tvmazeEpId);

                if (!seasonMap.TryGetValue(seasonNum, out var seasonMeta))
                {
                    seasonMeta = new ShowMediaMetadata.SeasonMediaMetadata
                    {
                        SeasonNumber = seasonNum
                    };
                    seasonMap[seasonNum] = seasonMeta;
                    show.Seasons.Add(seasonMeta);
                }

                seasonMeta.Episodes.Add(new ShowMediaMetadata.SeasonMediaMetadata.EpisodeMediaMetadata
                {
                    EpisodeTitle = epTitle,
                    EpisodeNumber = epNum,
                    EpisodeYear = epYear,
                    EpisodeImdbId = epImdbId
                });
            }

            return show;
        }

        private static async Task<string> GetEpisodeImdbIdAsync(int episodeId)
        {
            try
            {
                string epDetailUrl = $"https://api.tvmaze.com/episodes/{episodeId}";
                HttpResponseMessage response = await GetWithRateLimitRetryAsync(epDetailUrl);
                if (!response.IsSuccessStatusCode) return string.Empty;

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("externals", out var externals) &&
                    externals.TryGetProperty("imdb", out var imdbProp) &&
                    imdbProp.ValueKind == JsonValueKind.String)
                {
                    return imdbProp.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // Fallback on error
                // Dev Note: Check dev configuration before issuing sound alert
                if (PreBuildTools.Setting.AllowBeep)
                    Console.Beep();
            }

            return string.Empty;
        }

        private static async Task<HttpResponseMessage> GetWithRateLimitRetryAsync(string url)
        {
            while (true)
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.StatusCode == (HttpStatusCode)429) // Too Many Requests
                {
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (PreBuildTools.Setting.AllowBeep)
                        Console.Beep();

                    _RateLimitHit = true;

                    int delayMs = 75000;
                    if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue)
                    {
                        delayMs += (int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds * 10;
                    }

                    PreBuildTools.Countdown(delayMs, PreBuildTools.Setting.AllowColors, "Rate limit reached (429). Retrying...");

                    PreBuildTools.Write($"\rRate limit reached (429). Retrying in: 0ms".PadRight(95), ConsoleColor.Yellow);
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (PreBuildTools.Setting.AllowBeep)
                        Console.Beep();

                    continue;
                }

                _RateLimitHit = false;
                return response;
            }
        }

        private static string GetTvShowFolderTvShowName(string getTvShowName)
        {
            string[] parts = getTvShowName.Split(" ");
            string tvShowName = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].First().ToString() == "(")
                    break;
                else
                    tvShowName += parts[i] + " ";
            }
            tvShowName = tvShowName.TrimEnd();
            if (string.IsNullOrEmpty(tvShowName))
                throw new Exception("TV Show Name not found in the folder name.");
            return tvShowName;
        }

        private static string GetTvShowFolderReleaseYear(string getReleaseYear)
        {
            string releaseYear = getReleaseYear.Split("(").Last().Split(")").First();
            if (string.IsNullOrEmpty(releaseYear))
                throw new Exception("Release Year not found in the folder name.");
            if (!int.TryParse(releaseYear, out _))
                throw new Exception("Release Year is not a valid integer.");
            return releaseYear;
        }

        private static string GetTvShowFolderImdbId(string getImdbId)
        {
            string imdbId = getImdbId.Split(" ").Last().Split("-").Last().Split("]").First();
            if (string.IsNullOrEmpty(imdbId))
                throw new Exception("IMDb ID not found in the folder name.");
            return imdbId;
        }

        public static bool CheckAndApplyFileImdbIds(ShowMediaMetadata showMetadata)
        {
            bool imdbFound = false;

            if (string.IsNullOrEmpty(showMetadata.FolderPath) || !Directory.Exists(showMetadata.FolderPath))
            {
                return false;
            }

            var seasonDirectories = Directory.GetDirectories(showMetadata.FolderPath)
                .Where(d => Regex.IsMatch(Path.GetFileName(d), @"(?i)(?:season|series|s)\s*\d+"));

            string[] validExtensions = { ".mkv", ".mp4", ".avi" };

            foreach (var seasonDir in seasonDirectories)
            {
                string folderName = Path.GetFileName(seasonDir);
                Match seasonMatch = Regex.Match(folderName, @"(?i)(?:season|series|s)\s*(\d+)");
                if (!seasonMatch.Success) continue;

                int seasonNum = int.Parse(seasonMatch.Groups[1].Value);
                var seasonMeta = showMetadata.Seasons.FirstOrDefault(s => s.SeasonNumber == seasonNum);
                if (seasonMeta == null) continue;

                var videoFiles = Directory.GetFiles(seasonDir)
                    .Where(f => validExtensions.Contains(Path.GetExtension(f).ToLower()));

                foreach (var filePath in videoFiles)
                {
                    Task.Delay(5).Wait(); // Introduce a small delay to avoid overwhelming the file & encounter error '429' system

                    string fileName = Path.GetFileName(filePath);
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                    Match imdbMatch = Regex.Match(fileName, @"(?i)(tt\d{7,10})");

                    if (imdbMatch.Success)
                    {
                        string extractedImdbId = imdbMatch.Groups[1].Value.ToLower();

                        Match fullMatch = Regex.Match(nameWithoutExt, @"(?i)(?:s|season\s*)?(\d+)(?:[ .\-x]+|(?:e|episode\s*|x))(\d+)(?:(?:(?:-|x|e|episode\s*)(?:e|episode\s*)?|[ .]+(?:e|episode\s*|x))(\d+))?");
                        Match epOnlyMatch = Regex.Match(nameWithoutExt, @"(?i)(?:^|[ \-])(?:e|episode\s*)?(\d+)(?:(?:(?:-|x|e|episode\s*)(?:e|episode\s*)?|[ .]+(?:e|episode\s*|x))(\d+))?\b");

                        int ep1Num = 0;
                        int ep2Num = 0;
                        bool isMatched = false;

                        if (fullMatch.Success)
                        {
                            ep1Num = int.Parse(fullMatch.Groups[2].Value);
                            if (fullMatch.Groups[3].Success)
                            {
                                ep2Num = int.Parse(fullMatch.Groups[3].Value);
                            }
                            isMatched = true;
                        }
                        else if (epOnlyMatch.Success)
                        {
                            ep1Num = int.Parse(epOnlyMatch.Groups[1].Value);
                            if (epOnlyMatch.Groups[2].Success)
                            {
                                ep2Num = int.Parse(epOnlyMatch.Groups[2].Value);
                            }
                            isMatched = true;
                        }

                        if (isMatched)
                        {
                            var primaryEpisode = seasonMeta.Episodes.FirstOrDefault(e => e.EpisodeNumber == ep1Num);
                            if (primaryEpisode != null)
                            {
                                primaryEpisode.EpisodeImdbId = extractedImdbId;
                                imdbFound = true;
                            }

                            if (ep2Num > 0)
                            {
                                var secondaryEpisode = seasonMeta.Episodes.FirstOrDefault(e => e.EpisodeNumber == ep2Num);
                                if (secondaryEpisode != null)
                                {
                                    secondaryEpisode.EpisodeImdbId = extractedImdbId;
                                    imdbFound = true;
                                }
                            }
                        }
                    }
                }
            }

            return imdbFound;
        }

        private static void Help()
        {
            Console.WriteLine();

            PreBuildTools.WriteLine("===============================================================================", ConsoleColor.Cyan);
            PreBuildTools.WriteLine("                            JELLY SOFTWARE - HELP                              ", ConsoleColor.Cyan);
            PreBuildTools.WriteLine("===============================================================================\n", ConsoleColor.Cyan);

            PreBuildTools.WriteLine(">> OVERVIEW", ConsoleColor.Yellow);
            PreBuildTools.WriteLine("  This application fetches metadata for a TV show based on its IMDb ID", ConsoleColor.Green);
            PreBuildTools.WriteLine("  and helps automatically organize and rename your folders and episode files.\n", ConsoleColor.Green);

            PreBuildTools.WriteLine(">> USAGE INSTRUCTIONS", ConsoleColor.Yellow);
            PreBuildTools.WriteLine("  1. When prompted, insert the full directory path of your TV show folder.", ConsoleColor.Green);
            PreBuildTools.WriteLine("  2. To quit the application at any time, type 'Break', 'Stop', or 'Exit'.", ConsoleColor.Green);
            PreBuildTools.WriteLine("  3. To get help with the application at any time, type 'Help'.", ConsoleColor.Green);
            PreBuildTools.WriteLine("  4. To change settings with the application at any time, type 'Setting' or 'Settings'.\n", ConsoleColor.Green);

            PreBuildTools.WriteLine(">> REQUIRED FOLDER FORMAT", ConsoleColor.Yellow);
            PreBuildTools.WriteLine("  The parent folder must contain the Title, Release Year, and IMDb ID.", ConsoleColor.Green);
            PreBuildTools.WriteLine("  Format:  Show Title (Year) [imdbid-ttXXXXXXX]", ConsoleColor.Green);
            PreBuildTools.WriteLine("  Example: Breaking Bad (2008) [imdbid-tt0903747]", ConsoleColor.Green);
            PreBuildTools.WriteLine("  Example: Family Guy (1999) [imdbid-tt0182576]\n", ConsoleColor.Green);

            PreBuildTools.WriteLine(">> FEATURES & BEHAVIOR", ConsoleColor.Yellow);
            PreBuildTools.WriteLine("  * Auto-Mapping: The app fetches data and maps it to your season folders.", ConsoleColor.Green);
            PreBuildTools.WriteLine("  * Multi-Part Episodes: If a season folder has multiple files for the same", ConsoleColor.Green);
            PreBuildTools.WriteLine("    episode number, the process will pause and ask you how to resolve it.", ConsoleColor.Green);
            PreBuildTools.WriteLine("  * IMDb ID Auto-Detection: If an IMDb ID (e.g., 'tt0959621') already exists", ConsoleColor.Green);
            PreBuildTools.WriteLine("    in an episode's file name, the app will automatically detect it and tag", ConsoleColor.Green);
            PreBuildTools.WriteLine("    the newly renamed file with it.", ConsoleColor.Green);
            PreBuildTools.WriteLine("  * At Local path: A new extension file 'json' will be created unless one already exists.", ConsoleColor.Green);

            PreBuildTools.WriteLine(">> TROUBLESHOOTING & NOTES", ConsoleColor.Yellow);
            PreBuildTools.WriteLine("  * If the console appears frozen during processing, try pressing [ENTER].", ConsoleColor.Green);
            PreBuildTools.WriteLine("  * Note: This software has currently not been tested on 'Season 0' (Specials).\n", ConsoleColor.Green);

            PreBuildTools.WriteLine("===============================================================================\n", ConsoleColor.Cyan);
        }

        public class ShowMediaMetadata
        {
            public ShowMediaMetadata()
            {
                Seasons = new List<SeasonMediaMetadata>();
            }

            public List<SeasonMediaMetadata> Seasons { get; set; }
            public string FolderPath { get; set; } = string.Empty;
            public string FolderName
            {
                get
                {
                    string[] strings = FolderPath.Split("\\");
                    if (strings[strings.Length - 1] != string.Empty)
                        return strings[strings.Length - 1];
                    else
                        return strings[strings.Length - 2];
                }
            }
            public string ShowTitle { get; set; } = string.Empty;
            public int ShowYear
            {
                get
                {
                    if (Seasons.Count > 0)
                    {
                        return Seasons.First().SeasonYear;
                    }
                    return default;
                }
            }
            public string ShowImdbId { get; set; } = string.Empty;

            public class SeasonMediaMetadata
            {
                public SeasonMediaMetadata()
                {
                    Episodes = new List<EpisodeMediaMetadata>();
                }

                public List<EpisodeMediaMetadata> Episodes { get; set; }
                public int SeasonYear
                {
                    get
                    {
                        if (Episodes.Count > 0)
                        {
                            return Episodes.First().EpisodeYear;
                        }
                        return default;
                    }
                }
                public int SeasonNumber { get; set; } = default;

                public class EpisodeMediaMetadata
                {
                    public EpisodeMediaMetadata() { }

                    public string EpisodeTitle { get; set; } = string.Empty;
                    public int EpisodeYear { get; set; } = default;
                    public string EpisodeImdbId { get; set; } = string.Empty;
                    public int EpisodeNumber { get; set; } = default;
                }
            }
        }

        public class TxtFile
        {
            public static readonly string WelcomeMessage = "Version 1.0.7.1\nType 'Help' for more information.\nType 'Settings' to modify application settings.\nInsert TV Show Folder Path:";
        }
    }
}
