using ZeroLevel;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Log.AddConsoleLogger();
            Bootstrap.Startup<MyService>(args);
        }
    }
}
