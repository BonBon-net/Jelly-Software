using Jelly_Software.AppSettings;
using Jelly_Software.Tools;

namespace Jelly_Software
{
    internal class Program
    {
        static void Main(string[] args)
        {
            initialize();

            if (preBuildTools.Setting.IsProgramInitialized)
            {
                ImdbService.TVShowMain().Wait();
            }
            else
            {
                // Play a beep sound to indicate failure
                Console.Beep();

                preBuildTools.WriteLine("\n\n\n\n[Initialization Failed] Exiting application...", ConsoleColor.Red, !preBuildTools.Setting.IsProgramInitialized);
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
                // 1. Clear the console and display startup message with color support
                Console.Clear();
                preBuildTools.WriteLine("[STAND BY] Initialization in progress...", ConsoleColor.Yellow, !preBuildTools.Setting.IsProgramInitialized);

                // 2. Load settings
                Setting setting = preBuildTools.Setting.Load();
                if (setting == null)
                    throw new Exception("Setting loader returned a critical failure.");
                preBuildTools.WriteLine($"\n[INFO] Settings loaded successfully from {Setting.FilePath}", ConsoleColor.Green, !preBuildTools.Setting.IsProgramInitialized);

                // 3. Mark initialization as successful
                preBuildTools.WriteLine("\n[SUCCESS] Initialization complete!", ConsoleColor.Green, !preBuildTools.Setting.IsProgramInitialized);

                Console.WriteLine();
                if (preBuildTools.Setting.AllowShowInitializeProgress)
                    preBuildTools.Countdown(preBuildTools.Setting.InitializeProgressTimer, !preBuildTools.Setting.IsProgramInitialized);

                // --> Apply the global background and safe typing color
                Console.BackgroundColor = preBuildTools.Setting.BackgroundColor;
                Console.ForegroundColor = preBuildTools.EnsureContrast(ConsoleColor.Gray);
                Console.Clear();
                Console.CursorVisible = true;
                preBuildTools.Setting.IsProgramInitialized = true;
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                // 4. Handle initialization failure
                preBuildTools.WriteLine($"\n[INITIALIZE FAILED] : {ex.Message}", ConsoleColor.Red, !preBuildTools.Setting.IsProgramInitialized);
            }
        }
    }
}
