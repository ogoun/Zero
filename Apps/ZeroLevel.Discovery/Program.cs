using System;
using Topshelf;
using static ZeroLevel.Bootstrap;

namespace ZeroLevel.Discovery
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            IServiceExecution se = null;
            HostFactory.Run(x =>
            {
                x.StartAutomatically();
                x.Service<BootstrapFluent>(s =>
                {
                    s.ConstructUsing(name => Bootstrap.Startup<DiscoveryService>(args));
                    s.WhenStopped(tc => { tc.Stop(); Bootstrap.Shutdown(); });
                    s.WhenStarted(tc => { se = tc.Run(); });
                });
                x.RunAsLocalSystem();

                x.SetDescription("Discovery");
                x.SetDisplayName("Discovery");
                x.SetServiceName("Discovery");

                x.OnException(ex =>
                {
                    Log.Error(ex, "Service exception");
                });
            });
            if (Environment.UserInteractive && args?.Length < 1)
            {
                se?.WaitWhileStatus(ZeroServiceStatus.Running);
            }
        }
    }
}