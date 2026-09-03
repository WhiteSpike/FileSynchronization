namespace FileSynchronization
{
    internal static class StringExtensions
    {
        public static string File(this string directory, string filename)
        {
            return $"{directory}\\{filename}";
        }
    }
}
