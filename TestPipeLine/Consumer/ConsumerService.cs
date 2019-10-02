using System;
using System.Threading;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace Consumer
{
    public class ConsumerService
        : BaseZeroService
    {
        protected override void StartAction()
        {
            ReadServiceInfo();
            AutoregisterInboxes(UseHost(8802));

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

        [ExchangeReplier("handle")]
        public bool Handler(ISocketClient client, int data)
        {
            return (data ^ Interlocked.Increment(ref _proceed)) % 2 == 0;
        }
    }
}
