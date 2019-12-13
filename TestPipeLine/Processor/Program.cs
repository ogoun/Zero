using ZeroLevel;
using ZeroLevel.Logging;

namespace Processor
{
    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<ProcessorService>(args)
                .EnableConsoleLog(LogLevel.FullStandart)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
