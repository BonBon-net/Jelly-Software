using Jelly_Software.AppSettings;
using Jelly_Software.Tools;
using System;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace Jelly_Software
{
    internal class Program
    {
        static void Main(string[] args)
        {
            initialize();

            if (_AppDatabase.IsProgramInitialized)
            {
                ImdbService.TVShowMain().Wait();
            }
            else
            {
                // Play a beep sound to indicate failure
                Console.Beep();

                preBuildTools.WriteLineColored("\n\n\n\n[Initialization Failed] Exiting application...", ConsoleColor.Red, !_AppDatabase.IsProgramInitialized);
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
                preBuildTools.WriteLineColored("[STAND BY] Initialization in progress...\n", ConsoleColor.Yellow, !_AppDatabase.IsProgramInitialized);

                // 2. Load settings
                if (_AppDatabase.LoadSettings())
                    throw new Exception("Setting loader returned a critical failure.");

                // 3. Mark initialization as successful
                _AppDatabase.IsProgramInitialized = true;
                preBuildTools.WriteLineColored("\n[SUCCESS] Initialization complete!", ConsoleColor.Green, !_AppDatabase.IsProgramInitialized);

                Console.WriteLine();
                if (_AppDatabase.ProgramSettings.AllowShowInitializeProgress)
                    preBuildTools.Countdown(_AppDatabase.ProgramSettings.InitializeProgressTimer, _AppDatabase.IsProgramInitialized);

                // --> Apply the global background and safe typing color
                Console.BackgroundColor = _AppDatabase.ProgramSettings.BackgroundColor;
                Console.ForegroundColor = preBuildTools.EnsureContrast(ConsoleColor.Gray);
                Console.Clear();
                Console.CursorVisible = true;
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                // 4. Handle initialization failure
                preBuildTools.WriteLineColored($"\n[INITIALIZE FAILED] : {ex.Message}", ConsoleColor.Red, !_AppDatabase.IsProgramInitialized);
            }
        }
    }
}
