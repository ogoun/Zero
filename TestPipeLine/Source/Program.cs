using ZeroLevel;

namespace Source
{
    static class Program
    {
        static void Main(string[] args)
        {
            IConfiguration conf = Configuration.Create();
            conf.Append("ServiceName", "Test source");
            conf.Append("ServiceKey", "test.source");
            conf.Append("ServiceType", "Sources");
            conf.Append("ServiceGroup", "Test");
            conf.Append("Version", "1.0.0.1");
            conf.Append("discovery", "127.0.0.1:5012");
            Configuration.Save(conf);

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
