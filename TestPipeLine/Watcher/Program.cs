using ZeroLevel;

namespace Watcher
{
    static class Program
    {
        static void Main(string[] args)
        {
            IConfiguration conf = Configuration.Create();
            conf.Append("ServiceName", "Watcher");
            conf.Append("ServiceKey", "test.watcher");
            conf.Append("ServiceType", "System");
            conf.Append("ServiceGroup", "Test");
            conf.Append("Version", "1.0.0.1");
            conf.Append("discovery", "127.0.0.1:5012");
            Configuration.Save(conf);

            Bootstrap.Startup<WatcherService>(args)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
