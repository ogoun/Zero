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
            

            Sheduller.RemindEvery(TimeSpan.FromMilliseconds(350), () =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("Discovery table");
                sb.AppendLine("—————————————————————————————————————————————————————————————————————————");

                foreach (var record in Exchange.DiscoveryStorage.GetAll())
                {
                    sb.AppendLine($"\t{record.Key}:\t{record.Value.Address}:{record.Value.Port}");
                }

                sb.AppendLine("Routing table");
                sb.AppendLine("—————————————————————————————————————————————————————————————————————————");

                foreach (var record in Exchange.RoutesStorage.GetAll())
                {
                    sb.AppendLine($"\t{record.Key}:\t{record.Value.Address}:{record.Value.Port}");
                }
                sb.AppendLine();
                Console.Clear();
                Console.WriteLine($"Watch info: \r\n{sb}");
            });
        }

        protected override void StopAction()
        {
        }
    }
}
