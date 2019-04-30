namespace ZeroLevel.Discovery
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Log.AddConsoleLogger(Services.Logging.LogLevel.System | Services.Logging.LogLevel.FullDebug);
            Bootstrap.Startup<DiscoveryService>(args);
        }
    }
}