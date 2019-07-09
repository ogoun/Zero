using System;
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

            Sheduller.RemindEvery(TimeSpan.FromMilliseconds(10), () =>
            {
                if (Exchange.Send("test.processor", "handle", Environment.TickCount))
                {
                    Interlocked.Increment(ref _proceed);
                }
            });
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
