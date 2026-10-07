namespace Project.Core.Files
{
    public static class SizeExtensions
    {
        public static string ToReadableFileSize(this long fileSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = Convert.ToDouble(fileSize);
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        public static double ConvertBytesToMegabytes(this long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        public static double ConvertKilobytesToMegabytes(this long kilobytes)
        {
            return kilobytes / 1024f;
        }
    }
}