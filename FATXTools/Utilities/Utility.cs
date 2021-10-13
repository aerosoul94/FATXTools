using System.IO;

namespace FATXTools.Utilities
{
    public static class Utility
    {
        public static string[] Suffix = { "B", "KB", "MB", "GB", "TB" };

        public static string FormatBytes(long bytes)
        {
            int i;
            double dblSByte = bytes;

            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return string.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        public static string UniqueFileName(string path, int maxAttempts = 256)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            var fileDirectory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var fileBaseName = Path.GetFileNameWithoutExtension(fileName);
            var fileExt = Path.GetExtension(fileName);

            for (var i = 1; i <= maxAttempts; i++)
            {
                var testPath = $"{fileDirectory}\\{fileBaseName} ({i}){fileExt}";

                if (!File.Exists(testPath))
                {
                    return testPath;
                }
            }

            return null;
        }
    }
}
