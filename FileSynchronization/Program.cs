using FileSynchronization;

class Program
{

    static int Main(string[] args)
    {
        CommandLineArguments commandLineArguments = new(args);
        if (!commandLineArguments.IsValid())
        {
            Console.Error.WriteLine("Error: Provided arguments were not valid.\n Use the following format:\n \"FileSynchronization <sourcePath> <replicaPath> <synchronizationInterval> <logFilePath>\"");
            return -1;
        }
        
        Console.WriteLine("Arguments received from command line, starting synchronization process...");
        return 0;
    }
}