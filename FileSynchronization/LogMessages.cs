using System;
using System.Collections.Generic;
using System.Text;

namespace FileSynchronization
{
    internal static class LogMessages
    {
        internal const string SYNC_START = "Starting synchronization proccess.";
        internal const string SYNC_END = "Synchronization proccess completed.";

        internal const string INITIAL_SYNC_START = "Starting initial synchronization proccess.";
        internal const string INITIAL_SYNC_END = "Initial synchronization proccess completed.";

        internal const string FILE_CREATED = "File created: {0}";
        internal const string FILE_CHANGED = "File changed: {0}";
        internal const string FILE_DELETED = "File deleted: {0}";
        internal const string FILE_RENAMED = "File renamed from {0} to {1}";

        internal const string SYNC_FILE_CREATED = "File create synchronization: {0}";
        internal const string SYNC_FILE_CHANGED = "File change synchronization: {0}";
        internal const string SYNC_FILE_DELETED = "File delete synchronization: {0}";
        internal const string SYNC_FILE_RENAMED = "File rename synchronization: from {0} to {1}";

        internal const string WRONG_ARGUMENTS_ERROR = "Error: Provided arguments were not valid.\n Use the following format:\n \"FileSynchronization <sourcePath> <replicaPath> <synchronizationInterval> <logFilePath>\"";
        internal const string SOURCE_PATH_ERROR = "Error: Provided source directory folder path ({0}) does not exist.";
        internal const string DESTINATION_PATH_INFO = "Info: Provided destination directory folder path ({0}) does not exist. Creating it now.";
        internal const string INTERVAL_ERROR = "Error: Provided synchronization interval ({0}) is not a valid integer.";
        internal const string INTERVAL_NEGATIVE_ERROR = "Error: Provided synchronization interval ({0}) must be a positive integer.";
        internal const string LOG_FILE_PATH_ERROR = "Error: Provided log file path ({0}) is not valid.";
        internal const string LOG_FILE_PATH_INFO = "Info: Directories in the provided log file path do not exist. Creating them now.";
        internal const string ARGUMENTS_RECEIVED_INFO = "Arguments received from command line, starting synchronizer process...";
    }
}
