using System;
using System.Net;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace TestApp
{
    public class MyService
        : BaseZeroService
    {
        public MyService()
            : base()
        {
        }

        protected override void StartAction()
        {
            Log.Info("Started");
            UseHost(8800)
                .RegisterInbox<string, string>("upper", (c, s) => s.ToUpperInvariant())
                .RegisterInbox<IPEndPoint, string>("ip2str", (c, ip) => $"{ip.Address}:{ip.Port}");


            Sheduller.RemindEvery(TimeSpan.FromSeconds(5), () =>
            {
                var client = ConnectToService(new IPEndPoint(IPAddress.Loopback, 8800));
                client.Request<string, string>("upper", "hello", s => Log.Info(s));
            });
            
            Sheduller.RemindEvery(TimeSpan.FromSeconds(6), () =>
            {
                var client = ConnectToService(new IPEndPoint(IPAddress.Loopback, 8800));
                client.Request<IPEndPoint, string>("ip2str", new IPEndPoint(NetUtils.GetNonLoopbackAddress(), NetUtils.GetFreeTcpPort()), s => Log.Info(s));
            });
            
        }

        protected override void StopAction()
        {
            Log.Info("Stopped");
        }
    }
}
