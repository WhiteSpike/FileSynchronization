namespace FileSynchronization
{
    /// <summary>
    /// Performs the file synchronization between the source and target directories by monitoring the state changes in the source and replicating it in the target directory.
    /// </summary>
    internal class FileSynchronizer
    {
        /// <summary>
        /// The command line arguments provided to the application
        /// </summary>
        CommandLineArguments parameters;
        /// <summary>
        /// The monitor used to keep track of the state changes in the source directory
        /// </summary>
        FileSystemWatcher sourceWatcher;
        /// <summary>
        /// The collection of operations performed in the source directory that need to be replicated in the target directory
        /// </summary>
        LinkedList<FileSystemEventArgs> queueOperations;
        Logger logger;

        /// <summary>
        /// Default constructor of the FileSynchronizer class. Initializes the source watcher, performs the initial synchronization, and sets up the logger.
        /// </summary>
        /// <param name="parameters"></param>
        public FileSynchronizer(CommandLineArguments parameters)
        {
            this.parameters = parameters;
            this.queueOperations = new LinkedList<FileSystemEventArgs>();
            this.logger = new Logger(parameters.LogFilePath);
            SetupSourceWatcher();
            InitialSynchronization();
            logger.Log(LogMessages.SYNCHRONIZER_INITIALIZED);
        }
        /// <summary>
        /// Copies the initial state of the source directory into the target directory and have both directories with the same content.
        /// </summary>
        void InitialSynchronization()
        {
            logger.Log(LogMessages.INITIAL_SYNC_START);
            CopyAllFilesInDirectory(parameters.SourcePath, parameters.DestinationPath);
            logger.Log(LogMessages.INITIAL_SYNC_END);
        }
        /// <summary>
        /// Copies all contents of the specified source directory to the specified destination directory, including all subdirectories and files.
        /// </summary>
        /// <param name="sourceDirectory">File path of the directory we are copying the content from</param>
        /// <param name="destinationDirectory">File path of the directory we are copying the content to</param>
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
        /// <summary>
        /// Setup function for the state change monitor of the source directory.
        /// </summary>
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
        /// <summary>
        /// Event handler for OS errors that may occur while monitoring the source directory.
        /// </summary>
        /// <param name="sender">Object that sent the error event</param>
        /// <param name="e">The event received from the monitor</param>
        void OnSourceError(object sender, ErrorEventArgs e)
        {
            logger.Log(e.GetException().Message);
        }
        /// <summary>
        /// Event handler for when a file or directory is renamed in the source directory.
        /// </summary>
        /// <param name="sender">Object that sent the error event</param>
        /// <param name="e">The event received from the monitor</param>
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
        /// <summary>
        /// Event handler for when a file or directory is created in the source directory.
        /// Note: A move operation is treated as a delete followed by a create, so this event will be triggered when a file or directory is moved out of the source directory.
        /// </summary>
        /// <param name="sender">Object that sent the error event</param>
        /// <param name="e">The event received from the monitor</param>
        void OnSourceCreated(object sender, FileSystemEventArgs e)
        {
            logger.Log(string.Format(LogMessages.FILE_CREATED, e.FullPath));
            queueOperations.AddFirst(e);
        }
        /// <summary>
        /// Event handler for when a file or directory is changed (like size, attributes, etc.) in the source directory.
        /// </summary>
        /// <param name="sender">Object that sent the error event</param>
        /// <param name="e">The event received from the monitor</param>
        void OnSourceChanged(object sender, FileSystemEventArgs e)
        {
            logger.Log(string.Format(LogMessages.FILE_CHANGED, e.FullPath));
            queueOperations.AddFirst(e);
        }
        /// <summary>
        /// Event handler for when a file or directory is deleted in the source directory.
        /// Note: A move operation is treated as a delete followed by a create, so this event will be triggered when a file or directory is moved out of the source directory.
        /// </summary>
        /// <param name="sender">Object that sent the error event</param>
        /// <param name="e">The event received from the monitor</param>
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
        /// <summary>
        /// Main function that performs the period synchronization between the source folder and the target folder by executing all state changing operations from the source folder.
        /// </summary>
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
        /// <summary>
        /// Main function handler that performs the file or directory creation operation in the target folder
        /// </summary>
        /// <param name="operation">Create operation executed in the source folder</param>
        /// <param name="destinationRootPath">Path to the target directory</param>
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

        /// <summary>
        /// Main function handler that performs the file or directory change operation in the target folder
        /// </summary>
        /// <param name="operation">Change operation executed in the source folder</param>
        /// <param name="destinationRootPath">Path to the target directory</param>
        void ChangeFile(FileSystemEventArgs operation, string destinationRootPath)
        {
            if (operation.FullPath.IsDirectory()) return;
            File.Copy(operation.FullPath, destinationRootPath.File(operation.Name), overwrite: true);
            logger.Log(string.Format(LogMessages.SYNC_FILE_CHANGED, operation.FullPath));
        }

        /// <summary>
        /// Main function handler that performs the file or directory deletion operation in the target folder
        /// </summary>
        /// <param name="operation">Deletion operation executed in the source folder</param>
        /// <param name="destinationRootPath">Path to the target directory</param>
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

        /// <summary>
        /// Main function handler that performs the file or directory rename operation in the target folder
        /// </summary>
        /// <param name="operation">Rename operation executed in the source folder</param>
        /// <param name="destinationRootPath">Path to the target directory</param>
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
        /// <summary>
        /// Function that makes the synchronizer sleep for the specified synchronization interval before performing the next synchronization.
        /// </summary>
        internal void Rest()
        {
            Thread.Sleep(GetSynchronizationInterval());
        }
    }
}
