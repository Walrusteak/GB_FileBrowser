using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using static System.Console;

namespace FileManager
{
    class Program
    {
        private static bool error;
        static int currentPage;
        static int hix = 0;
        static string text;
        static string directory;

        static List<string> history;
        static List<(string name, bool isDirectory)> list;

        private static int TotalPages => (int)Math.Ceiling((float)list.Count / Interface.ListAreaHeight);

        static void Main(string[] args)
        {
            history = new List<string> { "" };
            Reset();
            RestoreCurrentDirectory();
            while (true)
                ProcessInput();
        }

        private static Configuration GetUserConfig()
        {
            Configuration roaming = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap { ExeConfigFilename = roaming.FilePath };
            return ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
        }

        private static void SaveCurrentDirectory()
        {
            Configuration config = GetUserConfig();
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;
            if (settings["Directory"] == null)
                settings.Add("Directory", directory);
            else
                settings["Directory"].Value = directory;
            config.Save();
        }

        private static void RestoreCurrentDirectory()
        {
            Configuration config = GetUserConfig();
            KeyValueConfigurationCollection settings = config.AppSettings.Settings;
            directory = settings["Directory"]?.Value;
            if (directory != null)
                GetList(directory);
        }

        static void Reset()
        {
            text = "";
            Clear();
            Interface.GetWindowSize();
            Interface.PrintBounds();
            Interface.PrepareCommandField();
        }

        static void ProcessInput()
        {
            ConsoleKeyInfo key = ReadKey();
            switch (key.Key)
            {
                case ConsoleKey.P:
                    if (key.Modifiers == ConsoleModifiers.Control)
                        PrintPrevPage();
                    else
                        text += key.KeyChar;
                    break;
                case ConsoleKey.N:
                    if (key.Modifiers == ConsoleModifiers.Control)
                        PrintNextPage();
                    else
                        text += key.KeyChar;
                    break;
                case ConsoleKey.R:
                    if (key.Modifiers == ConsoleModifiers.Control)
                        Reset();
                    else
                        text += key.KeyChar;
                    break;
                case ConsoleKey.UpArrow:
                    if (text == "" || text == history[hix])
                    {
                        if (++hix > history.Count - 1)
                            hix = 0;
                        text = history[hix];
                        Interface.PrepareCommandField();
                        Write(text);
                    }
                    break;
                case ConsoleKey.DownArrow:
                    if (text == "" || text == history[hix])
                    {
                        if (--hix < 0)
                            hix = history.Count - 1;
                        text = history[hix];
                        Interface.PrepareCommandField();
                        Write(text);
                    }
                    break;
                case ConsoleKey.Enter:
                    if (history.FirstOrDefault() != text)
                    {
                        history.Insert(0, text);
                        hix = 0;
                    }
                    ParseCommand(text);
                    text = "";
                    break;
                case ConsoleKey.Backspace:
                    if (text != "")
                    {
                        text = text.Remove(text.Length - 1);
                        Interface.PrepareCommandField();
                        Write(text);
                    }
                    else
                        Interface.PrepareCommandField();
                    break;
                default:
                    text += key.KeyChar;
                    break;
            }
        }

        public static void ParseCommand(string command)
        {
            string[] split = command.Split(' ', 2, StringSplitOptions.TrimEntries);
            string cmd = split[0].ToLower();
            List<string> args = split.Length > 1 ? ParseArgs(split[1]) : null;
            switch (cmd)
            {
                case "exit":
                    Clear();
                    SaveCurrentDirectory();
                    Environment.Exit(0);
                    break;
                case "help":
                    Help();
                    break;
                case "cls": //clear screen
                    Reset();
                    break;
                case "ls":
                    GetList(args);
                    break;
                case "page":
                    PrintPage(args);
                    break;
                case "fi":
                    PrintFileInfo(args);
                    break;
                case "di":
                    PrintDirInfo(args);
                    break;
                case "cp":
                    Copy(args);
                    break;
                case "cpdir":
                    CopyDir(args);
                    break;
                case "rm":
                    Delete(args);
                    break;
                case "rmdir":
                    DeleteDir(args);
                    break;
                default:
                    PrintError("Неизвестная команда, введите help для вывода справки");
                    break;
            }
            Interface.PrepareCommandField();
        }

