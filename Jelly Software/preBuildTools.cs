using Jelly_Software.AppSettings;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Timers;

namespace Jelly_Software
{
    public static class PreBuildTools
    {
        /// <summary>
        /// Default configuration for console background color and color allowance.
        /// </summary>
        public static Setting Setting = new();

        private static readonly ConsoleColor DefaltForegroundColor = ConsoleColor.White;
        private static readonly bool DefaltAllowColors = true;

        /// <summary>Ensures the text color contrasts with the console background.</summary>
        /// <param name="textColor">Intended text color.</param>
        /// <returns>A contrasting ConsoleColor.</returns>
        public static ConsoleColor EnsureContrast(ConsoleColor textColor)
        {
            // ... (keep the rest of the method exactly the same)
            // Fallback to the current console background color
            ConsoleColor bgColor = Console.BackgroundColor;

            // Use the saved setting only if the settings are fully initialized
            if (Setting != null)
            {
                bgColor = Setting.BackgroundColor;
            }

            if (textColor == bgColor)
            {
                int colorValue = (int)textColor;
                // Shift by 8 to toggle between the light and dark version of the color
                return (ConsoleColor)(colorValue > 7 ? colorValue - 8 : colorValue + 8);
            }

            return textColor;
        }

        /// <summary>Writes text to the console, followed by a line terminator.</summary>
        /// <param name="text">Text to write.</param> <param name="color">Text color.</param>
        /// <param name="allowColors">Whether to allow color rendering.</param>
        public static void WriteLine()
        {
            WriteLine(string.Empty, DefaltForegroundColor, Setting.AllowColors);
        }
        public static void WriteLine(string text)
        {
            WriteLine(text, DefaltForegroundColor, Setting.AllowColors);
        }
        public static void WriteLine(string text, bool allowColors)
        {
            WriteLine(text, DefaltForegroundColor, allowColors);
        }
        public static void WriteLine(string text, ConsoleColor color)
        {
            WriteLine(text, color, Setting.AllowColors);
        }
        public static void WriteLine(string text, ConsoleColor color, bool AllowColors)
        {
            if (AllowColors)
            {
                // Route the requested color through the contrast check
                Console.ForegroundColor = EnsureContrast(color);
                Console.WriteLine(text);

                Console.ForegroundColor = DefaltForegroundColor;
            }
            else
            {
                Console.ForegroundColor = DefaltForegroundColor;
                Console.WriteLine(text);
            }
        }

        /// <summary>Writes text to the console without a line terminator.</summary>
        /// <param name="text">Text to write.</param> <param name="color">Text color.</param>
        /// <param name="allowColors">Whether to allow color rendering.</param>
        public static void Write(string text)
        {
            Write(text, DefaltForegroundColor, Setting.AllowColors);
        }
        public static void Write(string text, bool allowColors)
        {
            Write(text, DefaltForegroundColor, allowColors);
        }
        public static void Write(string text, ConsoleColor color)
        {
            Write(text, color, Setting.AllowColors);
        }
        public static void Write(string text, ConsoleColor color, bool AllowColors)
        {
            if (AllowColors)
            {
                // Route the requested color through the contrast check
                Console.ForegroundColor = EnsureContrast(color);
                Console.Write(text);

                Console.ForegroundColor = DefaltForegroundColor;
            }
            else
            {
                Console.ForegroundColor = DefaltForegroundColor;
                Console.Write(text);
            }
        }

        /// <summary>Moves a file or directory to a new destination.</summary>
        /// <param name="sourcePath">The current path.</param>
        /// <param name="destinationPath">The target destination path.</param>
        public static void MoveFileSystemItem(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentNullException(nameof(sourcePath));

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentNullException(nameof(destinationPath));

            // Clean up trailing slashes to ensure Path.GetFileName works correctly on folders
            sourcePath = sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            destinationPath = destinationPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!File.Exists(sourcePath) && File.Exists(destinationPath))
                destinationPath = GoToParentDirectory(destinationPath);

            // If the destination is an existing folder, append the source item's name automatically
            if (Directory.Exists(destinationPath))
            {
                string itemName = Path.GetFileName(sourcePath);
                destinationPath = Path.Combine(destinationPath, itemName);
            }

            // Ensure the target directory structure exists
            string? destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            if (File.Exists(sourcePath))
                File.Move(sourcePath, destinationPath);
            else if (Directory.Exists(sourcePath))
                Directory.Move(sourcePath, destinationPath);
            else
            {
                if (Setting.AllowBeep)
                    Console.Beep();

                WriteLine($"[ERROR] Source path does not exist: {sourcePath}", ConsoleColor.Red);
            }
        }

