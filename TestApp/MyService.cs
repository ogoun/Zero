using System;
using System.Net;
using System.Threading;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Network.SDL;
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
            var client = Exchange.GetConnection("192.168.51.104:50223");
            client?.Request<ServiceDescription>("__service_description__", record => 
            {
                Log.Info(record.ServiceInfo.ServiceKey);
            });
            return;

            Log.Info("Started");
            ReadServiceInfo();
            var host = UseHost(8800);
            AutoregisterInboxes(host);
            host.OnConnect += Host_OnConnect;
            host.OnDisconnect += Host_OnDisconnect;

            int counter = 0;
            Sheduller.RemindEvery(TimeSpan.FromSeconds(1), () =>
            {
                Log.Info($"RPS: {counter}");
                Interlocked.Exchange(ref counter, 0);
            });

            Exchange.RoutesStorage.Set("test.app", new IPEndPoint(IPAddress.Loopback, 8800));
            using (var waiter = new ManualResetEventSlim(false))
            {
                while (true)
                {
                    try
                    {
                        Exchange.GetConnection("test.app")?.Request<int>("counter", s =>
                        {
                            waiter.Set();
                            Interlocked.Add(ref counter, s);
                        });
                    }
                    catch
                    {
                        Thread.Sleep(300);
                    }
                    waiter.Wait();
                    waiter.Reset();
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