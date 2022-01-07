using ZeroLevel;
using ZeroLevel.Logging;

namespace Consumer
{
    static class Program
    {
        static void Main(string[] args)
        {
            IConfiguration conf = Configuration.Create();
            conf.Append("ServiceName", "Test consumer");
            conf.Append("ServiceKey", "test.consumer");
            conf.Append("ServiceType", "Destination");
            conf.Append("ServiceGroup", "Test");
            conf.Append("Version", "1.0.0.1");
            conf.Append("discovery", "127.0.0.1:5012");
            Configuration.Save(conf);

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
