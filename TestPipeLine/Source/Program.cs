using ZeroLevel;

namespace Source
{
    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<SourceService>(args)
                .EnableConsoleLog(ZeroLevel.Logging.LogLevel.FullStandart)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
