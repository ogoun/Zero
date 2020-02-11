namespace ZeroLevel.EventServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<EventService>(args, configuration: () => Configuration.ReadOrEmptySetFromIniFile("config.ini"))
                .EnableConsoleLog()
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running);
            Bootstrap.Shutdown();
        }
    }
}
