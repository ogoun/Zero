using System;
using System.Net;
using ZeroLevel;
using ZeroLevel.Services.Applications;

namespace TestApp
{
    public class MyService
        : BaseZeroService
    {
        public MyService()
            :base()
        {
        }

        protected override void StartAction()
        {
            Log.Info("Started");
            Sheduller.RemindEvery(TimeSpan.FromSeconds(5), () => {
                var client = ConnectToService(new IPEndPoint(IPAddress.Loopback, 8800));
                client.Request<string, string>("upper", "hello", s => Log.Info(s));
            });
        }

        protected override void StopAction()
        {
            Log.Info("Stopped");
        }
    }
}
