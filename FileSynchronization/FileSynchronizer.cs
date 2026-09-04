namespace FileSynchronization
{
    internal class FileSynchronizer
    {
        CommandLineArguments parameters;
        FileSystemWatcher sourceWatcher;
        LinkedList<FileSystemEventArgs> queueOperations;
        Logger logger;

        public FileSynchronizer(CommandLineArguments parameters)
        {
            this.parameters = parameters;
            this.queueOperations = new LinkedList<FileSystemEventArgs>();
            this.logger = new Logger(parameters.LogFilePath);
            SetupSourceWatcher();
            InitialSynchronization();
            logger.Log(LogMessages.SYNCHRONIZER_INITIALIZED);
        }

        void InitialSynchronization()
        {
            logger.Log(LogMessages.INITIAL_SYNC_START);
            CopyAllFilesInDirectory(parameters.SourcePath, parameters.DestinationPath);
            logger.Log(LogMessages.INITIAL_SYNC_END);
        }

        void CopyAllFilesInDirectory(string sourceDirectory, string destinationDirectory)
        {
            foreach (string sourceFilePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDirectory, sourceFilePath);
                string destinationFilePath = Path.Combine(destinationDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
            }
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
            sourceWatcher.Error += OnSourceError;
            sourceWatcher.IncludeSubdirectories = true;
            sourceWatcher.EnableRaisingEvents = true;
        }   
        void OnSourceError(object sender, ErrorEventArgs e)
        {
            logger.Log(e.GetException().Message);
        }

        void OnSourceRenamed(object sender, RenamedEventArgs e)
        {
            logger.Log(string.Format(LogMessages.FILE_RENAMED, e.OldFullPath, e.FullPath));
            LinkedListNode<FileSystemEventArgs> currentNode = queueOperations.Last;
            bool foundCreate = false;
            bool foundRenamed = false;
            while (currentNode != null)
            {
                LinkedListNode<FileSystemEventArgs> it = currentNode;
                FileSystemEventArgs current = currentNode.Value;
                currentNode = currentNode.Previous;
                if (current.Name != e.OldName) continue;
                    switch (current.ChangeType)
                {
                    default:
                        {
                            it.Value = new FileSystemEventArgs(current.ChangeType, GetSourcePath(), e.Name);
                            if (current.ChangeType == WatcherChangeTypes.Created) foundCreate = true;
                            break;
                        }
                    case WatcherChangeTypes.Renamed:
                        {
                            RenamedEventArgs currentRenamed = current as RenamedEventArgs;
                            it.Value = new RenamedEventArgs(current.ChangeType, GetSourcePath(), e.Name, currentRenamed.OldName);
                            foundRenamed = true;
                            break;
                        }

                }
            }
            if (!foundCreate && !foundRenamed) queueOperations.AddFirst(e);
        }

        void OnSourceCreated(object sender, FileSystemEventArgs e)
        {
            logger.Log(string.Format(LogMessages.FILE_CREATED, e.FullPath));
            queueOperations.AddFirst(e);
        }

        void OnSourceChanged(object sender, FileSystemEventArgs e)
        {
            logger.Log(string.Format(LogMessages.FILE_CHANGED, e.FullPath));
            queueOperations.AddFirst(e);
        }

        void OnSourceDeleted(object sender, FileSystemEventArgs e)
        {
            logger.Log(string.Format(LogMessages.FILE_DELETED, e.FullPath));
            LinkedListNode<FileSystemEventArgs> currentNode = queueOperations.Last;
            while (currentNode != null)
            {
                LinkedListNode<FileSystemEventArgs> it = currentNode;
                FileSystemEventArgs current = currentNode.Value;
                currentNode = currentNode.Previous;
                if (current.Name != e.Name) continue;

                queueOperations.Remove(it);
            }
            queueOperations.AddFirst(e);
        }

        internal void Synchronize()
        {
            logger.Log(LogMessages.SYNC_START);
            while (queueOperations.Count > 0)
            {
                FileSystemEventArgs operation = queueOperations.Last.Value;
                queueOperations.RemoveLast();
                switch (operation.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        CreateFile(operation, parameters.DestinationPath);
                        break;
                    case WatcherChangeTypes.Changed:
                        ChangeFile(operation, parameters.DestinationPath);
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
            logger.Log(LogMessages.SYNC_END);
        }

        void CreateFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            if (operation.FullPath.IsDirectory())
            {
                Directory.CreateDirectory(destinationRootPath.File(operation.Name));
                CopyAllFilesInDirectory(operation.FullPath, destinationRootPath.File(operation.Name));
                logger.Log(string.Format(LogMessages.SYNC_FOLDER_CREATED, operation.FullPath));
                return;
            }
            FileInfo fileInfo = new FileInfo(destinationRootPath.File(operation.Name));
            fileInfo.Directory.Create();
            File.Copy(operation.FullPath, destinationRootPath.File(operation.Name));
            logger.Log(string.Format(LogMessages.SYNC_FILE_CREATED, operation.FullPath));
        }

        void ChangeFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            File.Copy(operation.FullPath, destinationRootPath.File(operation.Name), overwrite: true);
            logger.Log(string.Format(LogMessages.SYNC_FILE_CHANGED, operation.FullPath));
        }

        void DeleteFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            if (destinationRootPath.File(operation.Name).IsDirectory())
            {
                Directory.Delete(destinationRootPath.File(operation.Name), recursive: true);
                logger.Log(string.Format(LogMessages.SYNC_FOLDER_DELETED, operation.FullPath));
                return;
            }

            File.Delete(destinationRootPath.File(operation.Name));
            logger.Log(string.Format(LogMessages.SYNC_FILE_DELETED, operation.FullPath));
        }

        void RenameFile(RenamedEventArgs operation, string destinationRootPath)
        {
            if (operation.FullPath.IsDirectory())
            {
                Directory.Move(destinationRootPath.File(operation.OldName), destinationRootPath.File(operation.Name));
                logger.Log(string.Format(LogMessages.SYNC_FOLDER_RENAMED, operation.OldFullPath, operation.FullPath));
                return;
            }

            FileInfo fileInfo = new FileInfo(destinationRootPath.File(operation.Name));
            fileInfo.Directory.Create(); // Create the subdirectories if necessary
            File.Move(destinationRootPath.File(operation.OldName), destinationRootPath.File(operation.Name));
            logger.Log(string.Format(LogMessages.SYNC_FILE_RENAMED, operation.OldFullPath, operation.FullPath));
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
