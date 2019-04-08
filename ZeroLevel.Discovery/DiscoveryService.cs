using System.Collections.Generic;
using ZeroLevel.Models;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Discovery
{
    public sealed class DiscoveryService
        : BaseWindowsService, IZeroService
    {
        private IExService _exInbox;

        public DiscoveryService()
            : base("Discovery")
        {
        }

        public override void DisposeResources()
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
            var routeTable = new RouteTable();

            Injector.Default.Register<RouteTable>(routeTable);
            var port = Configuration.Default.First<int>("apiport");
            Startup.StartWebPanel(port, false);

            var socketPort = Configuration.Default.First<int>("socketport");
            _exInbox = ExchangeTransportFactory.GetServer(socketPort);
            _exInbox.RegisterInbox<IEnumerable<ServiceEndpointsInfo>>("services", (_, __) => routeTable.Get());
            _exInbox.RegisterInbox<ExServiceInfo, InvokeResult>("register", (info, _, __) => routeTable.Append(info));

            Log.Info($"TCP server started {_exInbox.Endpoint.Address}:{socketPort}");
        }

        public override void StopAction()
        {
            _exInbox.Dispose();
        }
    }
}