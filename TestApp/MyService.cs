using System;
using System.Net;
using System.Threading;
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
            ReadServiceInfo();
            var host = UseHost();
            AutoregisterInboxes(host);
            host.OnConnect += Host_OnConnect;
            host.OnDisconnect += Host_OnDisconnect;

            int counter = 0;
            Sheduller.RemindEvery(TimeSpan.FromSeconds(1), () =>
            {
                Log.Info($"RPS: {counter}");
                Interlocked.Exchange(ref counter, 0);
            });

            //Exchange.RoutesStorage.Set("test.app", new IPEndPoint(IPAddress.Loopback, 8800));

            while (true)
            {
                try
                {
                    Exchange.GetConnection("test.app")?.Request<int>("counter", s => Interlocked.Add(ref counter, s));
                }
                catch
                {
                    Thread.Sleep(300);
                }
            }
        }

        private void Host_OnDisconnect(ISocketClient obj)
        {
            Log.Info($"Client '{obj.Endpoint.Address}:{obj.Endpoint.Port}' disconnected");
        }

        private void Host_OnConnect(ExClient obj)
        {
            Log.Info($"Client '{obj.Socket.Endpoint.Address}:{obj.Socket.Endpoint.Port}' connected");
        }

        [ExchangeReplierWithoutArg("counter")]
        public int GetCounter(ISocketClient client)
        {
            return 1;
        }

        protected override void StopAction()
        {
            Log.Info("Stopped");
        }
    }
}