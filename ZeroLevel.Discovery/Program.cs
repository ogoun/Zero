namespace ZeroLevel.Discovery
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Bootstrap.Startup<DiscoveryService>(args);
        }
    }
}