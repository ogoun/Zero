using System.Collections.Generic;
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
            var socketPort = Configuration.Default.First<int>("socketport");
            _exInbox = UseHost(socketPort);
            _exInbox.RegisterInbox<IEnumerable<ServiceEndpointsInfo>>("services", (_, __) => routeTable.Get());
            _exInbox.RegisterInbox<ZeroServiceInfo, InvokeResult>("register", (client, info) => routeTable.Append(info, client));
        }

        protected override void StopAction()
        {
        }
    }
}