using ZeroLevel;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Bootstrap.Startup<MyService>(args, () => Configuration.ReadSetFromIniFile("config.ini"));
        }
    }
}