        /// <summary>Renames a file or directory in place.</summary>
        /// <param name="sourcePath">The current path.</param>
        /// <param name="newName">The new name for the item.</param>
        public static void RenameFileSystemItem(string sourcePath, string newName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentNullException(nameof(sourcePath));

            string? directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            string sanitizedName = SanitizeString(newName);
            string destinationPath = Path.Combine(directory, sanitizedName);

            if (File.Exists(sourcePath))
                File.Move(sourcePath, destinationPath);
            else if (Directory.Exists(sourcePath))
                Directory.Move(sourcePath, destinationPath);
            else
            {
                if (Setting.AllowBeep)
                    Console.Beep();

                WriteLine($"[ERROR] Path does not exist: {sourcePath}", ConsoleColor.Red);
            }
        }

        /// <summary>Removes illegal characters from a filename string.</summary>
        /// <param name="filename">The input string.</param>
        /// <returns>A sanitized string valid for file systems.</returns>
        public static string SanitizeString(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return string.Empty;
            }

            // Matches < > : " / \ | ? * and control characters (0-31)
            string illegalChars = @"[<>:""/\\|?\*\x00-\x1F]";

            // Remove the illegal characters
            string cleanString = Regex.Replace(filename, illegalChars, string.Empty);

            // Remove trailing spaces and periods (Windows restriction)
            return cleanString.TrimEnd(' ', '.');
        }

        /// <summary>Extracts the parent directory from a path string.</summary>
        /// <param name="path">The full path.</param>
        /// <returns>The parent directory path.</returns>
        public static string GoToParentDirectory(string path)
        {
            List<string> pathParts = path.Split('\\').ToList();
            pathParts = GetParentDirectory(pathParts);
            return pathParts.First();

            List<string> GetParentDirectory(List<string> inParts)
            {
                if (inParts.Count - 1 > 1)
                {
                    inParts[0] += $"\\{inParts[1]}";
                    inParts.RemoveAt(1);
                    inParts = GetParentDirectory(inParts);
                }
                return inParts;
            }
        }

        /// <summary>Clears a specified number of lines upwards from the cursor.</summary>
        /// <param name="lineCount">Number of lines to clear.</param>
        /// <returns>Void.</returns>
        public static void ClearConsoleLines(int lineCount)
        {
            int currentTop = Console.CursorTop;

            for (int i = 1; i <= lineCount; i++)
            {
                int targetTop = currentTop - i;
                if (targetTop < 0) break;

                Console.SetCursorPosition(0, targetTop);
                // WindowWidth - 1 prevents auto-wrapping to the next row
                Write(new string(' ', Console.WindowWidth - 1));
            }

            // Reset cursor to the top of the cleared block
            int resetTop = Math.Max(0, currentTop - lineCount);
            Console.SetCursorPosition(0, resetTop);
        }

        /// <summary>Prompts user for confirmation.</summary>
        /// <param name="question">Prompt.</param> <param name="charAnswers">Valid keys.</param> <param name="warnings">Warnings.</param>
        /// <returns>True if first option chosen, false otherwise.</returns>
        public static bool GetUserConfirmation(string[] question, char[] charAnswers, string[] warnings)
        {
            if (question.Length != 2)
                throw new ArgumentException("Question array must contain exactly two elements: the question and the prompt.");
            if (charAnswers.Length != 2)
                throw new ArgumentException("charQuestion array must contain exactly two elements: the question and the prompt.");

            for (int i = 0; i < charAnswers.Length; i++)
            {
                if (charAnswers[i] == '\0')
                    throw new ArgumentException("charQuestion cannot contain null characters.");
                if (!char.IsLetterOrDigit(charAnswers[i]))
                    throw new ArgumentException("Each element in charQuestion must be a letter or digit.");
                if (charAnswers[i].ToString().Length != 1)
                    throw new ArgumentException("Each element in charQuestion must be a single character.");
                if (!char.IsUpper(charAnswers[i]))
                    charAnswers[i] = char.ToUpper(charAnswers[i]);
            }

            bool userInput;
            while (true)
            {
                if (warnings.Length > 0)
                {
                    for (int i = 0; i < warnings.Length; i++)
                    {
                        string warnMsg = warnings.Length > 1
                            ? $"WARNING ({i + 1}): {warnings[i]}"
                            : $"WARNING: {warnings[i]}";
                        WriteLine(warnMsg, ConsoleColor.Yellow);
                    }
                }

                WriteLine($"[{charAnswers.First()}] {question.First()}", ConsoleColor.Green);
                WriteLine($"[{charAnswers.Last()}] {question.Last()}", ConsoleColor.Green);

                Write("> ", ConsoleColor.Green);

                string input = "";

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (input.Length > 0)
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (Setting.AllowBeep)
                                Console.Beep();
                            Console.WriteLine();
                            break;
                        }
                    }
                    else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                    {
                        input = "";
                        Write("\b \b");
                    }
                    else if (input.Length == 0)
                    {
                        char pressedChar = char.ToUpper(key.KeyChar);
                        if (pressedChar == charAnswers.First().ToString().ToUpper().ToCharArray().First() ||
                            pressedChar == charAnswers.Last().ToString().ToUpper().ToCharArray().First())
                        {
                            input = pressedChar.ToString();
                            Write(input);
                        }
                    }
                }

                if (input.Equals(charAnswers.First().ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    userInput = true;
                    break;
                }
                else if (input.Equals(charAnswers.Last().ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    userInput = false;
                    break;
                }

                ClearConsoleLines(question.Length + warnings.Length);
            }
            return userInput;
        }

        /// <summary>Displays an asynchronous countdown in the console.</summary>
        /// <param name="delayMs">Delay.</param> <param name="allowManualBreak">Allow skip.</param>
        /// <param name="customMessage">Message.</param> <param name="showColors">Use colors.</param>
        private static readonly int DefaultDelayMs = 5000;
        private static readonly bool DefaultAllowManualBreak = true;
        private static readonly string DefaultCustomMessage = string.Empty;
        private static readonly bool DefaultShowColors = true;
        public static void Countdown()
        {
            Countdown(DefaultDelayMs, DefaultAllowManualBreak, DefaultCustomMessage, DefaultShowColors);
        }
        public static void Countdown(int delayMs)
        {
            Countdown(delayMs, DefaultAllowManualBreak, DefaultCustomMessage, DefaultShowColors);
        }
        public static void Countdown(int delayMs, string customMessage)
        {
            Countdown(delayMs, DefaultAllowManualBreak, customMessage, DefaultShowColors);
        }
        public static void Countdown(int delayMs, bool allowManualBreak)
        {
            Countdown(delayMs, allowManualBreak, DefaultCustomMessage, DefaultShowColors);
        }
        public static void Countdown(int delayMs, bool allowManualBreak, string customMessage)
        {
            Countdown(delayMs, allowManualBreak, customMessage, DefaultShowColors);
        }
        public static void Countdown(int delayMs, bool allowManualBreak, string customMessage, bool showColors)
        {
            Console.CursorVisible = false;

            if (allowManualBreak)
            {
                if (showColors)
                    WriteLine($"[COUNTDOWN] Press [ESC], [ENTER], [SPACEBAR], or [BACKSPACE] to break the countdown.", ConsoleColor.Yellow, !Setting.IsProgramInitialized);
                else
                    WriteLine($"[COUNTDOWN] Press [ESC], [ENTER], [SPACEBAR], or [BACKSPACE] to break the countdown.", !Setting.IsProgramInitialized);
            }

            if (!string.IsNullOrEmpty(customMessage?.Trim()))
            {
                if (showColors)
                    WriteLine(customMessage.Trim(), ConsoleColor.Cyan, !Setting.IsProgramInitialized);
                else
                    WriteLine(customMessage.Trim(), !Setting.IsProgramInitialized);
            }
            // Run the countdown task unconditionally. The break logic is handled inside.
            waitCountdown(delayMs).Wait();

            Console.CursorVisible = true;

            async Task waitCountdown(int delayMsCountdown)
            {
                DateTime targetTime = DateTime.UtcNow.AddMilliseconds(delayMsCountdown);

                while (true)
                {
                    TimeSpan remaining = targetTime - DateTime.UtcNow;

                    if (remaining.TotalMilliseconds <= 0)
                        break;

                    // If breaking is allowed, check the input buffer for the required keys
                    if (allowManualBreak)
                    {
                        bool breakRequested = false;

                        // Read all available keys in the buffer to prevent lag
                        while (Console.KeyAvailable)
                        {
                            // intercept: true prevents the typed key from rendering on the screen
                            ConsoleKey key = Console.ReadKey(intercept: true).Key;
                            if (key == ConsoleKey.Escape || key == ConsoleKey.Enter || key == ConsoleKey.Spacebar || key == ConsoleKey.Backspace)
                            {
                                breakRequested = true;
                                break; // Breaks the inner key-reading loop
                            }
                        }

                        if (breakRequested)
                            break; // Breaks the outer countdown loop entirely
                    }

                    string formattedTime = "";
                    if (remaining.Days > 0) formattedTime += $"{remaining.Days}d ";
                    if (remaining.Days > 0 || remaining.Hours > 0) formattedTime += $"{remaining.Hours}h ";
                    if (remaining.Days > 0 || remaining.Hours > 0 || remaining.Minutes > 0) formattedTime += $"{remaining.Minutes}m ";
                    if (remaining.Days > 0 || remaining.Hours > 0 || remaining.Minutes > 0 || remaining.Seconds > 0) formattedTime += $"{remaining.Seconds}s ";
                    formattedTime += $"{remaining.Milliseconds}ms";

                    ClearConsoleLines(0);
                    if (showColors)
                        Write($"[COUNTDOWN] Remaining time: {formattedTime}", ConsoleColor.Cyan, !Setting.IsProgramInitialized);
                    else
                        Write($"[COUNTDOWN] Remaining time: {formattedTime}", !Setting.IsProgramInitialized);

                    await Task.Delay(15);
                }
            }
        }
    }
}
