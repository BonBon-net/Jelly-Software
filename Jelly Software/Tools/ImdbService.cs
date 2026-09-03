using Jelly_Software.AppSettings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
                        Console.WriteLine("A");
                        Ending();
                        _InvalidOperation = false;
                    }

                    preBuildTools.WriteLineGreen(TxtFile.WelcomeMessage);
                    preBuildTools.WriteColored("> ", ConsoleColor.Green);

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
                        Settings(folderPath);
                        continue; // Prevents falling through to Ending()
                    }
                    else if (folderPath.ToLower() == "break" || folderPath.ToLower() == "stop" || folderPath.ToLower() == "exit")
                    {
                        preBuildTools.WriteLineGreen("Exiting the application...");
                        break;
                    }
                    else
                    {
                        if (!Directory.Exists(folderPath))
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (ProgramSettings.AllowBeep)
                                Console.Beep();
                            preBuildTools.WriteLineColored("Error: The folder path does not exist.\nPlease check the path and try again.", ConsoleColor.Red);
                            Ending();
                            continue;
                        }
                        if (!Directory.GetDirectories(folderPath).Any(d => Regex.IsMatch(d.Split("\\").Last(), @"(?i)(?:season|series|s)\s*\d+")))
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (ProgramSettings.AllowBeep)
                                Console.Beep();
                            preBuildTools.WriteLineColored("[ERROR] No valid season folders found.\nExpected folder format: 'Season 01', 'Series 1', or 'S01'.", ConsoleColor.Red);
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
                            if (ProgramSettings.AllowBeep)
                                Console.Beep();
                            preBuildTools.WriteLineColored("[ERROR] No valid episode files (.mkv, .mp4, .avi) found inside the season folders.", ConsoleColor.Red);
                            Ending();
                            continue;
                        }

                        folderPath = $"{preBuildTools.GoToParentDirectory(folderPath)}\\{preBuildTools.SanitizeFilename(folderPath.Split("\\").Last())}";

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
                            if (ProgramSettings.AllowBeep)
                                Console.Beep();
                            preBuildTools.WriteLineColored($"\n[WARNING] Placeholder IMDb ID '{imdbId}' detected. Ignoring IMDb ID and searching database...", ConsoleColor.Yellow);
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
                                    if (ProgramSettings.AllowBeep)
                                        Console.Beep();
                                    preBuildTools.WriteLineColored($"\n[WARNING] Folder name '{tvShowName} ({releaseYear})' doesn't perfectly match fetched data: '{initialShowMetadata.ShowTitle} ({initialShowMetadata.ShowYear})'.", ConsoleColor.Yellow);
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
                                if (ProgramSettings.AllowBeep)
                                    Console.Beep();
                                preBuildTools.WriteLineColored($"\n[WARNING] Could not automatically pull exact match for IMDb ID: {imdbId}.", ConsoleColor.Yellow);
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
                            preBuildTools.WriteLineGreen($"Searching database for variations of '{cleanTvShowName}'...");

                            var rawSearchResults = await SearchTvMazeMultipleAsync(cleanTvShowName);

                            foreach (var res in rawSearchResults)
                            {
                                if (res.Type.Equals("TV Movie", StringComparison.OrdinalIgnoreCase) || res.Type.Equals("Movie", StringComparison.OrdinalIgnoreCase))
                                {
                                    // Dev Note: Check dev configuration before issuing sound alert
                                    if (ProgramSettings.AllowBeep)
                                        Console.Beep();
                                    preBuildTools.WriteLineColored($"\n[WARNING] '{res.Title} ({res.Year})' is classified as a movie ({res.Type}), skipping from TV show selection list.", ConsoleColor.Yellow);
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
                                if (ProgramSettings.AllowBeep)
                                    Console.Beep();
                                preBuildTools.WriteLineGreen("No alternative TV shows found on TVMaze.");
                                string[] manualQ = new string[] { "Would you like to manually enter a correct IMDb ID?", "Cancel operation" };
                                char[] manualA = new char[] { 'Y', 'N' };
                                bool wantManual = preBuildTools.GetUserConfirmation(manualQ, manualA, new string[] { });

                                if (wantManual)
                                {
                                    preBuildTools.WriteColored("Enter the correct IMDb ID (e.g., tt0182576) > ", ConsoleColor.Green);

                                    string manualImdb = Console.ReadLine()?.Trim() ?? string.Empty;

                                    if (!string.IsNullOrEmpty(manualImdb) && manualImdb.StartsWith("tt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        imdbId = manualImdb;
                                        showMetadata = await GetShowAsync(imdbId, tvShowFolderName);
                                    }
                                    else
                                    {
                                        // Dev Note: Check dev configuration before issuing sound alert
                                        if (ProgramSettings.AllowBeep)
                                            Console.Beep();

                                        preBuildTools.WriteLineColored("Invalid IMDb ID format. Canceling operation.", ConsoleColor.Red);
                                        Ending();
                                        continue;
                                    }
                                }
                                else
                                {
                                    // Dev Note: Check dev configuration before issuing sound alert
                                    if (ProgramSettings.AllowBeep)
                                        Console.Beep();

                                    preBuildTools.WriteLineGreen(OperationCancelled);
                                    Ending();
                                    continue;
                                }
                            }
                            else
                            {
                                // Dev Note: Check dev configuration before issuing sound alert
                                if (ProgramSettings.AllowBeep)
                                    Console.Beep();

                                preBuildTools.WriteLineGreen("\nFound multiple possibilities. Please confirm which series this is:");
                                for (int i = 0; i < searchResults.Count; i++)
                                {
                                    preBuildTools.WriteLineGreen($"  [{i + 1}] {searchResults[i].Title} ({searchResults[i].Year}) - IMDb ID: {searchResults[i].ImdbId}");
                                }
                                preBuildTools.WriteLineGreen("  [0] None of these (Cancel)");

                                int selectedIdx = -1;
                                while (true)
                                {
                                    preBuildTools.WriteColored("\nEnter the number of the correct show > ", ConsoleColor.Green);

                                    string choice = Console.ReadLine()?.Trim() ?? string.Empty;

                                    if (int.TryParse(choice, out selectedIdx) && selectedIdx >= 0 && selectedIdx <= searchResults.Count)
                                    {
                                        break;
                                    }

                                    // Dev Note: Check dev configuration before issuing sound alert
                                    if (ProgramSettings.AllowBeep)
                                        Console.Beep();

                                    preBuildTools.WriteLineColored("Invalid input. Please enter a valid number from the list.", ConsoleColor.Red);
                                }

                                if (selectedIdx == 0)
                                {
                                    preBuildTools.WriteLineGreen(OperationCancelled);
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
                                    bool replaceImdb = preBuildTools.GetUserConfirmation(repQ, repA, new string[] { });

                                    if (replaceImdb)
                                    {
                                        imdbId = newImdbId;
                                    }
                                    else
                                    {
                                        preBuildTools.WriteLineGreen(OperationCancelled);
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
                            if (ProgramSettings.AllowBeep)
                                Console.Beep();

                            preBuildTools.WriteLineGreen("\n[INFO] Found IMDb IDs in video file names and mapped them to corresponding episodes.");
                        }

                        Console.WriteLine();
                        string[] confirmQ = new string[] { $"Is this the correct TV Show: {showMetadata.ShowTitle} ({showMetadata.ShowYear})?", "Cancel operation" };
                        char[] confirmA = new char[] { 'Y', 'N' };
                        bool confirmShow = preBuildTools.GetUserConfirmation(confirmQ, confirmA, new string[] { });

                        if (confirmShow)
                        {
                            preBuildTools.WriteLineGreen($"\nFetching metadata for IMDb ID: {imdbId} and folder: {tvShowFolderName}...\n");

                            showMetadata.FolderPath = folderPath;
                            if (_RateLimitHit)
                            {
                                preBuildTools.WriteLineGreen($"\n\n\n\nShow Title: {showMetadata.ShowTitle}");
                            }
                            else
                            {
                                preBuildTools.WriteLineGreen($"\nShow Title: {showMetadata.ShowTitle}");
                            }

                            preBuildTools.WriteLineGreen($"Show Year: {showMetadata.ShowYear}");
                            preBuildTools.WriteLineGreen($"IMDb ID: {showMetadata.ShowImdbId}");
                            preBuildTools.WriteLineGreen($"Folder Path: {showMetadata.FolderName}");
                            preBuildTools.WriteLineGreen("Seasons and Episodes:");

                            foreach (var season in showMetadata.Seasons)
                            {
                                preBuildTools.WriteLineGreen($"\nSeason {season.SeasonNumber} ({season.SeasonYear}):");
                                foreach (var episode in season.Episodes)
                                {
                                    preBuildTools.WriteLineGreen($"  Episode {episode.EpisodeNumber}: {episode.EpisodeTitle} ({episode.EpisodeYear}) - IMDb ID: {episode.EpisodeImdbId}");
                                }
                            }

                            preBuildTools.WriteLineGreen("\nMetadata fetched successfully!");
                            Console.WriteLine();

                            string safeShowTitle = string.Concat(showMetadata.ShowTitle.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
                            string expectedFolderName = $"{safeShowTitle} ({showMetadata.ShowYear}) [imdbid-{showMetadata.ShowImdbId}]";

                            if (tvShowFolderName != expectedFolderName)
                            {
                                Console.WriteLine();
                                string[] folderQ = new string[]
                                {
                                    $"Rename parent folder from '{tvShowFolderName}' to '{expectedFolderName}'?",
                                    "Keep current folder name"
                                };
                                char[] folderA = new char[] { 'Y', 'N' };
                                bool renameFolder = preBuildTools.GetUserConfirmation(folderQ, folderA, new string[] { });

                                if (renameFolder)
                                {
                                    string newParentDirectory = $"{preBuildTools.GoToParentDirectory(showMetadata.FolderPath)}\\{expectedFolderName}";
                                    preBuildTools.RenameFileOrFolder(showMetadata.FolderPath, newParentDirectory);
                                    showMetadata.FolderPath = newParentDirectory;
                                    tvShowFolderName = expectedFolderName;
                                    preBuildTools.WriteLineGreen($"Parent folder renamed successfully to: {expectedFolderName}");
                                }
                            }

                            Console.WriteLine();
                            string[] question = new string[2] { "Would you like to rename the TV show files?", "Cancel operation" };
                            char[] charAnswers = new char[2] { 'Y', 'N' };
                            bool EditFiles = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });

                            if (EditFiles)
                            {
                                bool UseEpisodeReleaseYear = false, dashAfterReleaseYear = false, AllowEpisodeName = false, dashAfterSeasonEpisode = false, AllowImdb = false, dashBeforeImdb = false, AllowSeasonYear = false, AllowEpisodeYear = false;

                                Console.WriteLine();
                                question = new string[2] { "Yes, add (Release Year) for season folders", "No, don't add (Release Year) for season folders" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                AllowSeasonYear = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });

                                Console.WriteLine();
                                question = new string[2] { "Yes, add (Release Year) for episode naming", "No, don't add (Release Year) for episode naming" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                AllowEpisodeYear = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });

                                if (AllowEpisodeYear)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Use episode release year in naming", "Use TV show release year in naming" };
                                    charAnswers = new char[2] { '1', '2' };
                                    UseEpisodeReleaseYear = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                Console.WriteLine();
                                question = new string[2] { "Yes, add dash '-' between (Release Year) & SxxExx", "No, don't add dash '-'" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                dashAfterReleaseYear = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });

                                Console.WriteLine();
                                question = new string[2] { "Include episode names", "Exclude episode names" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                AllowEpisodeName = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });

                                if (AllowEpisodeName)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, add dash '-' between SxxExx & Episode Name", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    dashAfterSeasonEpisode = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                Console.WriteLine();
                                question = new string[2] { "Use IMDb IDs in file names", "Don't use IMDb IDs in file names" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                string[] warnings = new string[] { "The IMDb ID is not automated due to anti-bot restrictions", "Existing IMDb IDs in file names will be preserved and used" };
                                AllowImdb = preBuildTools.GetUserConfirmation(question, charAnswers, warnings);

                                if (AllowImdb || hasFileImdbIds)
                                {
                                    Console.WriteLine();
                                    if (hasFileImdbIds)
                                        preBuildTools.WriteLineGreen("IMDb IDs found in files. Would you like to add a dash '-' between Episode Name & IMDb ID?");

                                    question = new string[2] { "Yes, add dash '-' between Episode Name & IMDb ID", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    dashBeforeImdb = preBuildTools.GetUserConfirmation(question, charAnswers, new string[] { });
                                }

                                Console.WriteLine();
                                question = new string[2] { "Continue renaming", "Cancel renaming" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { "This is your last chance before all files are renamed!" };
                                bool lastChance = preBuildTools.GetUserConfirmation(question, charAnswers, warnings);

                                if (lastChance)
                                {
                                    ChanceFilesName(showMetadata, EditFiles, UseEpisodeReleaseYear, AllowEpisodeName, AllowImdb, dashAfterReleaseYear, dashAfterSeasonEpisode, dashBeforeImdb, AllowSeasonYear, AllowEpisodeYear);
                                }
                                else
                                {
                                    preBuildTools.WriteLineGreen(OperationCancelled);
                                    Ending();
                                    continue;
                                }
                            }
                            else
                            {
                                preBuildTools.WriteLineGreen(OperationCancelled);
                                Ending();
                                continue;
                            }
                        }
                        else
                        {
                            preBuildTools.WriteLineGreen(OperationCancelled);
                            Ending();
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (ProgramSettings.AllowBeep)
                        Console.Beep();

                    preBuildTools.WriteLineColored($"\n\nError: {ex.Message}", ConsoleColor.Red);
                    preBuildTools.WriteLineColored("Please try again or type 'Help' for more information.", ConsoleColor.Red);
                }

                Ending();
                continue;
            }

            void Ending()
            {
                // Dev Note: Check dev configuration before issuing sound alert
                if (ProgramSettings.AllowBeep)
                    Console.Beep();

                preBuildTools.WriteColored("\n\n\nPress any key to continue...", ConsoleColor.Green);
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

        private static void ChanceFilesName(ShowMediaMetadata showMetadata, bool EditFiles, bool UseEpisodeReleaseYear, bool AllowEpisodeName, bool AllowImdb, bool dashAfterReleaseYear, bool dashAfterSeasonEpisode, bool dashAfterImdb, bool AllowSeasonYear, bool AllowEpisodeYear)
        {
            List<FileInfo> files = new List<FileInfo>();
            List<DirectoryInfo> directories = new List<DirectoryInfo>();
            List<string> errorMessages = new List<string>();

            Console.WriteLine();
            preBuildTools.WriteLineGreen("Renaming files and folders...");

            directories.AddRange(new DirectoryInfo(showMetadata.FolderPath).GetDirectories());
            List<(string FolderName, int SeasonNum)> seasonFoldersList = new List<(string, int)>();
            int seasonChangedCount = 0;
            int seasonSkippedCount = 0;
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
                        if (ProgramSettings.AllowBeep)
                            Console.Beep();

                        errorMessages.Add($"[ERROR] Metadata for Season {parsedSeasonNum} not found. Skipping folder.");
                        preBuildTools.WriteLineColored($"\n{errorMessages.Last()}", ConsoleColor.Red);
                        directories.RemoveAt(i);
                        seasonSkippedCount++;
                        continue;
                    }

                    string seasonFolderFormat = $"Season {parsedSeasonNum:D2}";
                    if (AllowSeasonYear)
                        seasonFolderFormat += $" ({showMetadata.Seasons[metaSeasonIndex].SeasonYear})";
                    seasonFoldersList.Add((seasonFolderFormat, parsedSeasonNum));

                    if (directories[i].FullName.Split("\\").Last() != seasonFolderFormat)
                    {
                        seasonChangedCount++;
                        preBuildTools.RenameFileOrFolder(directories[i].FullName, $"{preBuildTools.GoToParentDirectory(directories[i].FullName)}\\{seasonFolderFormat}");
                    }

                    i++;
                }
                else
                {
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (ProgramSettings.AllowBeep)
                        Console.Beep();

                    preBuildTools.WriteLineColored($"Warning: Folder name '{folderName}' does not match season format. Skipping.", ConsoleColor.Yellow);
                    directories.RemoveAt(i);
                    seasonSkippedCount++;
                    continue;
                }
            }

            preBuildTools.WriteLineGreen($"\nTotal Folders: {seasonFoldersList.Count + seasonSkippedCount}\nTotal Seasons: {seasonFoldersList.Count}\nChanged: {seasonChangedCount}\nSkipped: {seasonSkippedCount}");
            int episodeSkippedCount = 0;
            string unusedFolderPath = $"{showMetadata.FolderPath}\\Unused Episodes";

            for (int i = 0; i < seasonFoldersList.Count; i++)
            {
                List<FileInfo> seasonFiles = new List<FileInfo>();
                string currentFolderName = seasonFoldersList[i].FolderName;
                int currentSeasonNum = seasonFoldersList[i].SeasonNum;
                int metaSeasonIndex = currentSeasonNum - 1;

                seasonFiles.AddRange(new DirectoryInfo($"{showMetadata.FolderPath}\\{currentFolderName}").GetFiles());
                var episodeGroups = new Dictionary<string, List<(FileInfo OriginalFile, string Extension, int EpNum, string ExtractedImdbId)>>();

                Console.WriteLine();
                for (int j = 0; j < seasonFiles.Count; j++)
                {
                    string fileName = seasonFiles[j].Name;
                    string fileExtension = fileName.Split('.').Last().ToLower();

                    if (fileExtension != "mkv" && fileExtension != "mp4" && fileExtension != "avi")
                    {
                        episodeSkippedCount++;
                        continue;
                    }

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
                    else
                    {
                        episodeSkippedCount++;
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
                        if (ProgramSettings.AllowBeep)
                            Console.Beep();

                        errorMessages.Add($"[ERROR] Skipping '{epString}': Metadata only has {showMetadata.Seasons[metaSeasonIndex].Episodes.Count} episodes for Season {currentSeasonNum}.");
                        preBuildTools.WriteLineColored($"\n{errorMessages.Last()}", ConsoleColor.Red);
                        episodeSkippedCount += filesInGroup.Count;
                        continue;
                    }

                    string baseNewName = showMetadata.ShowTitle;
                    if (AllowEpisodeYear)
                    {
                        if (UseEpisodeReleaseYear)
                        {
                            if (dashAfterReleaseYear)
                                baseNewName += $" ({showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeYear}) -";
                            else
                                baseNewName += $" ({showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeYear})";
                        }
                        else
                        {
                            if (dashAfterReleaseYear)
                                baseNewName += $" ({showMetadata.ShowYear}) -";
                            else
                                baseNewName += $" ({showMetadata.ShowYear})";
                        }
                    }

                    baseNewName += $" {epString}";
                    if (AllowEpisodeName)
                    {
                        if (dashAfterSeasonEpisode)
                            baseNewName += " -";
                        string rawTitle = showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeTitle;
                        string cleanTitle = Regex.Replace(rawTitle, @"[<>:""/\\|?*]", string.Empty);
                        cleanTitle = Regex.Replace(cleanTitle, @"\s{2,}", " ");
                        baseNewName += $" {cleanTitle}";
                    }

                    if (AllowImdb)
                    {
                        string apiImdbId = showMetadata.Seasons[metaSeasonIndex].Episodes[epIndex].EpisodeImdbId;
                        string finalImdbId = !string.IsNullOrEmpty(apiImdbId) ? apiImdbId : extractedImdbId;

                        if (!string.IsNullOrEmpty(finalImdbId))
                        {
                            if (dashAfterImdb)
                                baseNewName += $" - [imdbid-{finalImdbId}]";
                            else
                                baseNewName += $" [imdbid-{finalImdbId}]";
                        }
                        else
                        {
                            if (dashAfterImdb)
                                baseNewName += " - [imdbid-]";
                            else
                                baseNewName += $" [imdbid-]";
                        }
                    }
                    else if (!string.IsNullOrEmpty(extractedImdbId))
                    {
                        if (dashAfterImdb)
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
                            preBuildTools.WriteLineGreen($"Renaming '{fileData.OriginalFile.Name}' to '{finalName}'");
                            preBuildTools.RenameFileOrFolder(fileData.OriginalFile.FullName, $"{preBuildTools.GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                        }
                    }
                    else
                    {
                        // Dev Note: Check dev configuration before issuing sound alert
                        if (ProgramSettings.AllowBeep)
                            Console.Beep();

                        preBuildTools.WriteLineColored($"\n[ATTENTION] Multiple files detected for {epString}:", ConsoleColor.Yellow);

                        for (int k = 0; k < filesInGroup.Count; k++)
                            preBuildTools.WriteLineGreen($"  [{k + 1}] {filesInGroup[k].OriginalFile.Name}");

                        preBuildTools.WriteLineGreen("  [A] Keep ALL (Rename as Part 1, Part 2...)");
                        preBuildTools.WriteLineGreen("  [S] Skip ALL (Moves everything to Unused folder)\n");

                        string choice;
                        bool isFirstAttempt = true;
                        while (true)
                        {
                            if (!isFirstAttempt)
                            {
                                preBuildTools.ClearConsoleLines(2);
                            }

                            preBuildTools.WriteColored("Which one do you want to keep? (Enter number - '1', '2', 'A', or 'S') > ", ConsoleColor.Green);

                            choice = Console.ReadLine()?.Trim().ToUpper() ?? string.Empty;

                            if (choice == "A" || choice == "S")
                                break;
                            if (int.TryParse(choice, out int selectedNum) && selectedNum >= 1 && selectedNum <= filesInGroup.Count)
                                break;

                            // Dev Note: Check dev configuration before issuing sound alert
                            if (ProgramSettings.AllowBeep)
                                Console.Beep();

                            preBuildTools.WriteLineColored("Invalid input. Please enter a valid number from the list ('1', '2', 'A', or 'S').", ConsoleColor.Red);
                            isFirstAttempt = false;
                        }

                        if (choice == "A")
                        {
                            for (int k = 0; k < filesInGroup.Count; k++)
                            {
                                var fileData = filesInGroup[k];
                                string finalName = $"{baseNewName} - Part {k + 1}.{fileData.Extension}";
                                preBuildTools.RenameFileOrFolder(fileData.OriginalFile.FullName, $"{preBuildTools.GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                            }
                        }
                        else if (choice == "S")
                        {
                            if (!Directory.Exists(unusedFolderPath)) Directory.CreateDirectory(unusedFolderPath);

                            for (int k = 0; k < filesInGroup.Count; k++)
                            {
                                var fileData = filesInGroup[k];
                                string unusedDestination = $"{unusedFolderPath}\\{fileData.OriginalFile.Name}";
                                preBuildTools.RenameFileOrFolder(fileData.OriginalFile.FullName, unusedDestination);
                            }
                            episodeSkippedCount += filesInGroup.Count;
                            preBuildTools.WriteLineGreen($"Moved all files for {epString} to 'Unused Episodes'.");
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
                                    preBuildTools.RenameFileOrFolder(fileData.OriginalFile.FullName, $"{preBuildTools.GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                                }
                                else
                                {
                                    string unusedDestination = $"{unusedFolderPath}\\{fileData.OriginalFile.Name}";
                                    preBuildTools.RenameFileOrFolder(fileData.OriginalFile.FullName, unusedDestination);
                                }
                            }

                            episodeSkippedCount += (filesInGroup.Count - 1);
                            preBuildTools.WriteLineGreen($"Kept option {choice}. Moved remaining files to 'Unused Episodes'.");
                        }
                    }
                }
            }

            if (errorMessages.Count > 0)
            {
                Console.WriteLine("\n\n");
                for (int i = 0; i < errorMessages.Count; i++)
                    preBuildTools.WriteLineColored($"{i + 1}) {errorMessages[i]}", ConsoleColor.Red);

                // Dev Note: Check dev configuration before issuing sound alert
                if (ProgramSettings.AllowBeep)
                    Console.Beep();

                preBuildTools.WriteLineColored($"\n\nTotal Errors: {errorMessages.Count.ToString("N0", new CultureInfo("de-DE"))}", ConsoleColor.Red);
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
                if (ProgramSettings.AllowBeep)
                    Console.Beep();

                if (showResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ArgumentException($"No show found for IMDb ID: {imdbId}. Please check the IMDb ID and try again.");
                }
                else
                {
                    throw new ArgumentException($"Failed to fetch data from TVMaze. Status Code: {showResponse.StatusCode}");
                }
            }

            string showJson = await showResponse.Content.ReadAsStringAsync();
            using var showDoc = JsonDocument.Parse(showJson);
            var root = showDoc.RootElement;

            if (root.TryGetProperty("name", out var nameProp))
            {
                show.ShowTitle = nameProp.GetString() ?? string.Empty;
            }

            if (!root.TryGetProperty("id", out var idProp))
            {
                return show;
            }

            int showId = idProp.GetInt32();

            string episodesUrl = $"https://api.tvmaze.com/shows/{showId}/episodes";
            HttpResponseMessage episodesResponse = await GetWithRateLimitRetryAsync(episodesUrl);

            if (!episodesResponse.IsSuccessStatusCode)
            {
                return show;
            }

            string episodesJson = await episodesResponse.Content.ReadAsStringAsync();
            using var episodesDoc = JsonDocument.Parse(episodesJson);
            var episodesArray = episodesDoc.RootElement;

            if (episodesArray.ValueKind != JsonValueKind.Array)
            {
                return show;
            }

            var seasonMap = new Dictionary<int, ShowMediaMetadata.SeasonMediaMetadata>();

            foreach (var ep in episodesArray.EnumerateArray())
            {
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
                if (ProgramSettings.AllowBeep)
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
                    if (ProgramSettings.AllowBeep)
                        Console.Beep();

                    _RateLimitHit = true;

                    int delayMs = 65000;
                    if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue)
                    {
                        delayMs += (int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds * 5;
                    }

                    DateTime targetTime = DateTime.UtcNow.AddMilliseconds(delayMs);

                    while (true)
                    {
                        TimeSpan remaining = targetTime - DateTime.UtcNow;

                        if (remaining.TotalMilliseconds <= 0)
                            break;

                        string formattedTime = "";
                        if (remaining.Days > 0) formattedTime += $"{remaining.Days}d ";
                        if (remaining.Days > 0 || remaining.Hours > 0) formattedTime += $"{remaining.Hours}h ";
                        if (remaining.Days > 0 || remaining.Hours > 0 || remaining.Minutes > 0) formattedTime += $"{remaining.Minutes}m ";
                        if (remaining.Days > 0 || remaining.Hours > 0 || remaining.Minutes > 0 || remaining.Seconds > 0) formattedTime += $"{remaining.Seconds}s ";
                        formattedTime += $"{remaining.Milliseconds}ms";

                        preBuildTools.WriteColored($"\rRate limit reached (429). Retrying in: {formattedTime}".PadRight(95), ConsoleColor.Yellow);

                        await Task.Delay(15);
                    }

                    preBuildTools.WriteColored($"\rRate limit reached (429). Retrying in: 0ms".PadRight(95), ConsoleColor.Yellow);
                    // Dev Note: Check dev configuration before issuing sound alert
                    if (ProgramSettings.AllowBeep)
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

            preBuildTools.WriteLineColored("===============================================================================", ConsoleColor.Cyan);
            preBuildTools.WriteLineColored("                            JELLY SOFTWARE - HELP                              ", ConsoleColor.Cyan);
            preBuildTools.WriteLineColored("===============================================================================\n", ConsoleColor.Cyan);

            preBuildTools.WriteLineColored(">> OVERVIEW", ConsoleColor.Yellow);
            preBuildTools.WriteLineGreen("  This application fetches metadata for a TV show based on its IMDb ID");
            preBuildTools.WriteLineGreen("  and helps automatically organize and rename your folders and episode files.\n");

            preBuildTools.WriteLineColored(">> USAGE INSTRUCTIONS", ConsoleColor.Yellow);
            preBuildTools.WriteLineGreen("  1. When prompted, insert the full directory path of your TV show folder.");
            preBuildTools.WriteLineGreen("  2. To quit the application at any time, type 'Break', 'Stop', or 'Exit'.");
            preBuildTools.WriteLineGreen("  3. To get help with the application at any time, type 'Help'.");
            preBuildTools.WriteLineGreen("  4. To change settings with the application at any time, type 'Setting' or 'Settings'.\n");

            preBuildTools.WriteLineColored(">> REQUIRED FOLDER FORMAT", ConsoleColor.Yellow);
            preBuildTools.WriteLineGreen("  The parent folder must contain the Title, Release Year, and IMDb ID.");
            preBuildTools.WriteLineGreen("  Format:  Show Title (Year) [imdbid-ttXXXXXXX]");
            preBuildTools.WriteLineGreen("  Example: Breaking Bad (2008) [imdbid-tt0903747]");
            preBuildTools.WriteLineGreen("  Example: Family Guy (1999) [imdbid-tt0182576]\n");

            preBuildTools.WriteLineColored(">> FEATURES & BEHAVIOR", ConsoleColor.Yellow);
            preBuildTools.WriteLineGreen("  * Auto-Mapping: The app fetches data and maps it to your season folders.");
            preBuildTools.WriteLineGreen("  * Multi-Part Episodes: If a season folder has multiple files for the same");
            preBuildTools.WriteLineGreen("    episode number, the process will pause and ask you how to resolve it.");
            preBuildTools.WriteLineGreen("  * IMDb ID Auto-Detection: If an IMDb ID (e.g., 'tt0959621') already exists");
            preBuildTools.WriteLineGreen("    in an episode's file name, the app will automatically detect it and tag");
            preBuildTools.WriteLineGreen("    the newly renamed file with it.");
            preBuildTools.WriteLineGreen("  * At Local path: A new extension file 'json' will be created unless one already exists.");

            preBuildTools.WriteLineColored(">> TROUBLESHOOTING & NOTES", ConsoleColor.Yellow);
            preBuildTools.WriteLineGreen("  * If the console appears frozen during processing, try pressing [ENTER].");
            preBuildTools.WriteLineGreen("  * Note: This software has currently not been tested on 'Season 0' (Specials).\n");

            preBuildTools.WriteLineColored("===============================================================================\n", ConsoleColor.Cyan);
        }

        private static void Settings(string input)
        {
            Console.CursorVisible = false;

            int settingsCursor = 1;
            int settingsPage = 1;

            // Updated count to account for AllowColors and AllowBeep
            int totalSettings = 2;
            int totalSettingsPages = (totalSettings + 9) / 10;

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
                }
                else if (KEY.Key == ConsoleKey.D || KEY.Key == ConsoleKey.RightArrow)
                {
                    settingsPage++;
                    settingsCursor = 1;
                    if (settingsPage > totalSettingsPages)
                        settingsPage = 1;
                }
                else if (KEY.Key == ConsoleKey.Enter)
                {
                    settingSwitch();

                    // Dev Note: Check dev configuration before issuing sound alert
                    if (ProgramSettings.AllowBeep)
                        Console.Beep();
                }
                else if (KEY.Key == ConsoleKey.Backspace || KEY.Key.ToString() == ConsoleKey.Escape.ToString())
                {
                    break;
                }
            }

            Console.CursorVisible = true;

            void writeSETTINGS()
            {
                preBuildTools.WriteLineColored($"\n'W' or '^' UP -- 'S' or 'v' DOWN -- 'A' or '<' Page Left -- 'D' or '>' Page Right\n'[ENTER]' or '[NUM PAD ENTER]' select -- '[ESCAPE]' or '[BACKSPACE]' Exit\n\n============================== SETTINGS ({settingsPage}/{totalSettingsPages}) ==============================\n", ConsoleColor.Cyan);

                if (settingsPage == 1)
                {
                    curser(1);
                    preBuildTools.WriteLineGreen($"{(ProgramSettings.AllowColors ? "Enabled " : "Disabled")} | Allow Colors in Output");

                    curser(2);
                    preBuildTools.WriteLineGreen($"{(ProgramSettings.AllowBeep ? "Enabled " : "Disabled")} | Allow Beep Sound Alerts");
                }

                preBuildTools.WriteLineColored("\n===============================================================================", ConsoleColor.Cyan);
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
                    {
                        ProgramSettings.AllowColors = !ProgramSettings.AllowColors;
                    }
                    else if (settingsCursor == 2)
                    {
                        ProgramSettings.AllowBeep = !ProgramSettings.AllowBeep;
                    }
                }

                _AppSettings.ProgramSettings.Save();
            }
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
            public static readonly string WelcomeMessage = "Version 1.0.6\nType 'Help' for more information.\nType 'Settings' to modify application settings.\nInsert TV Show Folder Path:";
        }
    }
}
