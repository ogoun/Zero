using ZeroLevel;
using ZeroLevel.Logging;

namespace Processor
{
    static class Program
    {
        static void Main(string[] args)
        {
            IConfiguration conf = Configuration.Create();
            conf.Append("ServiceName", "Test processor");
            conf.Append("ServiceKey", "test.processor");
            conf.Append("ServiceType", "Core");
            conf.Append("ServiceGroup", "Test");
            conf.Append("Version", "1.0.0.1");
            conf.Append("discovery", "127.0.0.1:5012");
            Configuration.Save(conf);

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
