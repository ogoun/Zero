using System;
using System.Linq;
using System.Text;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Services.Applications;

namespace Watcher
{
    public class WatcherService
        : BaseZeroService
    {
        protected override void StartAction()
        {
            ReadServiceInfo();
            AutoregisterInboxes(UseHost());

            Sheduller.RemindEvery(TimeSpan.FromMilliseconds(350), () =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("—————————————————————————————————————————————————————————————————————————");

                var success = Exchange.RequestBroadcastByGroup<ZeroServiceInfo>("Test", "meta", records =>
                {
                    if (records.Any() == false)
                    {
                        Log.Info("No services");
                    }

                    foreach (var record in records.OrderBy(r=>r.Name))
                    {
                        sb.Append(record.Name);
                        sb.Append(" (");
                        sb.Append(record.Version);
                        sb.AppendLine(")");
                        sb.AppendLine(record.ServiceKey);
                        sb.AppendLine(record.ServiceType);
                        sb.AppendLine(record.ServiceGroup);
                        sb.AppendLine();
                    }
                });
                if (!success)
                {
                    Log.Warning("[WatcherService] Can't send broadcast reqeust for meta");
                }

                success = Exchange.RequestBroadcastByType<long>("Sources", "Proceed", records =>
                {
                    sb.AppendLine("-----------------------------------------------------------------------------");
                    sb.Append("Source send items: ");
                    sb.AppendLine(records.Sum().ToString());
                });
                if (!success)
                {
                    Log.Warning("[WatcherService] Can't send broadcast reqeust to 'Sources'");
                }

                success = Exchange.RequestBroadcastByType<long>("Core", "Proceed", records =>
                {
                    sb.AppendLine("-----------------------------------------------------------------------------");
                    sb.Append("Proccessor handle and send items: ");
                    sb.AppendLine(records.Sum().ToString());
                });
                if (!success)
                {
                    Log.Warning("[WatcherService] Can't send broadcast reqeust to 'Core'");
                }

                success = Exchange.RequestBroadcastByType<long>("Destination", "Proceed", records =>
                {
                    sb.AppendLine("-----------------------------------------------------------------------------");
                    sb.Append("Consumer catch: ");
                    sb.AppendLine(records.Sum().ToString());
                });
                if (!success)
                {
                    Log.Warning("[WatcherService] Can't send broadcast reqeust to 'Destination'");
                }

                sb.AppendLine("—————————————————————————————————————————————————————————————————————————");
                sb.AppendLine();
                Console.Clear();
                Console.WriteLine($"Watch info: \r\n{sb}");
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

        [ExchangeReplierWithoutArg("ping")]
        public bool Ping(ISocketClient client)
        {
            return true;
        }
    }
}
