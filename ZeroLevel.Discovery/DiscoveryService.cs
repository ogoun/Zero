using ZeroLevel.Services.Applications;

namespace ZeroLevel.Discovery
{
    public sealed class DiscoveryService
        : BaseWindowsService, IZeroService
    {
        public DiscoveryService()
            : base("Discovery")
        {
        }

        public override void PauseAction()
        {
        }

        public override void ResumeAction()
        {
        }

        public override void StartAction()
        {
            Injector.Default.Register<RouteTable>(new RouteTable());
            var port = Configuration.Default.First<int>("port");
            Startup.StartWebPanel(port, false);
        }

        public override void StopAction()
        {
        }
    }
}