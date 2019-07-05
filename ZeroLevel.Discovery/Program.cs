namespace ZeroLevel.Discovery
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Bootstrap.Startup<DiscoveryService>(args)
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}