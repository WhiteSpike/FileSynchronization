namespace FileSynchronization
{
    internal class Logger
    {
        string logFilePath;

        public Logger(string logFilePath)
        {   
            this.logFilePath = logFilePath;
        }

        internal void Log(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            Console.WriteLine(logEntry);
            using (StreamWriter writer = new(logFilePath, append: true))
            {
                writer.WriteLine(logEntry);
            }
        }
    }
}
