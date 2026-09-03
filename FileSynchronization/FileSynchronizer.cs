namespace FileSynchronization
{
    internal class FileSynchronizer
    {
        CommandLineArguments parameters;
        // TODO Keep track of operations required to synchronize

        public FileSynchronizer(CommandLineArguments parameters)
        {
            this.parameters = parameters;
        }

        internal void Synchronize()
        {
            // TODO Synchronize files from source to destination
            Console.WriteLine("Synchronized.");
        }

        internal int GetSynchronizationInterval()
        {
            return parameters.SynchronizationInterval;
        }
        internal string GetSourcePath()
        {
            return parameters.SourcePath;
        }
        internal string GetTargetPath()
        {
            return parameters.DestinationPath;
        }
        internal string GetLogFilePath()
        {
            return parameters.LogFilePath;
        }

        internal void Rest()
        {
            Thread.Sleep(GetSynchronizationInterval());
        }
    }
}
