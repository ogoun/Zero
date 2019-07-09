using ZeroLevel;

namespace Watcher
{
    static class Program
    {
        static void Main(string[] args)
        {
            Bootstrap.Startup<WatcherService>(args)
                .UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
