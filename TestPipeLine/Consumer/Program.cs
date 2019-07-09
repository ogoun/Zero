using ZeroLevel;

namespace Consumer
{
    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<ConsumerService>(args)
                .EnableConsoleLog(ZeroLevel.Services.Logging.LogLevel.FullStandart)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
