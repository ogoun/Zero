using System;
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

            AutoregisterInboxes(UseHost());

            int counter = 0;
            Sheduller.RemindEvery(TimeSpan.FromSeconds(1), () =>
            {
                Log.Info($"RPS: {counter}");
                Interlocked.Exchange(ref counter, 0);
            });
            for (int i = 0; i < int.MaxValue; i++)
            {
                try
                {
                    Exchange.GetConnection("test.app").Request<int>("counter", s =>
                    {
                        Interlocked.Add(ref counter, s);
                    });
                }
                catch
                {
                    Thread.Sleep(300);
                }
            }


            /*
            Sheduller.RemindEvery(TimeSpan.FromSeconds(1), () =>
            {
                var client = Exchange.GetConnection("test.app");
                client.Send("pum");
                client.Send<string>(BaseSocket.DEFAULT_MESSAGE_INBOX, "'This is message'");
                client.Request<DateTime, string>("d2s", DateTime.Now, s => Log.Info($"Response: {s}"));
                client.Request<IPEndPoint, string>(BaseSocket.DEFAULT_REQUEST_INBOX,
                        new IPEndPoint(NetUtils.GetNonLoopbackAddress(), NetUtils.GetFreeTcpPort()),
                        s => Log.Info($"Response: {s}"));
                client.Request<string>("now", s => Log.Info($"Response date: {s}"));
                client.Request<string>(BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, s => Log.Info($"Response ip: {s}"));
            });
            */
            /*Sheduller.RemindEvery(TimeSpan.FromSeconds(3), () =>
            {
                Exchange.Request<ZeroServiceInfo>("test.app", "metainfo", info =>
                {
                    var si = new StringBuilder();
                    si.AppendLine(info.Name);
                    si.AppendLine(info.ServiceKey);
                    si.AppendLine(info.Version);

                    Log.Info("Service info:\r\n{0}", si.ToString());
                });
            });*/
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