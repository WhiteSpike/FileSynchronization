# FileSynchronization
A C# program that synchronizes between a source folder and a replica folder on a set time interval.

# What it does
- The synchronizer performs a one-way synchronization from the source folder to the replica folder, making the contents of the replica completely match with the contents of the source folder.
- This is done through an operation queue where it keeps tracks of the operations performed on the source folder such as file creations, copies and deletions.
	- Some of these operations are later filtered to check if its execution or lack of would make the same result (creating and deleting the same file in the same interval is the same as not creating the file at all, as an example).
- Once the synchronizer has reached its set time interval, it will execute the synchronization by performing all its operations stored in the queue (FIFO).
	- File creation: copies the content from the source folder to the replica folder.
	- File change/copy: same as file creation, but the file already exists in the replica folder.
	- File deletion: deletes the file from the replica folder.
	- File move: moves the file from one location to another within the replica folder, mirroring the move operation in the source folder.
	- File rename: renames the file in the replica folder to match the new name in the source folder.
	- All of these operations are also applied on its subdirectories.
- On the execution of each operation, a log is generated in a log file (specified through command line), containing a timestamp and a description of the operation performed and during synchronization.

# How to use
- Download the latest release from the releases page.
	- You should get an ``FileSynchronizer.exe`` and ``FileSynchronizer.dll`` files.
- Open a command terminal in the folder where you have the downloaded files.
- Run the following command where:
	- `<source_folder>`: The path to the source folder you want to synchronize from (must exist in your system).
	- `<replica_folder>`: The path to the replica folder you want to synchronize to.
	- `<sync_interval>`: The time interval (in seconds) at which the synchronization should occur.
	- `<log_file>`: The path to the log file where synchronization logs will be written.

```bash
./FileSynchronizer.exe <source_folder> <replica_folder> <sync_interval> <log_file>
```

- Once the command is executed, the program will start monitoring the source folder for changes and will synchronize the replica folder at the specified interval.
	- At the beginning of its execution, it will perform a synchronization to copy all the current contents of the source folder to the replica folder.
	- Afterwards, it will perform the periodic synchronization based on the operations detected in the source folder. No newline at end of file