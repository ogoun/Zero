using System.Collections.Generic;
using ZeroLevel.Models;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace ZeroLevel.Discovery
{
    public sealed class DiscoveryService
        : BaseZeroService
    {
        private IExService _exInbox;

        public DiscoveryService()
            : base("Discovery")
        {
        }

        protected override void StartAction()
        {
            var routeTable = new RouteTable();

            Injector.Default.Register<RouteTable>(routeTable);
            
            var socketPort = Configuration.Default.First<int>("socketport");
            _exInbox = ExchangeTransportFactory.GetServer(socketPort);
            _exInbox.RegisterInbox<IEnumerable<ServiceEndpointsInfo>>("services", (_, __) => routeTable.Get());
            _exInbox.RegisterInbox<ExServiceInfo, InvokeResult>("register", (info, _, client) => routeTable.Append(info, client));

            Log.Info($"TCP server started {_exInbox.Endpoint.Address}:{socketPort}");
        }

        protected override void StopAction()
        {
            _exInbox.Dispose();
        }
    }
}