        private static List<string> ParseArgs(string args)
        {
            List<string> list = new();
            System.Text.StringBuilder sb = new();
            string s;
            bool quote = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == '"' && (i == 0 || args[i - 1] != '\\'))
                    quote = !quote;
                else if (quote || args[i] != ' ')
                    sb.Append(args[i]);
                else
                {
                    s = sb.ToString();
                    if (s != "")
                        list.Add(sb.ToString());
                    sb.Clear();
                }
            }
            s = sb.ToString();
            if (s.Length != 0 && s != "")
                list.Add(s);
            return list;
        }

        private static void Help()
        {
            list = new();
            list.Add(("ls <путь> - просмотр содержимого каталога", false));
            list.Add(("fi <путь> - просмотр информации о файле", false));
            list.Add(("di <путь> - просмотр информации о каталоге", false));
            list.Add(("cp <путь из> <путь в> - копировать файл", false));
            list.Add(("cpdir <путь из> <путь в> - копировать каталог", false));
            list.Add(("rm <путь> - удалить файл", false));
            list.Add(("rmdir <путь> - удалить каталог", false));
            list.Add(("cls (Ctrl+R) - перезагрузить размеры консоли и очистить экран", false));
            list.Add(("page <номер> - открыть страницу", false));
            list.Add(("Ctrl+N - следующая страница", false));
            list.Add(("Ctrl+P - предыдущая страница", false));
            list.Add(("help - вывод справки", false));
            list.Add(("exit - выход", false));
            currentPage = 1;
            PrintList(0);
        }

        private static void GetList(string path)
        {
            try
            {
                list = Commands.List(path);
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                list = null;
                Interface.ClearList();
                directory = null;
                return;
            }
            directory = path;
            currentPage = 1;
            PrintList(0);
        }

        private static void GetList(List<string> args)
        {
            if (!args?.Any() ?? true)
            {
                PrintError("Недостаточно аргументов");
                return;
            }

            GetList(args[0]);
        }

        private static void PrintList(int part)
        {
            if (error)
            {
                Interface.ClearInfo();
                error = false;
            }

            Interface.ClearList();
            Interface.PrintPageNumber(currentPage, TotalPages);
            int y = 1;
            int yMax = Interface.ListAreaHeight + y;
            for (int i = part * Interface.ListAreaHeight; y < yMax && i < list.Count; i++)
            {
                SetCursorPosition(1, y++);
                if (list[i].isDirectory)
                    ForegroundColor = ConsoleColor.Green;
                Write(ClampString(list[i].name));
                ResetColor();
            }
            Interface.PrepareCommandField();
        }

        private static void PrintPage(List<string> args)
        {
            if (list == null)
            {
                PrintError("Список отсутствует");
                return;
            }
            if (!args?.Any() ?? true)
            {
                PrintError("Недостаточно аргументов");
                return;
            }
            if (!Int32.TryParse(args[0], out int page))
            {
                PrintError("Неверный аргумент");
                return;
            }
            if (page < 1)
            {
                PrintError("Номер страницы не может быть меньше 1");
                return;
            }
            if (page > TotalPages)
            {
                PrintError($"Номер страницы не может быть больше {TotalPages}");
                return;
            }
            currentPage = page;
            PrintCurrentPage();
        }

        private static void PrintPrevPage()
        {
            if (!list?.Any() ?? true)
                return;
            if (currentPage > 1)
                currentPage--;
            PrintCurrentPage();
        }

        private static void PrintNextPage()
        {
            if (!list?.Any() ?? true)
                return;
            if (currentPage < TotalPages)
                currentPage++;
            PrintCurrentPage();
        }

        private static void PrintCurrentPage() => PrintList(currentPage - 1);

        private static void PrintFileInfo(List<string> args)
        {
            if (!args?.Any() ?? true)
            {
                PrintError("Недостаточно аргументов");
                return;
            }

            List<string> fileInfo;
            try
            {
                fileInfo = Commands.GetFileInfo(args[0]);
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                return;
            }

            Interface.ClearInfo();
            int y = Interface.InfoBlockY;
            foreach (string row in fileInfo)
            {
                SetCursorPosition(1, y++);
                Write(ClampString(row));
            }
        }

        private static void PrintDirInfo(List<string> args)
        {
            if (!args?.Any() ?? true)
            {
                PrintError("Недостаточно аргументов");
                return;
            }

            List<string> dirInfo;
            try
            {
                dirInfo = Commands.GetDirectoryInfo(args[0]);
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                return;
            }

            Interface.ClearInfo();
            int y = Interface.InfoBlockY;
            foreach (string row in dirInfo)
            {
                SetCursorPosition(1, y++);
                Write(ClampString(row));
            }
        }

        private static void Copy(List<string> args)
        {
            if (args == null || args.Count < 2)
            {
                PrintError("Недостаточно аргументов");
                return;
            }

            try
            {
                Commands.Copy(args[0], args[1]);
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                return;
            }
        }

        private static void CopyDir(List<string> args)
        {
            if (args == null || args.Count < 2)
            {
                PrintError("Недостаточно аргументов");
                return;
            }

            try
            {
                Commands.CopyDir(args[0], args[1]);
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                return;
            }
        }

        private static void Delete(List<string> args)
        {
            if (!args?.Any() ?? true)
            {
                PrintError("Недостаточно аргументов");
                return;
            }

            try
            {
                Commands.Delete(args[0]);
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                return;
            }
        }

        private static void DeleteDir(List<string> args)
        {
            if (!args?.Any() ?? true)
            {
                PrintError("Недостаточно аргументов");
                return;
            }

            try
            {
                Commands.DeleteDir(args[0]);
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
                return;
            }
        }

        private static void PrintError(string str)
        {
            error = true;
            Interface.ClearInfo();
            SetCursorPosition(1, Interface.InfoBlockY);
            Write("Ошибка");
            SetCursorPosition(1, Interface.InfoBlockY + 1);
            Write(ClampString(str));
            SaveErrorToFile(str);
        }

        private static string ClampString(string str) => str.Substring(0, Math.Min(str.Length, Interface.AreaWidth));

        private static void SaveErrorToFile(string message)
        {
            Directory.CreateDirectory("errors");
            File.AppendAllText(Path.Combine("errors", "exception.txt"), message + Environment.NewLine);
        }
    }
}
