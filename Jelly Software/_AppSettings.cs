using Jelly_Software.AppSettings;
using System;

namespace Jelly_Software
{
    public class _AppSettings
    { 
        public static ProgramSettings ProgramSettings = null!;

        /// <summary>
        /// Loads application configuration and prints status updates to the console.
        /// </summary>
        /// <returns>True if a critical error occurs, otherwise False.</returns>
        public static bool LoadSettings()
        {
            int settingsCount = 2; // Total settings tracked during initialization

            try
            {
                ProgramSettings = AppSettings.ProgramSettings.Load();

                // Dev Note: Colored output during startup steps
                preBuildTools.WriteLineColored($"[LOADED] (1/{settingsCount}) Allow Colors: {ProgramSettings.AllowColors}", ConsoleColor.Cyan);
                preBuildTools.WriteLineColored($"[LOADED] (2/{settingsCount}) Allow Sound Beep: {ProgramSettings.AllowBeep}", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                preBuildTools.WriteLineColored($"[CRITICAL ERROR] Failed to load program settings:\n{ex.Message}", ConsoleColor.Red);
                return true;
            }

            return false;
        }
    }
}
