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
        private static bool initialized;

        static void Main(string[] args)
        {
            initialized = false;
            Console.CursorVisible = false;

            initialize();

            if (initialized)
            {
                ImdbService.TVShowMain().Wait();
            }
            else
            {
                // Play a beep sound to indicate failure
                Console.Beep();

                preBuildTools.WriteLineColored("\n\n\n\n[Initialization Failed] Exiting application...", ConsoleColor.Red);
                // User instruction prompt
                Console.WriteLine("Press [ENTER] to close.");
                Console.ReadLine();
            }
        }

        private static void initialize()
        {
            try
            {
                // 1. Clear the console and display startup message with color support
                Console.Clear();
                preBuildTools.WriteLineColored("[STAND BY] Initialization in progress...\n", ConsoleColor.Yellow, true);

                // 2. Load settings
                if (_AppDatabase.LoadSettings())
                    throw new Exception("Setting loader returned a critical failure.");

                // 3. Mark initialization as successful
                initialized = true;
                preBuildTools.WriteLineColored("\n[SUCCESS] Initialization complete!", ConsoleColor.Green, true);

                if (_AppDatabase.ProgramSettings.AllowShowInitializeProgress) // <-- use the type name, not the instance
                    System.Threading.Thread.Sleep(3500); // Brief pause so the user can view success status

                Console.Clear();
                Console.CursorVisible = true;
            }
            catch (Exception ex)
            {
                // Play a beep sound to indicate failure
                Console.Beep();
                // 4. Handle initialization failure
                preBuildTools.WriteLineColored($"\n[INITIALIZE FAILED] : {ex.Message}", ConsoleColor.Red, true);
            }
        }
    }
}
