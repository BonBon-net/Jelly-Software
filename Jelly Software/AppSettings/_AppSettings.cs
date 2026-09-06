using System;

namespace Jelly_Software.AppSettings
{
    public class _AppSettings
    {
        public static ProgramSettings ProgramSettings = null!;
        public static ImdbServiceSettings ImdbServiceSettings = null!;

        /// <summary>
        /// Loads application configuration and prints status updates to the console.
        /// </summary>
        /// <returns>True if a critical error occurs, otherwise False.</returns>
        public static bool LoadSettings()
        {
            int settingsCount = 11; // Total settings tracked during initialization

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
    }
}
