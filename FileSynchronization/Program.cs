using FileSynchronization;

class Program
{

    static int Main(string[] args)
    {
        CommandLineArguments commandLineArguments = new(args);
        if (!commandLineArguments.IsValid())
        {
            Console.Error.WriteLine(LogMessages.WRONG_ARGUMENTS_ERROR);
            return -1;
        }
        
        Console.WriteLine(LogMessages.ARGUMENTS_RECEIVED_INFO);
        InitializeSynchronizer(commandLineArguments);
        return 0;
    }

    internal static void InitializeSynchronizer(CommandLineArguments arguments)
    {
        FileSynchronizer synchronizer = new(arguments);
        for(;;)
        {
            synchronizer.Synchronize();
            synchronizer.Rest();
        }
    }
}