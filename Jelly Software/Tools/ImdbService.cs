using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Jelly_Software.preBuildTools;
using System.Globalization;

namespace Jelly_Software.Tools
{
    public class ImdbService
    {
        private static readonly HttpClient _httpClient = new();

        private static readonly string operationCancelled = "\n\nOperation cancelled by the user.";

        // Tracks if a 429 was hit during the current fetch to manage the line gaps
        private static bool _rateLimitHit = false;

        public static async Task TVShowMain()
        {
            while (true)
            {
                try
                {
                    WriteLineGreen("Version 1.0.5 (Testing)");
                    WriteLineGreen("Type 'Help' for more information.\nInsert TV Show Folder Path:");

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("> ");
                    Console.ResetColor();

                    string folderPath = Console.ReadLine() ?? throw new NullReferenceException();

                    if (folderPath.ToLower() == "help")
                    {
                        Help();
                    }
                    else if (folderPath.ToLower() == "break" || folderPath.ToLower() == "stop" || folderPath.ToLower() == "exit")
                    {
                        WriteLineGreen("Exiting the application...");
                        break;
                    }
                    else
                    {
                        if (!Directory.Exists(folderPath))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error: The folder path does not exist.\nPlease check the path and try again.");
                            Console.ResetColor();
                            Ending();
                            continue;
                        }
                        if (!Directory.GetDirectories(folderPath).Any(d => Regex.IsMatch(d.Split("\\").Last(), @"(?i)(?:season|series|s)\s*\d+")))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[ERROR] No valid season folders found.\nExpected folder format: 'Season 01', 'Series 1', or 'S01'.");
                            Console.ResetColor();
                            Ending();
                            continue;
                        }
                        // 3rd Check: Checks if any season folder contains at least one valid video file
                        if (!Directory.GetDirectories(folderPath)
                            .Where(d => Regex.IsMatch(d.Split("\\").Last(), @"(?i)(?:season|series|s)\s*\d+"))
                            .SelectMany(d => Directory.GetFiles(d))
                            .Any(f =>
                            {
                                string ext = f.Split('.').Last().ToLower();
                                return ext == "mkv" || ext == "mp4" || ext == "avi";
                            }))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[ERROR] No valid episode files (.mkv, .mp4, .avi) found inside the season folders.");
                            Console.ResetColor();
                            Ending();
                            continue;
                        }

                        folderPath = $"{GoToParentDirectory(folderPath)}\\{SanitizeFilename(folderPath.Split("\\").Last())}";

                        string tvShowFolderName = folderPath.Split("\\").Last();
                        string tvShowName = GetTvShowFolderTvShowName(tvShowFolderName);
                        string releaseYear = GetTvShowFolderReleaseYear(tvShowFolderName);
                        string imdbId = GetTvShowFolderImdbId(tvShowFolderName);

                        // Reset the flag for each new show search
                        _rateLimitHit = false;

                        ShowMediaMetadata showMetadata = null;
                        ShowMediaMetadata initialShowMetadata = null;
                        bool needsManualSelection = false;

                        // Check if IMDb ID is a placeholder like tt0000000
                        bool isPlaceholderImdb = imdbId.Equals("tt0000000", StringComparison.OrdinalIgnoreCase) || Regex.IsMatch(imdbId, @"^tt0{7,}$");

                        if (isPlaceholderImdb)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"\n[WARNING] Placeholder IMDb ID '{imdbId}' detected. Ignoring IMDb ID and searching database...");
                            Console.ResetColor();
                            needsManualSelection = true;
                        }
                        else
                        {
                            try
                            {
                                // Test initial fetch to verify if IMDb ID data matches folder name/year
                                initialShowMetadata = await GetShowAsync(imdbId, tvShowFolderName);

                                int.TryParse(releaseYear, out int folderYearInt);
                                bool titleMatches = initialShowMetadata.ShowTitle.Equals(tvShowName, StringComparison.OrdinalIgnoreCase);
                                bool yearMatches = folderYearInt == 0 || initialShowMetadata.ShowYear == folderYearInt;

                                if (!titleMatches || !yearMatches)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"\n[WARNING] Folder name '{tvShowName} ({releaseYear})' doesn't perfectly match the fetched data: '{initialShowMetadata.ShowTitle} ({initialShowMetadata.ShowYear})'.");
                                    Console.ResetColor();
                                    needsManualSelection = true;
                                }
                                else
                                {
                                    showMetadata = initialShowMetadata;
                                }
                            }
                            catch
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"\n[WARNING] Could not automatically pull exact match for IMDb ID: {imdbId}.");
                                Console.ResetColor();
                                needsManualSelection = true;
                            }
                        }

                        // Build manual selection list if there's a mismatch, API failure, or placeholder IMDb ID
                        if (needsManualSelection)
                        {
                            var searchResults = new List<SearchResult>();

                            // 1. If an initial show was successfully fetched via the provided IMDb ID, add it to the list first
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

                            // 2. Perform multi-query search to get options
                            string cleanTvShowName = Regex.Replace(tvShowName, @"\s*\(\d{4}\)|\s*\[.*?\]", "").Trim();
                            WriteLineGreen($"Searching database for variations of '{cleanTvShowName}'...");

                            var rawSearchResults = await SearchTvMazeMultipleAsync(cleanTvShowName);

                            foreach (var res in rawSearchResults)
                            {
                                // Check if result is a movie or TV show
                                if (res.Type.Equals("TV Movie", StringComparison.OrdinalIgnoreCase) || res.Type.Equals("Movie", StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"\n[WARNING] '{res.Title} ({res.Year})' is classified as a movie ({res.Type}), skipping from TV show selection list.");
                                    Console.ResetColor();
                                }
                                else
                                {
                                    if (!searchResults.Any(s => s.ImdbId.Equals(res.ImdbId, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        searchResults.Add(res);
                                    }
                                }
                            }

                            // Handle zero search results with manual IMDb option
                            if (searchResults.Count == 0)
                            {
                                WriteLineGreen("No alternative TV shows found on TVMaze.");
                                string[] manualQ = new string[] { "Would you like to manually enter a correct IMDb ID?", "Cancel operation" };
                                char[] manualA = new char[] { 'Y', 'N' };
                                bool wantManual = GetUserConfirmation(manualQ, manualA, new string[] { });

                                if (wantManual)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("Enter the correct IMDb ID (e.g., tt0182576) > ");
                                    Console.ResetColor();

                                    string manualImdb = Console.ReadLine()?.Trim() ?? string.Empty;

                                    if (!string.IsNullOrEmpty(manualImdb) && manualImdb.StartsWith("tt", StringComparison.OrdinalIgnoreCase))
                                    {
                                        imdbId = manualImdb;
                                        showMetadata = await GetShowAsync(imdbId, tvShowFolderName);
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine("Invalid IMDb ID format. Canceling operation.");
                                        Console.ResetColor();
                                        Ending();
                                        continue;
                                    }
                                }
                                else
                                {
                                    WriteLineGreen(operationCancelled);
                                    Ending();
                                    continue;
                                }
                            }
                            else
                            {
                                WriteLineGreen("\nFound multiple possibilities. Please confirm which series this is:");
                                for (int i = 0; i < searchResults.Count; i++)
                                {
                                    WriteLineGreen($"  [{i + 1}] {searchResults[i].Title} ({searchResults[i].Year}) - IMDb ID: {searchResults[i].ImdbId}");
                                }
                                WriteLineGreen("  [0] None of these (Cancel)");

                                int selectedIdx = -1;
                                while (true)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("\nEnter the number of the correct show > ");
                                    Console.ResetColor();

                                    string choice = Console.ReadLine()?.Trim() ?? string.Empty;

                                    if (int.TryParse(choice, out selectedIdx) && selectedIdx >= 0 && selectedIdx <= searchResults.Count)
                                    {
                                        break;
                                    }
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Invalid input. Please enter a valid number from the list.");
                                    Console.ResetColor();
                                }

                                if (selectedIdx == 0)
                                {
                                    WriteLineGreen(operationCancelled);
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
                                    string[] repW = new string[] { };
                                    bool replaceImdb = GetUserConfirmation(repQ, repA, repW);

                                    if (replaceImdb)
                                    {
                                        imdbId = newImdbId;
                                    }
                                    else
                                    {
                                        WriteLineGreen(operationCancelled);
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

                        // Check files for IMDb IDs and assign them to matching episodes
                        bool hasFileImdbIds = CheckAndApplyFileImdbIds(showMetadata);

                        if (hasFileImdbIds)
                        {
                            WriteLineGreen("\n[INFO] Found IMDb IDs in video file names and mapped them to corresponding episodes.");
                        }

                        // Final confirmation check
                        Console.WriteLine();
                        string[] confirmQ = new string[] { $"Is this the correct TV Show: {showMetadata.ShowTitle} ({showMetadata.ShowYear})?", "Cancel operation" };
                        char[] confirmA = new char[] { 'Y', 'N' };
                        string[] confirmWarnings = new string[] { };
                        bool confirmShow = GetUserConfirmation(confirmQ, confirmA, confirmWarnings);

                        if (confirmShow)
                        {
                            WriteLineGreen($"\nFetching metadata for IMDb ID: {imdbId} and folder: {tvShowFolderName}...\n");

                            showMetadata.FolderPath = folderPath;
                            if (_rateLimitHit)
                            {
                                Console.Write("\n\n\n\n");
                                WriteLineGreen($"Show Title: {showMetadata.ShowTitle}");
                            }
                            else
                            {
                                WriteLineGreen($"\nShow Title: {showMetadata.ShowTitle}");
                            }

                            WriteLineGreen($"Show Year: {showMetadata.ShowYear}");
                            WriteLineGreen($"IMDb ID: {showMetadata.ShowImdbId}");
                            WriteLineGreen($"Folder Path: {showMetadata.FolderName}");
                            WriteLineGreen("Seasons and Episodes:");

                            foreach (var season in showMetadata.Seasons)
                            {
                                WriteLineGreen($"\nSeason {season.SeasonNumber} ({season.SeasonYear}):");
                                foreach (var episode in season.Episodes)
                                {
                                    WriteLineGreen($"  Episode {episode.EpisodeNumber}: {episode.EpisodeTitle} ({episode.EpisodeYear}) - IMDb ID: {episode.EpisodeImdbId}");
                                }
                            }

                            WriteLineGreen("\nMetadata fetched successfully!");
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
                                bool renameFolder = GetUserConfirmation(folderQ, folderA, new string[] { });

                                if (renameFolder)
                                {
                                    string newParentDirectory = $"{GoToParentDirectory(showMetadata.FolderPath)}\\{expectedFolderName}";
                                    RenameFileOrFolder(showMetadata.FolderPath, newParentDirectory);
                                    showMetadata.FolderPath = newParentDirectory;
                                    tvShowFolderName = expectedFolderName;
                                    WriteLineGreen($"Parent folder renamed successfully to: {expectedFolderName}");
                                }
                            }

                            Console.WriteLine();
                            string[] question = new string[2] { "Would you like to rename the TV show files?", "Cancel operation" };
                            char[] charAnswers = new char[2] { 'Y', 'N' };
                            string[] warnings = new string[] { };
                            bool EditFiles = GetUserConfirmation(question, charAnswers, warnings);

                            if (EditFiles)
                            {
                                // --- Options for Renaming ---
                                bool UseEpisodeReleaseYear = false, dashAfterReleaseYear = false, AllowEpisodeName = false, dashAfterSeasonEpisode = false, AllowImdb = false, dashBeforeImdb = false, AllowSeasonYear = false;

                                Console.WriteLine();
                                question = new string[2] { "Yes, add (Release Year) for season folders", "No, don't add (Release Year) for season folders" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { };
                                AllowSeasonYear = GetUserConfirmation(question, charAnswers, warnings);

                                Console.WriteLine();
                                question = new string[2] { "Use episode release year in naming", "Use TV show release year in naming" };
                                charAnswers = new char[2] { '1', '2' };
                                warnings = new string[] { };
                                UseEpisodeReleaseYear = GetUserConfirmation(question, charAnswers, warnings);

                                Console.WriteLine();
                                question = new string[2] { "Yes, add dash '-' between (Release Year) & SxxExx", "No, don't add dash '-'" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { };
                                dashAfterReleaseYear = GetUserConfirmation(question, charAnswers, warnings);

                                Console.WriteLine();
                                question = new string[2] { "Include episode names", "Exclude episode names" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { };
                                AllowEpisodeName = GetUserConfirmation(question, charAnswers, warnings);

                                if (AllowEpisodeName)
                                {
                                    Console.WriteLine();
                                    question = new string[2] { "Yes, add dash '-' between SxxExx & Episode Name", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    warnings = new string[] { };
                                    dashAfterSeasonEpisode = GetUserConfirmation(question, charAnswers, warnings);
                                }

                                Console.WriteLine();
                                question = new string[2] { "Use IMDb IDs in file names", "Don't use IMDb IDs in file names" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { "The IMDb ID is not automated due to anti-bot restrictions", "Existing IMDb IDs in file names will be preserved and used" };
                                AllowImdb = GetUserConfirmation(question, charAnswers, warnings);

                                if (AllowImdb || hasFileImdbIds)
                                {
                                    Console.WriteLine();
                                    if (hasFileImdbIds)
                                        WriteLineGreen("IMDb IDs found in files. Would you like to add a dash '-' between Episode Name & IMDb ID?");

                                    question = new string[2] { "Yes, add dash '-' between Episode Name & IMDb ID", "No, don't add dash '-'" };
                                    charAnswers = new char[2] { 'Y', 'N' };
                                    warnings = new string[] { };
                                    dashBeforeImdb = GetUserConfirmation(question, charAnswers, warnings);
                                }

                                Console.WriteLine();
                                question = new string[2] { "Continue renaming", "Cancel renaming" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { "This is your last chance before all files are renamed!" };
                                bool lastChance = GetUserConfirmation(question, charAnswers, warnings);

                                if (lastChance)
                                {
                                    ChanceFilesName(showMetadata, EditFiles, UseEpisodeReleaseYear, AllowEpisodeName, AllowImdb, dashAfterReleaseYear, dashAfterSeasonEpisode, dashBeforeImdb, AllowSeasonYear);
                                }
                                else
                                {
                                    WriteLineGreen(operationCancelled);
                                    Ending();
                                    continue;
                                }
                            }
                            else
                            {
                                WriteLineGreen(operationCancelled);
                                Ending();
                                continue;
                            }
                        }
                        else
                        {
                            WriteLineGreen(operationCancelled);
                            Ending();
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n\nError: {ex.Message}");
                    Console.WriteLine("Please try again or type 'Help' for more information.");
                    Console.ResetColor();
                }

                Ending();
                continue;
            }

            void Ending()
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\n\n\nPress any key to continue...");
                Console.ResetColor();
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

        private static void ChanceFilesName(ShowMediaMetadata showMetadata, bool EditFiles, bool UseEpisodeReleaseYear, bool AllowEpisodeName, bool AllowImdb, bool dashAfterReleaseYear, bool dashAfterSeasonEpisode, bool dashAfterImdb, bool AllowSeasonYear)
        {
            List<FileInfo> files = new List<FileInfo>();
            List<DirectoryInfo> directories = new List<DirectoryInfo>();
            List<string> errorMessages = new List<string>();

            Console.WriteLine();
            WriteLineGreen("Renaming files and folders...");

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
                        errorMessages.Add($"[ERROR] Metadata for Season {parsedSeasonNum} not found. Skipping folder.");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n{errorMessages.Last()}");
                        Console.ResetColor();
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
                        RenameFileOrFolder(directories[i].FullName, $"{GoToParentDirectory(directories[i].FullName)}\\{seasonFolderFormat}");
                    }

                    i++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: The folder name '{folderName}' does not match the expected season format. Skipping this folder.");
                    Console.ResetColor();
                    directories.RemoveAt(i);
                    seasonSkippedCount++;
                    continue;
                }
            }

            WriteLineGreen($"\nTotal Folders: {seasonFoldersList.Count + seasonSkippedCount}\nTotal Seasons: {seasonFoldersList.Count}\nChanged: {seasonChangedCount}\nSkipped: {seasonSkippedCount}");
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
                        errorMessages.Add($"[ERROR] Skipping '{epString}': Metadata only has {showMetadata.Seasons[metaSeasonIndex].Episodes.Count} episodes for Season {currentSeasonNum}.");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n{errorMessages.Last()}");
                        Console.ResetColor();
                        episodeSkippedCount += filesInGroup.Count;
                        continue;
                    }

                    string baseNewName = showMetadata.ShowTitle;
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
                            WriteLineGreen($"Renaming '{fileData.OriginalFile.Name}' to '{finalName}'");
                            RenameFileOrFolder(fileData.OriginalFile.FullName, $"{GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"\n[ATTENTION] Multiple files detected for {epString}:");
                        Console.ResetColor();

                        for (int k = 0; k < filesInGroup.Count; k++)
                            WriteLineGreen($"  [{k + 1}] {filesInGroup[k].OriginalFile.Name}");

                        WriteLineGreen("  [A] Keep ALL (Rename as Part 1, Part 2...)");
                        WriteLineGreen("  [S] Skip ALL (Moves everything to Unused folder)\n");

                        string choice;
                        bool isFirstAttempt = true;
                        while (true)
                        {
                            if (!isFirstAttempt)
                            {
                                ClearConsoleLines(2);
                            }

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("Which one do you want to keep? (Enter number - '1', '2', 'A', or 'S') > ");
                            Console.ResetColor();

                            choice = Console.ReadLine()?.Trim().ToUpper() ?? string.Empty;

                            if (choice == "A" || choice == "S")
                                break;
                            if (int.TryParse(choice, out int selectedNum) && selectedNum >= 1 && selectedNum <= filesInGroup.Count)
                                break;

                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Invalid input. Please enter a valid number from the list ('1', '2', 'A', or 'S').");
                            Console.ResetColor();
                            isFirstAttempt = false;
                        }

                        if (choice == "A")
                        {
                            for (int k = 0; k < filesInGroup.Count; k++)
                            {
                                var fileData = filesInGroup[k];
                                string finalName = $"{baseNewName} - Part {k + 1}.{fileData.Extension}";
                                RenameFileOrFolder(fileData.OriginalFile.FullName, $"{GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                            }
                        }
                        else if (choice == "S")
                        {
                            if (!Directory.Exists(unusedFolderPath)) Directory.CreateDirectory(unusedFolderPath);

                            for (int k = 0; k < filesInGroup.Count; k++)
                            {
                                var fileData = filesInGroup[k];
                                string unusedDestination = $"{unusedFolderPath}\\{fileData.OriginalFile.Name}";
                                RenameFileOrFolder(fileData.OriginalFile.FullName, unusedDestination);
                            }
                            episodeSkippedCount += filesInGroup.Count;
                            WriteLineGreen($"Moved all files for {epString} to 'Unused Episodes'.");
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
                                    RenameFileOrFolder(fileData.OriginalFile.FullName, $"{GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                                }
                                else
                                {
                                    string unusedDestination = $"{unusedFolderPath}\\{fileData.OriginalFile.Name}";
                                    RenameFileOrFolder(fileData.OriginalFile.FullName, unusedDestination);
                                }
                            }

                            episodeSkippedCount += (filesInGroup.Count - 1);
                            WriteLineGreen($"Kept option {choice}. Moved remaining files to 'Unused Episodes'.");
                        }
                    }
                }
            }

            if (errorMessages.Count > 0)
            {
                Console.WriteLine("\n\n");
                Console.ForegroundColor = ConsoleColor.Red;
                for (int i = 0; i < errorMessages.Count; i++)
                    Console.WriteLine($"{i + 1}) {errorMessages[i]}");

                Console.WriteLine($"\n\nTotal Errors: {errorMessages.Count.ToString("N0", new CultureInfo("de-DE"))}");
                Console.ResetColor();
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
                    _rateLimitHit = true;

                    int delayMs = 30000;
                    if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue)
                    {
                        delayMs += (int)response.Headers.RetryAfter.Delta.Value.TotalMilliseconds;
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

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"\rRate limit reached (429). Retrying in: {formattedTime}".PadRight(95));
                        Console.ResetColor();

                        await Task.Delay(15);
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"\rRate limit reached (429). Retrying in: 0ms".PadRight(95));
                    Console.ResetColor();

                    continue;
                }

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

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================================");
            Console.WriteLine("                            JELLY SOFTWARE - HELP                              ");
            Console.WriteLine("===============================================================================\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> OVERVIEW");
            Console.ResetColor();
            WriteLineGreen("  This application fetches metadata for a TV show based on its IMDb ID");
            WriteLineGreen("  and helps automatically organize and rename your folders and episode files.\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> USAGE INSTRUCTIONS");
            Console.ResetColor();
            WriteLineGreen("  1. When prompted, insert the full directory path of your TV show folder.");
            WriteLineGreen("  2. To quit the application at any time, type 'Break', 'Stop', or 'Exit'.\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> REQUIRED FOLDER FORMAT");
            Console.ResetColor();
            WriteLineGreen("  The parent folder must contain the Title, Release Year, and IMDb ID.");
            WriteLineGreen("  Format:  Show Title (Year) [imdbid-ttXXXXXXX]");
            WriteLineGreen("  Example: Breaking Bad (2008) [imdbid-tt0903747]");
            WriteLineGreen("  Example: Family Guy (1999) [imdbid-tt0182576]\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> FEATURES & BEHAVIOR");
            Console.ResetColor();
            WriteLineGreen("  * Auto-Mapping: The app fetches data and maps it to your season folders.");
            WriteLineGreen("  * Multi-Part Episodes: If a season folder has multiple files for the same");
            WriteLineGreen("    episode number, the process will pause and ask you how to resolve it.");
            WriteLineGreen("  * IMDb ID Auto-Detection: If an IMDb ID (e.g., 'tt0959621') already exists");
            WriteLineGreen("    in an episode's file name, the app will automatically detect it and tag");
            WriteLineGreen("    the newly renamed file with it.\n");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> TROUBLESHOOTING & NOTES");
            Console.ResetColor();
            WriteLineGreen("  * If the console appears frozen during processing, try pressing [ENTER].");
            WriteLineGreen("  * Note: This software has currently not been tested on 'Season 0' (Specials).\n");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("===============================================================================\n");
            Console.ResetColor();
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
    }
}