using ZeroLevel;

namespace Source
{
    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<SourceService>(args)
                .EnableConsoleLog(ZeroLevel.Services.Logging.LogLevel.FullStandart)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
