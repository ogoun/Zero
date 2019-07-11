using System;
using System.Threading;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace Processor
{
    public class ProcessorService
        : BaseZeroService
    {
        protected override void StartAction()
        {
            ReadServiceInfo();
            AutoregisterInboxes(UseHost());

            Sheduller.RemindEvery(TimeSpan.FromSeconds(1), () =>
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(_proceed);
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

        [ExchangeHandler("handle")]
        public void Handler(ISocketClient client, int data)
        {
            var next = (int)(data ^ Interlocked.Increment(ref _proceed));
            Exchange.Request<int, bool>("test.consumer", "handle", next, result => { });
        }
    }
}
