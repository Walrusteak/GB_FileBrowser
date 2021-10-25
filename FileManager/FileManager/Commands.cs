using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileManager
{
    public static class Commands
    {
        public static List<(string name, bool isDirectory)> List(string path)
        {
            List<(string, bool)> list = new();
            foreach (string dir in Directory.GetDirectories(path))
            {
                list.Add((new DirectoryInfo(dir).Name, true));
                list.AddRange(Directory.GetDirectories(dir).Select(d => ($"  {new DirectoryInfo(d).Name}", true)));
                list.AddRange(Directory.GetFiles(dir).Select(f => ($"  {Path.GetFileName(f)}", false)));
            }
            list.AddRange(Directory.GetFiles(path).Select(f => (Path.GetFileName(f), false)));
            return list;
        }

        public static void Copy(string source, string target) => File.Copy(source, target);

        public static void CopyDir(string source, string target)
        {
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(source, target));
            foreach (string file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(source, target), true);
        }

        public static void Delete(string path) => File.Delete(path);

        public static void DeleteDir(string path) => Directory.Delete(path, true);

        public static List<string> GetFileInfo(string path)
        {
            List<string> list = new();
            FileInfo fi = new(path);
            list.Add($"Создан: {fi.CreationTime}");
            list.Add($"Изменен: {fi.LastWriteTime}");
            list.Add($"Размер (КиБ): {fi.Length / 1024}");
            return list;
        }

        public static List<string> GetDirectoryInfo(string path)
        {
            List<string> list = new();
            DirectoryInfo di = new DirectoryInfo(path);
            list.Add($"Создан: {di.CreationTime}");
            list.Add($"Каталогов: {di.GetDirectories().Length}");
            list.Add($"Файлов: {di.GetFiles().Length}");
            list.Add($"Размер (КиБ): {DirSize(di) / 1024}");
            return list;
        }

        private static long DirSize(DirectoryInfo di)
        {
            long size = 0;
            foreach (DirectoryInfo dir in di.GetDirectories())
                size += DirSize(dir);
            foreach (FileInfo fi in di.GetFiles())
                size += fi.Length;
            return size;
        }
    }
}
