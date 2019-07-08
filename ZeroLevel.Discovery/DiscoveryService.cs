using ZeroLevel.Models;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace ZeroLevel.Discovery
{
    public sealed class DiscoveryService
        : BaseZeroService
    {
        private IRouter _exInbox;

        public DiscoveryService()
            : base("Discovery")
        {
        }

        protected override void StartAction()
        {
            var routeTable = new RouteTable();
            Injector.Default.Register<RouteTable>(routeTable);
            var servicePort = Configuration.Default.First<int>("port");
            _exInbox = UseHost(servicePort);
            _exInbox.RegisterInbox("services", (_) => routeTable.Get());
            _exInbox.RegisterInbox<ZeroServiceInfo, InvokeResult>("register", (client, info) => routeTable.Append(info, client));
        }

        protected override void StopAction()
        {
        }
    }
}