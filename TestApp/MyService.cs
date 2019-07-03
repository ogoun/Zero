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

            AutoregisterInboxes(UseHost(8800));
            var client = ConnectToService(new IPEndPoint(IPAddress.Loopback, 8800));

            Sheduller.RemindEvery(TimeSpan.FromSeconds(5), () =>
            {
                client.Send("pum");
                client.Send<string>(BaseSocket.DEFAULT_MESSAGE_INBOX, "'This is message'");
                client.Request<DateTime, string>("d2s", DateTime.Now, s => Log.Info($"Response: {s}"));
                client.Request<IPEndPoint, string>(BaseSocket.DEFAULT_REQUEST_INBOX,
                        new IPEndPoint(NetUtils.GetNonLoopbackAddress(), NetUtils.GetFreeTcpPort()),
                        s => Log.Info($"Response: {s}"));
                client.Request<string>("now", s => Log.Info($"Response date: {s}"));
                client.Request<string>(BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, s => Log.Info($"Response ip: {s}"));
            });
        }

        [ExchangeHandler("pum")]
        public void MessageHandler(ISocketClient client)
        {
            Log.Info("Called message handler without arguments");
        }

        [ExchangeMainHandler]
        public void MessageHandler(ISocketClient client, string message)
        {
            Log.Info($"Called message handler (DEFAULT INBOX) with argument: {message}");
        }

        [ExchangeReplier("d2s")]
        public string date2str(ISocketClient client, DateTime date)
        {
            Log.Info($"Called reqeust handler with argument: {date}");
            return date.ToLongDateString();
        }

        [ExchangeMainReplier]
        public string ip2str(ISocketClient client, IPEndPoint ip)
        {
            Log.Info($"Called reqeust handler (DEFAULT INBOX) with argument: {ip.Address}:{ip.Port}");
            return $"{ip.Address}:{ip.Port}";
        }


        [ExchangeReplierWithoutArg("now")]
        public string GetTime(ISocketClient client)
        {
            Log.Info("Called reqeust handler without arguments");
            return DateTime.Now.ToShortDateString();
        }

        [ExchangeMainReplierWithoutArg]
        public string GetMyIP(ISocketClient client)
        {
            Log.Info("Called reqeust handler (DEFAULT INBOX) without argument");
            return NetUtils.GetNonLoopbackAddress().ToString();
        }

        protected override void StopAction()
        {
            Log.Info("Stopped");
        }
    }
}
