namespace FileSynchronization
{
    internal static class StringExtensions
    {
        public static string File(this string directory, string filename)
        {
            return $"{directory}\\{filename}";
        }

        public static bool IsDirectory(this string path)
        {
            return (FileAttributes.Directory & System.IO.File.GetAttributes(path)) == FileAttributes.Directory;
        })
    }
}
