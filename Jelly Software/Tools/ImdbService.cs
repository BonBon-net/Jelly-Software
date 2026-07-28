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

        public static async Task TVShowMain()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Version 1.0.3.1");
                    Console.Write("Insert 'Help' for more infomation!\nInsert Tv Show Folder Path\n> ");
                    string folderPath = Console.ReadLine() ?? throw new NullReferenceException();
                    if (folderPath.ToLower() == "Help".ToLower())
                    {
                        Help();
                    }
                    else if (folderPath.ToLower() == "Break".ToLower() || folderPath.ToLower() == "Stop".ToLower() || folderPath.ToLower() == "Exit".ToLower())
                    {
                        Console.WriteLine("Exiting the application...");
                        break;
                    }
                    else
                    {
                        string tvShowFolderName = folderPath.Split("\\").Last();

                        string tvShowName = GetTvShowFolderTvShowName(tvShowFolderName);
                        string releaseYear = GetTvShowFolderReleaseYear(tvShowFolderName);
                        string imdbId = GetTvShowFolderImdbId(tvShowFolderName);

                        Console.WriteLine($"\nFetching metadata for IMDb ID: {imdbId} and folder: {tvShowFolderName}...\n");
                        ShowMediaMetadata showMetadata = await ImdbService.GetShowAsync(imdbId, tvShowFolderName);
                        showMetadata.FolderPath = folderPath;

                        Console.WriteLine($"\nShow Title: {showMetadata.ShowTitle}");
                        Console.WriteLine($"Show Year: {showMetadata.ShowYear}");
                        Console.WriteLine($"IMDb ID: {showMetadata.ShowImdbId}");
                        Console.WriteLine($"Folder Path: {showMetadata.FolderName}");
                        Console.WriteLine("Seasons and Episodes:");

                        foreach (var season in showMetadata.Seasons)
                        {
                            Console.WriteLine($"\nSeason {season.SeasonNumber} ({season.SeasonYear}):");
                            foreach (var episode in season.Episodes)
                            {
                                Console.WriteLine($"  Episode {episode.EpisodeNumber}: {episode.EpisodeTitle} ({episode.EpisodeYear}) - IMDb ID: {episode.EpisodeImdbId}");
                            }
                        }

                        Console.WriteLine("\nMetadata fetched successfully!");

                        // EditFiles
                        Console.WriteLine();
                        string[] question = new string[2] { "Would you like to rename the tv show files", "Would you like to cancel?" };
                        char[] charAnswers = new char[2] { 'Y', 'N' };
                        string[] warnings = new string[] { };
                        bool EditFiles = GetUserConfirmation(question, charAnswers, warnings);
                        bool UseEpisodeReleaseYear = false;
                        bool dashAfterReleaseYear = false;
                        bool AllowEpisodeName = false;
                        bool dashAfterSeasonEpisode = false;
                        bool AllowImdb = false;
                        bool dashAfterImdb = false;
                        if (EditFiles)
                        {
                            // UseEpisodeReleaseYear
                            Console.WriteLine();
                            question = new string[2] { "Use episode Release Year in nameing", "Use tv show Release Year in nameing" };
                            charAnswers = new char[2] { '1', '2' };
                            warnings = new string[] { };
                            UseEpisodeReleaseYear = GetUserConfirmation(question, charAnswers, warnings);

                            // Allow dash in between "UseEpisodeReleaseYear" & "Series Season & episode"
                            Console.WriteLine();
                            question = new string[2] { "Would you like too add dash '-' between (Release Year) & SxxExx", "Exclude the dash '-'" };
                            charAnswers = new char[2] { 'Y', 'N' };
                            warnings = new string[] { };
                            dashAfterReleaseYear = GetUserConfirmation(question, charAnswers, warnings);

                            // AllowEpisodeName
                            Console.WriteLine();
                            question = new string[2] { "Allow the usage of episode name", "Disallow the usage of episode name" };
                            charAnswers = new char[2] { 'Y', 'N' };
                            warnings = new string[] { };
                            AllowEpisodeName = GetUserConfirmation(question, charAnswers, warnings);

                            if (AllowEpisodeName)
                            {
                                Console.WriteLine();
                                question = new string[2] { "Would you like too add dash '-' between SxxExx & Episode Name", "Exclude the dash '-'" };
                                charAnswers = new char[2] { 'Y', 'N' };
                                warnings = new string[] { };
                                dashAfterSeasonEpisode = GetUserConfirmation(question, charAnswers, warnings);
                            }

                            // AllowImdb
                            Console.WriteLine();
                            question = new string[2] { "allow the usage of Imdb", "Disallow the usage of Imdb" };
                            charAnswers = new char[2] { 'Y', 'N' };
                            warnings = new string[] { "The 'id' from 'Imdb' is not automated due to anti-bot", "imdb id that exist in file name will be used" };
                            AllowImdb = GetUserConfirmation(question, charAnswers, warnings);

                            Console.WriteLine();
                            question = new string[2] { "Would you like too add dash '-' between Episode Name & Imdb id", "Exclude the dash '-'" };
                            charAnswers = new char[2] { 'Y', 'N' };
                            warnings = new string[] { };
                            dashAfterImdb = GetUserConfirmation(question, charAnswers, warnings);

                            Console.WriteLine();
                            question = new string[2] { "Last chance to cancel", "Continue" };
                            charAnswers = new char[2] { 'Y', 'N' };
                            warnings = new string[] { "This is the last chance before all the file's will be rename'd !!!" };
                            bool lastChance = GetUserConfirmation(question, charAnswers, warnings);

                            if (!lastChance)
                            {
                                if (tvShowFolderName != $"{showMetadata.ShowTitle} ({showMetadata.ShowYear}) [imdbid-{showMetadata.ShowImdbId}]")
                                {
                                    Console.WriteLine($"\nWarning: The folder name '{tvShowFolderName}' does not match the expected format '{showMetadata.ShowTitle} ({showMetadata.ShowYear}) [imdbid-{showMetadata.ShowImdbId}]'.");
                                    question = new string[2] { "Do you want to rename the folder name with the expected format & continue?", "Do you want to cancel?" };
                                    charAnswers = new char[2] { '1', '2' };
                                    warnings = new string[] { };
                                    bool userConfirmation = GetUserConfirmation(question, charAnswers, warnings);
                                    if (userConfirmation)
                                    {
                                        string newParentDirectory = $"{GoToParentDirectory(showMetadata.FolderPath)}\\{showMetadata.ShowTitle} ({showMetadata.ShowYear}) [imdbid-{showMetadata.ShowImdbId}]";
                                        RenameFileOrFolder(showMetadata.FolderPath, newParentDirectory);
                                        showMetadata.FolderPath = newParentDirectory;
                                        ChanceFilesName(showMetadata, EditFiles, UseEpisodeReleaseYear, AllowEpisodeName, AllowImdb, dashAfterReleaseYear, dashAfterSeasonEpisode, dashAfterImdb);
                                    }
                                    else
                                    {
                                        Console.WriteLine("\n\nOperation cancelled by the user.");
                                    }
                                }
                                else
                                {
                                    ChanceFilesName(showMetadata, EditFiles, UseEpisodeReleaseYear, AllowEpisodeName, AllowImdb, dashAfterReleaseYear, dashAfterSeasonEpisode, dashAfterImdb);
                                }
                            }
                            else
                            {
                                Console.WriteLine("\n\nOperation cancelled by the user.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\n\nOperation cancelled by the user.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n\nError: {ex.Message}");
                    Console.WriteLine("Please try again or type 'Help' for more information.");
                }
                // Clear the console for the next iteration
                Console.Write("\n\n\nPress any key to continue");
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void ChanceFilesName(ShowMediaMetadata showMetadata, bool EditFiles, bool UseEpisodeReleaseYear, bool AllowEpisodeName, bool AllowImdb, bool dashAfterReleaseYear, bool dashAfterSeasonEpisode, bool dashAfterImdb)
        {
            List<FileInfo> files = new List<FileInfo>();
            List<DirectoryInfo> directories = new List<DirectoryInfo>();

            List<string> errorMassages = new List<string>();

            Console.WriteLine();
            Console.WriteLine("Renaming files and folders...");

            directories.AddRange(new DirectoryInfo(showMetadata.FolderPath).GetDirectories());

            List<(string FolderName, int SeasonNum)> seasonFoldersList = new List<(string, int)>();
            int seasonChancedCount = 0;
            int seasonSkippedCount = 0;

            for (int i = 0; i < directories.Count;)
            {
                string folderName = directories[i].Name;

                if (folderName.Equals("Unused Episodes", StringComparison.OrdinalIgnoreCase))
                {
                    directories.RemoveAt(i);
                    continue;
                }

                // Flexible Regex to find variations like: "Season 1", "Season.1", "S01", "Series 1", etc.
                Match seasonMatch = Regex.Match(folderName, @"(?i)(?:season|series|s)\s*(\d+)");

                if (seasonMatch.Success)
                {
                    int parsedSeasonNum = int.Parse(seasonMatch.Groups[1].Value);
                    int metaSeasonIndex = parsedSeasonNum - 1;

                    if (metaSeasonIndex < 0 || metaSeasonIndex >= showMetadata.Seasons.Count)
                    {
                        errorMassages.Add($"[ERROR] Metadata for Season {parsedSeasonNum} not found. Skipping folder.");
                        Console.WriteLine($"\n{errorMassages.Last()}");
                        directories.RemoveAt(i);
                        seasonSkippedCount++;
                        continue;
                    }

                    string seasonFolderFormat = $"Season {parsedSeasonNum:D2} ({showMetadata.Seasons[metaSeasonIndex].SeasonYear})";
                    seasonFoldersList.Add((seasonFolderFormat, parsedSeasonNum));

                    if (directories[i].FullName.Split("\\").Last() != seasonFolderFormat)
                    {
                        seasonChancedCount++;
                        RenameFileOrFolder(directories[i].FullName, $"{GoToParentDirectory(directories[i].FullName)}\\{seasonFolderFormat}");
                    }

                    i++;
                }
                else
                {
                    Console.WriteLine($"Warning: The folder name '{folderName}' does not match the expected season format. Skipping this folder.");
                    directories.RemoveAt(i);
                    seasonSkippedCount++;
                    continue;
                }
            }

            Console.WriteLine($"\nTotal Folders: {seasonFoldersList.Count + seasonSkippedCount}\nTotal seasons: {seasonFoldersList.Count}\nchanged: {seasonChancedCount}\nSkipped: {seasonSkippedCount}");

            int episodeSkippedCount = 0;
            string unusedFolderPath = $"{showMetadata.FolderPath}\\Unused Episodes";

            for (int i = 0; i < seasonFoldersList.Count; i++)
            {
                List<FileInfo> seasonFiles = new List<FileInfo>();
                string currentFolderName = seasonFoldersList[i].FolderName;
                int currentSeasonNum = seasonFoldersList[i].SeasonNum;
                int metaSeasonIndex = currentSeasonNum - 1;

                seasonFiles.AddRange(new DirectoryInfo($"{showMetadata.FolderPath}\\{currentFolderName}").GetFiles());

                // Updated dictionary to also track the ExtractedImdbId
                var episodeGroups = new Dictionary<string, List<(FileInfo OriginalFile, string Extension, int EpNum, string ExtractedImdbId)>>();

                for (int j = 0; j < seasonFiles.Count; j++)
                {
                    string fileName = seasonFiles[j].Name;
                    string fileExtension = fileName.Split('.').Last().ToLower();

                    if (fileExtension != "mkv" && fileExtension != "mp4" && fileExtension != "avi")
                    {
                        episodeSkippedCount++;
                        continue;
                    }

                    // Strip the file extension so we don't accidentally parse numbers inside ".mp4" etc.
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                    // 1. Try explicit Season AND Episode formats (e.g., S02E05, 2x05, 02.05, 2x05-06)
                    Match fullMatch = Regex.Match(nameWithoutExt, @"(?i)(?:s|season\s*)?(\d+)(?:[ .\-x]+|(?:e|episode\s*|x))(\d+)(?:[ .\-x]+(?:e|episode\s*|x)?(\d+))?");

                    // 2. Try standalone Episode formats with unlimited digits (e.g., 05, 104, 05-06)
                    Match epOnlyMatch = Regex.Match(nameWithoutExt, @"(?i)(?:^|[ \-])(?:e|episode\s*)?(\d+)(?:[ .\-x]+(?:e|episode\s*)?(\d+))?\b");

                    // Extract existing IMDb ID from the original file name if it exists
                    Match imdbMatch = Regex.Match(fileName, @"(?i)(tt\d{7,10})");
                    string extractedImdbId = imdbMatch.Success ? imdbMatch.Groups[1].Value.ToLower() : string.Empty;

                    bool isMatched = false;
                    int seasonNum = 0, episode1Num = 0, episode2Num = 0;
                    bool hasMultiPart = false;

                    if (fullMatch.Success)
                    {
                        // We found both Season and Episode in the file name
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
                        // We ONLY found an episode number, so we grab the season from the folder loop!
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
                    string extractedImdbId = filesInGroup[0].ExtractedImdbId; // Grab the extracted ID
                    int epIndex = epNum - 1;

                    if (epIndex < 0 || epIndex >= showMetadata.Seasons[metaSeasonIndex].Episodes.Count)
                    {
                        errorMassages.Add($"[ERROR] Skipping '{epString}': Metadata only has {showMetadata.Seasons[metaSeasonIndex].Episodes.Count} episodes for Season {currentSeasonNum}.");
                        Console.WriteLine($"\n{errorMassages.Last()}");
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

                    // Handles IMDb ID injection logic
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
                        // Forces IMDb ID into the name if the original file had it, even if AllowImdb is false
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
                            Console.WriteLine($"Renaming '{fileData.OriginalFile.Name}' to '{finalName}'");
                            RenameFileOrFolder(fileData.OriginalFile.FullName, $"{GoToParentDirectory(fileData.OriginalFile.FullName)}\\{finalName}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"\n[ATTENTION] Multiple files detected for {epString}:");
                        for (int k = 0; k < filesInGroup.Count; k++)
                            Console.WriteLine($"  [{k + 1}] {filesInGroup[k].OriginalFile.Name}");

                        Console.WriteLine($"  [A] Keep ALL (Rename as Part 1, Part 2...)");
                        Console.WriteLine($"  [S] Skip ALL (Moves everything to Unused folder)\n");

                        string choice;
                        bool isFirstAttempt = true;

                        while (true)
                        {
                            if (!isFirstAttempt)
                            {
                                ClearConsoleLines(2);
                            }

                            Console.Write("Which one do you want to keep? (Enter number - '1', '2', 'A', or 'S') > ");
                            choice = Console.ReadLine()?.Trim().ToUpper() ?? string.Empty;

                            if (choice == "A" || choice == "S")
                                break;

                            if (int.TryParse(choice, out int selectedNum) && selectedNum >= 1 && selectedNum <= filesInGroup.Count)
                                break;

                            Console.WriteLine("Invalid input. Please enter a valid number from the list - '1', '2', 'A', or 'S'.");
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
                            Console.WriteLine($"Moved all files for {epString} to 'Unused Episodes'.");
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
                            Console.WriteLine($"Kept option {choice}. Moved the rest to 'Unused Episodes'.");
                        }
                    }
                }
            }

            if (errorMassages.Count > 0)
            {
                Console.WriteLine($"\n\n");
                for (int i = 0; i < errorMassages.Count; i++)
                    Console.WriteLine($"{i + 1}) {errorMassages[i]}");
                Console.WriteLine($"\n\nTotal Errors: {errorMassages.Count.ToString("N0", new CultureInfo("de-DE"))}");
            }
        }

        private static async Task<ShowMediaMetadata> GetShowAsync(string imdbId, string folderPath = "")
        {
            var show = new ShowMediaMetadata
            {
                ShowImdbId = imdbId,
                //FolderName = folderPath
            };

            // Step 1: Look up show to get TVMaze numeric ID
            string lookupUrl = $"https://api.tvmaze.com/lookup/shows?imdb={imdbId}";
            HttpResponseMessage showResponse = await _httpClient.GetAsync(lookupUrl);
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

            // Step 2: Fetch all episodes for the show
            string episodesUrl = $"https://api.tvmaze.com/shows/{showId}/episodes";
            HttpResponseMessage episodesResponse = await _httpClient.GetAsync(episodesUrl);

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

                // Step 3: Fetch specific episode details to retrieve its IMDb ID
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

        // Helper method to look up external IDs for an individual episode
        private static async Task<string> GetEpisodeImdbIdAsync(int episodeId)
        {
            try
            {
                string epDetailUrl = $"https://api.tvmaze.com/episodes/{episodeId}";
                HttpResponseMessage response = await _httpClient.GetAsync(epDetailUrl);
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
            if (tvShowName == string.Empty || tvShowName == null)
                throw new Exception("TV Show Name not found in the folder name.");
            return tvShowName;
        }
        private static string GetTvShowFolderReleaseYear(string getReleaseYear)
        {
            string releaseYear = getReleaseYear.Split("(").Last().Split(")").First();
            if (releaseYear == string.Empty || releaseYear == null)
                throw new Exception("Release Year not found in the folder name.");
            if (!int.TryParse(releaseYear, out _))
                throw new Exception("Release Year is not a valid integer.");
            return releaseYear;
        }
        private static string GetTvShowFolderImdbId(string getImdbId)
        {
            string imdbId = getImdbId.Split(" ").Last().Split("-").Last().Split("]").First();
            if (imdbId == string.Empty || imdbId == null)
                throw new Exception("IMDb ID not found in the folder name.");
            return imdbId;
        }
        private static void Help()
        {
            Console.WriteLine("\nHelp Information:");
            Console.WriteLine("\n1) This application fetches metadata for a TV show based on its IMDb ID.");
            Console.WriteLine("\n2) Please provide the folder path of the TV show when prompted.");
            Console.WriteLine("\n3) The folder name should contain the TV show name, release year, and IMDb ID in the following format:");
            Console.WriteLine(" 'TV Show Name' '(Release Year)' '[IMDb ID]'");
            Console.WriteLine("-    Example 1: Breaking Bad (2008) [imdbid-tt0903747]");
            Console.WriteLine("-    Example 2: Family Guy (1999) [imdbid-tt0182576]");
            Console.WriteLine("\n4) The application will then fetch and display metadata for the show, including seasons and episodes.");
            Console.WriteLine("\n5) Insert 'Break', 'Stop' or 'Exit' to leave this application");
            Console.WriteLine("\n6) In case of console is frozen try & press enter");
            Console.WriteLine("\n7) Be aware if a season folder has episode's with the number it'll pause until question answered by a user");
            Console.WriteLine("\n8) This software has not been test'd on Season 0");
            Console.WriteLine("\n9) If any imdb id Example 'tt0959621' exist in file name - '[imdbid-]' will be added for the file name with the found id");
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