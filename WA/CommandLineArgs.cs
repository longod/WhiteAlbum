namespace WA
{
    // for injection
    public class CommandLineArgs
    {
        public string[] Args { get; }

        public CommandLineArgs(string[] args)
        {
            Args = args;
        }
    }
}
