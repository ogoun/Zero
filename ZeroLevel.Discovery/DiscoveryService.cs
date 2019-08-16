using System.Linq;
using ZeroLevel.Models;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace ZeroLevel.Discovery
{
    public sealed class DiscoveryService
        : BaseZeroService
    {
        private IRouter _exInbox;
        private ServiceEndpointsTable _table;

        public DiscoveryService()
            : base("Discovery")
        {
        }

        protected override void StartAction()
        {
            _table = new ServiceEndpointsTable();
            var servicePort = Configuration.Default.First<int>("port");
            _exInbox = UseHost(servicePort);
            _exInbox.RegisterInbox("services", (_) => _table.GetRoutingTable().ToList());
            _exInbox.RegisterInbox<ServiceRegisterInfo, InvokeResult>("register", (client, info) => _table.AppendOrUpdate(info, client));
        }

        protected override void StopAction()
        {
        }
    }
}