using System.Collections.Concurrent;
using System.Net;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace Processor
{
    public class ProcessorService
        : BaseZeroService
    {
        private Thread _processThread;

        protected override void StartAction()
        {
            _processThread = new Thread(HandleIncoming);
            _processThread.IsBackground = true;
            _processThread.Start();

            ReadServiceInfo();
            AutoregisterInboxes(UseHost(8801));

            Exchange.RoutesStorage.Set("test.consumer", new IPEndPoint(IPAddress.Loopback, 8802));

            Sheduller.RemindEvery(TimeSpan.FromSeconds(1), () =>
            {
                Console.SetCursorPosition(0, 0);
                Console.WriteLine(_proceed);
            });
        }

        private void HandleIncoming()
        {
            while (_incoming.IsCompleted == false)
            {
                long data;
                if (_incoming.TryTake(out data, 100))
                {
                    var next = (int)(data ^ Interlocked.Increment(ref _proceed));
                    Exchange.Request<int, bool>("test.consumer", "handle", next, result => { });
                }
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

        BlockingCollection<long> _incoming = new BlockingCollection<long>();

        [ExchangeReplier("handle")]
        public bool Handler(ISocketClient client, long data)
        {
            return _incoming.TryAdd(data);
        }
    }
}
