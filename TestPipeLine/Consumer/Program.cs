using ZeroLevel;
using ZeroLevel.Logging;

namespace Consumer
{
    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<ConsumerService>(args)
                .EnableConsoleLog(LogLevel.FullStandart)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
