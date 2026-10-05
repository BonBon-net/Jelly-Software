using Jelly_Software.AppSettings;
using Jelly_Software.Tools;

namespace Jelly_Software
{
    internal class Program
    {
        static void Main(string[] args)
        {
            initialize();

            if (PreBuildTools.Setting.IsProgramInitialized)
            {
                ImdbService.TVShowMain().Wait();
            }
            else
            {
                // Play a beep sound to indicate failure
                Console.Beep();

                PreBuildTools.WriteLine("\n\n\n\n[Initialization Failed] Exiting application...", ConsoleColor.Red, PreBuildTools.Setting.AllowColors);
                // User instruction prompt
                Console.WriteLine("Press any key to close.");
                Console.ReadKey();
            }
        }

        private static void initialize()
        {
            try
            {
                Console.CursorVisible = false;
                // 2. Load settings from the database
                Setting setting = PreBuildTools.Setting.Load();
                if (setting == null)
                    throw new Exception("Setting loader returned a critical failure.");
                // Set console colors based on settings
                Console.BackgroundColor = PreBuildTools.Setting.BackgroundColor;
                Console.ForegroundColor = PreBuildTools.EnsureContrast(ConsoleColor.Gray);

                // 1. Clear the console and display startup message with color support
                Console.Clear();
                PreBuildTools.WriteLine("[STAND BY] Initialization in progress...", ConsoleColor.Yellow, setting.AllowColors);

                Task.Delay(350).Wait(); // Simulate some delay for initialization
                PreBuildTools.Setting = setting;
                PreBuildTools.WriteLine($"\n[INFO] Settings loaded successfully from '{Setting.FilePath}'", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                // 3. Mark initialization as successful
                PreBuildTools.WriteLine("\n[SUCCESS] Initialization complete!", ConsoleColor.Green, PreBuildTools.Setting.AllowColors);

                Console.WriteLine();
                if (PreBuildTools.Setting.AllowShowInitializeProgress)
                    PreBuildTools.Countdown(PreBuildTools.Setting.InitializeProgressTimer, true, string.Empty, PreBuildTools.Setting.AllowColors);

                // Clear the console and reset cursor visibility
                Console.Clear();
                Console.CursorVisible = true;
                PreBuildTools.Setting.IsProgramInitialized = true;
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                // 4. Handle initialization failure
                PreBuildTools.WriteLine($"\n[INITIALIZE FAILED] : {ex.Message}", ConsoleColor.Red, PreBuildTools.Setting.AllowColors);
            }
        }
    }
}
