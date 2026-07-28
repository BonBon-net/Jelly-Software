using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jelly_Software
{
    public static class preBuildTools
    {
        public static void RenameFileOrFolder(string oldPath, string newPath)
        {
            string path = oldPath;
            string newName = newPath;
            string newpath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, newName);

            if (File.Exists(path))
            {
                File.Move(path, newpath);
            }
            else if (Directory.Exists(path))
            {
                Directory.Move(path, newpath);
            }
            else
            {
                Console.WriteLine($"Path does not exist: {path}");
            }
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
            for (int i = 0; i < lineCount; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new string(' ', Console.WindowWidth));
            }
            Console.SetCursorPosition(0, Console.CursorTop);
        }

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
                        if (warnings.Length > 1)
                            Console.WriteLine($"WARNING ({i + 1}): {warnings[i]}");
                        else
                            Console.WriteLine($"WARNING: {warnings[i]}");
                    }
                }

                Console.WriteLine($"[{charAnswers.First()}] {question.First()}");
                Console.WriteLine($"[{charAnswers.Last()}] {question.Last()}");
                Console.Write("> ");

                string input = "";

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (input.Length > 0)
                        {
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
    }
}