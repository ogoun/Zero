using System;
using System.Net;
using System.Threading;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace Source
{
    public class SourceService
        : BaseZeroService
    {
        protected override void StartAction()
        {
            ReadServiceInfo();
            AutoregisterInboxes(UseHost());

            Exchange.RoutesStorage.Set("test.processor", new IPEndPoint(IPAddress.Loopback, 8801));

            /*Sheduller.RemindEvery(TimeSpan.FromMilliseconds(100), () =>
            {
                if (Exchange.Send("test.processor", "handle", Environment.TickCount))
                {
                    Interlocked.Increment(ref _proceed);
                }
            });*/

            try
            {
                using (var waiter = new ManualResetEventSlim(false))
                {
                    while (true)
                    {
                        try
                        {
                            var ir = Exchange.GetConnection("test.processor")?.Request<long, bool>("handle"
                                , Environment.TickCount
                                , s =>
                             {
                                Interlocked.Increment(ref _proceed);
                                waiter.Set();
                            });
                            if (ir == null || ir == false)
                            {
                                Thread.Sleep(300);
                                waiter.Set();
                            }
                        }
                        catch
                        {
                            Thread.Sleep(300);
                            waiter.Set();
                        }
                        waiter.Wait(5000);
                        waiter.Reset();
                    }
                }
            }
            catch
            {
                Thread.Sleep(300);
            }
        }

        protected override void StopAction()
        {
        }

        [ExchangeReplierWithoutArg("meta")]
        public ZeroServiceInfo GetCounter(ISocketClient client)
        {
            return ServiceInfo;
        }

        private long _proceed = 0;

        [ExchangeReplierWithoutArg("Proceed")]
        public long GetProceedItemsCount(ISocketClient client)
        {
            return _proceed;
        }

        [ExchangeReplierWithoutArg("ping")]
        public bool Ping(ISocketClient client)
        {
            return true;
        }
    }
}
