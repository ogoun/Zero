using ZeroLevel;

namespace TestApp
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Bootstrap.Startup<MyService>(args,                
                () => Configuration.ReadSetFromIniFile("config.ini"))
                .EnableConsoleLog(ZeroLevel.Services.Logging.LogLevel.System | ZeroLevel.Services.Logging.LogLevel.FullDebug)
                //.UseDiscovery()
                .Run()
                .WaitWhileStatus(ZeroServiceStatus.Running)
                .Stop();
            Bootstrap.Shutdown();
        }
    }
}
