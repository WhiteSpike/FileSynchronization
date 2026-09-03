namespace FileSynchronization
{
    internal class FileSynchronizer
    {
        CommandLineArguments parameters;
        FileSystemWatcher sourceWatcher;
        LinkedList<FileSystemEventArgs> queueOperations;

        public FileSynchronizer(CommandLineArguments parameters)
        {
            this.parameters = parameters;
            this.queueOperations = new LinkedList<FileSystemEventArgs>();
            SetupSourceWatcher();
        }

        void SetupSourceWatcher()
        {
            this.sourceWatcher = new(parameters.SourcePath);

            sourceWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
            sourceWatcher.Created += OnSourceCreated;
            sourceWatcher.Changed += OnSourceChanged;
            sourceWatcher.Deleted += OnSourceDeleted;
            sourceWatcher.Renamed += OnSourceRenamed;
            sourceWatcher.IncludeSubdirectories = true;
            sourceWatcher.EnableRaisingEvents = true;
        }   

        void OnSourceChanged(object sender, FileSystemEventArgs e)
        {
            queueOperations.AddFirst(e);
            Console.WriteLine($"Source file {e.ChangeType}: {e.FullPath}");
        }

        void OnSourceRenamed(object sender, RenamedEventArgs e)
        {
            queueOperations.AddFirst(e);
            Console.WriteLine($"Source file renamed from {e.OldFullPath} to {e.FullPath}");
        }

        void OnSourceCreated(object sender, FileSystemEventArgs e)
        {
            queueOperations.AddFirst(e);
            Console.WriteLine($"Source file created: {e.FullPath}");
        }

        void OnSourceDeleted(object sender, FileSystemEventArgs e)
        {
            queueOperations.AddFirst(e);
            Console.WriteLine($"Source file deleted: {e.FullPath}");
        }

        internal void Synchronize()
        {
            while(queueOperations.Count > 0)
            {
                FileSystemEventArgs operation = queueOperations.Last.Value;
                queueOperations.RemoveLast();
                switch (operation.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        CreateFile(operation, parameters.DestinationPath);
                        break;
                    case WatcherChangeTypes.Changed:
                        CopyFile(operation, parameters.DestinationPath);
                        break;
                    case WatcherChangeTypes.Deleted:
                        DeleteFile(operation, parameters.DestinationPath);
                        break;
                    case WatcherChangeTypes.Renamed:
                        RenamedEventArgs renamedOperation = operation as RenamedEventArgs;
                        if (renamedOperation != null)
                        {
                            RenameFile(renamedOperation, parameters.DestinationPath);
                        }
                        break;
                }
            }
            Console.WriteLine("Synchronized.");
        }

        void CreateFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            File.Copy(operation.FullPath, destinationRootPath.File(operation.Name));
        }

        void CopyFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            File.Copy(operation.FullPath, destinationRootPath.File(operation.Name), overwrite: true);
        }

        void DeleteFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            File.Delete(destinationRootPath.File(operation.Name));
        }

        void RenameFile(RenamedEventArgs operation, string destinationRootPath)
        {
            File.Move(destinationRootPath.File(operation.OldName), destinationRootPath.File(operation.Name));
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
