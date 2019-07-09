using ZeroLevel;

namespace Processor
{
    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<ProcessorService>(args)
                .EnableConsoleLog(ZeroLevel.Services.Logging.LogLevel.FullStandart)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
