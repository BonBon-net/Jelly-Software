using Jelly_Software.AppSettings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jelly_Software
{
    public static class preBuildTools
    {
        /// <summary>
        /// Helper to print colored console lines respecting user 'AllowColors' setting.
        /// </summary>
        public static void WriteLineColored(string text, ConsoleColor color, bool forceColor = false)
        {
            if (forceColor || _AppDatabase.ProgramSettings.AllowColors)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(text);
            }
        }

        /// <summary>
        /// Helper to print colored console inline text respecting user 'AllowColors' setting.
        /// </summary>
        public static void WriteColored(string text, ConsoleColor color)
        {
            if (_AppDatabase.ProgramSettings.AllowColors)
            {
                Console.ForegroundColor = color;
                Console.Write(text);
                Console.ResetColor();
            }
            else
            {
                Console.Write(text);
            }
        }

        public static void RenameFileOrFolder(string oldPath, string newPath)
        {
            string path = oldPath;
            string newName = newPath;
            string newpath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, newName);

            newName = $"{GoToParentDirectory(newpath)}\\{SanitizeFilename(newpath.Split("\\").Last())}";

            if (File.Exists(path))
            {
                File.Move(path, newName);
            }
            else if (Directory.Exists(path))
            {
                Directory.Move(path, newName);
            }
            else
            {
                // Dev Note: Check dev configuration before issuing sound alert
                if (_AppDatabase.ProgramSettings.AllowBeep)
                    Console.Beep();
                WriteLineColored($"[ERROR] Path does not exist: {path}", ConsoleColor.Red);
            }
        }

        public static string SanitizeFilename(string filename)
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

        public static void ClearConsoleLines(int lineCount)
        {
            int currentTop = Console.CursorTop;

            for (int i = 1; i <= lineCount; i++)
            {
                int targetTop = currentTop - i;
                if (targetTop < 0) break;

                Console.SetCursorPosition(0, targetTop);
                // WindowWidth - 1 prevents auto-wrapping to the next row
                Console.Write(new string(' ', Console.WindowWidth - 1));
            }

            // Reset cursor to the top of the cleared block
            int resetTop = Math.Max(0, currentTop - lineCount);
            Console.SetCursorPosition(0, resetTop);
        }

        //","explanation":"Avoid ArgumentOutOfRangeException by not setting cursor to a negative top; clear current line when CursorTop is 0."}`
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
                        WriteLineColored(warnMsg, ConsoleColor.Yellow);
                    }
                }

                WriteLineColored($"[{charAnswers.First()}] {question.First()}", ConsoleColor.Green);
                WriteLineColored($"[{charAnswers.Last()}] {question.Last()}", ConsoleColor.Green);

                WriteColored("> ", ConsoleColor.Green);

                string input = "";

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (input.Length > 0)
                        {
                            // Dev Note: Check dev configuration before issuing sound alert
                            if (_AppDatabase.ProgramSettings.AllowBeep)
                                Console.Beep();
                            Console.WriteLine();
                            break;
                        }
                    }
                    else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                    {
                        input = "";
                        Console.Write("\b \b");
                    }
                    else if (input.Length == 0)
                    {
                        char pressedChar = char.ToUpper(key.KeyChar);
                        if (pressedChar == charAnswers.First().ToString().ToUpper().ToCharArray().First() ||
                            pressedChar == charAnswers.Last().ToString().ToUpper().ToCharArray().First())
                        {
                            input = pressedChar.ToString();
                            Console.Write(input);
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

        public static void Countdown(int delayMs, bool allowBreak)
        {
            Console.CursorVisible = false;

            if (allowBreak)
                WriteLineColored($"[COUNTDOWN] Press [ESC], [ENTER], [SPACEBAR], or [BACKSPACE] to break the countdown.", ConsoleColor.Yellow);
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
                    if (allowBreak)
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
                    WriteColored($"[COUNTDOWN] Remaining time: {formattedTime}", ConsoleColor.Cyan);

                    await Task.Delay(15);
                }
            }
        }
    }
}
