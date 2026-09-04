namespace FileSynchronization
{
    internal class CommandLineArguments
    {
        const string PATH_ERROR = "";

        const short SOURCE_FOLDER_INDEX = 0;
        const short TARGET_FOLDER_INDEX = 1;
        const short SYNCHRONIZATION_INTERVAL_INDEX = 2;
        const short LOG_FILE_PATH_INDEX = 3;

        internal string SourcePath;
        internal string DestinationPath;
        internal int SynchronizationInterval;
        internal string LogFilePath;

        public CommandLineArguments(string[] args)
        {
            if (args.Length < 4)
            {
                SourcePath = PATH_ERROR;
                DestinationPath = PATH_ERROR;
                SynchronizationInterval = -1;
                LogFilePath = PATH_ERROR;
            }
            else
            {
                SourcePath = ValidateSource(args[SOURCE_FOLDER_INDEX]);
                DestinationPath = ValidateDestination(args[TARGET_FOLDER_INDEX]);
                SynchronizationInterval = ValidateInterval(args[SYNCHRONIZATION_INTERVAL_INDEX]);
                LogFilePath = ValidateLogFilePath(args[LOG_FILE_PATH_INDEX]);
            }
        }
        internal bool IsValid()
        {
            return !string.IsNullOrEmpty(SourcePath) &&
                !string.IsNullOrEmpty(DestinationPath) &&
                SynchronizationInterval > 0 &&
                !string.IsNullOrEmpty(LogFilePath);
        }
        string ValidateSource(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.Error.WriteLine(string.Format(LogMessages.SOURCE_PATH_ERROR, directoryPath));
                return PATH_ERROR;
            }
            return directoryPath;
        }
        string ValidateDestination(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine(string.Format(LogMessages.DESTINATION_PATH_INFO, directoryPath));
                Directory.CreateDirectory(directoryPath);
            }
            return directoryPath;
        }
        int ValidateInterval(string interval)
        {
            if (!int.TryParse(interval, out int result))
            {
                Console.Error.WriteLine(string.Format(LogMessages.INTERVAL_ERROR, interval));
                return -1;
            }
            if (result <= 0)
            {
                Console.Error.WriteLine(string.Format(LogMessages.INTERVAL_NEGATIVE_ERROR, interval));
                return -1;
            }
            return result * 1000; // Convert seconds to milliseconds
        }

        string ValidateLogFilePath(string logFilePath)
        {
            FileInfo logFileInfo = new FileInfo(logFilePath);
            if (!logFileInfo.Directory.Exists)
            {
                Console.WriteLine(string.Format(LogMessages.LOG_FILE_PATH_INFO, logFilePath));
                logFileInfo.Directory.Create();
            }
            return logFilePath;
        }
    }
}
