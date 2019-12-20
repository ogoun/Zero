namespace ZeroLevel.Logger
{
    class Program
    {
        static void Main(string[] args)
        {            
            Bootstrap.Startup<LogService>(args)
                .EnableConsoleLog()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
