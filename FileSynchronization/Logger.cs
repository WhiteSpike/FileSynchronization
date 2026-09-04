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
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }
    }
}
