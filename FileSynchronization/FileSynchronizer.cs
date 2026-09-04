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
            InitialSynchronization();
        }

        void InitialSynchronization()
        {
            Console.WriteLine(LogMessages.INITIAL_SYNC_START);
            foreach (string sourceFilePath in Directory.GetFiles(parameters.SourcePath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(parameters.SourcePath, sourceFilePath);
                string destinationFilePath = Path.Combine(parameters.DestinationPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
            }
            Console.WriteLine(LogMessages.INITIAL_SYNC_END);
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

        void OnSourceRenamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine(string.Format(LogMessages.FILE_RENAMED, e.OldFullPath, e.FullPath));
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
            Console.WriteLine(string.Format(LogMessages.FILE_CREATED, e.FullPath));
            queueOperations.AddFirst(e);
        }

        void OnSourceChanged(object sender, FileSystemEventArgs e)
        {
            if ((File.GetAttributes(e.FullPath) & FileAttributes.Directory) == FileAttributes.Directory) return;
            Console.WriteLine(string.Format(LogMessages.FILE_CHANGED, e.FullPath));
            queueOperations.AddFirst(e);
        }

        void OnSourceDeleted(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine(string.Format(LogMessages.FILE_DELETED, e.FullPath));
            LinkedListNode<FileSystemEventArgs> currentNode = queueOperations.Last;
            while (currentNode != null)
            {
                FileSystemEventArgs current = currentNode.Value;
                if (current.Name != e.Name) continue;

                queueOperations.Remove(currentNode);
                currentNode = currentNode.Previous;
            }
            queueOperations.AddFirst(e);
        }

        internal void Synchronize()
        {
            Console.WriteLine(LogMessages.SYNC_START);
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
            Console.WriteLine(LogMessages.SYNC_END);
        }

        void CreateFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            if (operation.FullPath.IsDirectory())
            {
                Directory.CreateDirectory(destinationRootPath.File(operation.Name));
                return;
            }
            FileInfo fileInfo = new FileInfo(destinationRootPath.File(operation.Name));
            fileInfo.Directory.Create();
            File.Copy(operation.FullPath, destinationRootPath.File(operation.Name));
            Console.WriteLine(string.Format(LogMessages.SYNC_FILE_CREATED, operation.FullPath));
        }

        void ChangeFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            File.Copy(operation.FullPath, destinationRootPath.File(operation.Name), overwrite: true);
            Console.WriteLine(string.Format(LogMessages.SYNC_FILE_CHANGED, operation.FullPath));
        }

        void DeleteFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            if (destinationRootPath.File(operation.Name).IsDirectory())
            {
                Directory.Delete(destinationRootPath.File(operation.Name), recursive: true);
                return;
            }

            File.Delete(destinationRootPath.File(operation.Name));
            Console.WriteLine(string.Format(LogMessages.SYNC_FILE_DELETED, operation.FullPath));
        }

        void RenameFile(RenamedEventArgs operation, string destinationRootPath)
        {
            if (operation.FullPath.IsDirectory())
            {
                Directory.Move(destinationRootPath.File(operation.OldName), destinationRootPath.File(operation.Name));
                return;
            }

            FileInfo fileInfo = new FileInfo(destinationRootPath.File(operation.Name));
            fileInfo.Directory.Create(); // Create the subdirectories if necessary
            File.Move(destinationRootPath.File(operation.OldName), destinationRootPath.File(operation.Name));
            Console.WriteLine(string.Format(LogMessages.SYNC_FILE_RENAMED, operation.FullPath));
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
