using FASTER.core;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Microservices.Dump
{
    public class DumpStorage<T>
    {
        IDevice device;
        FasterLog log;

        public DumpStorage()
        {
            var folder = Path.Combine(Configuration.BaseDirectory, "dump");
            if (false == Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            device = Devices.CreateLogDevice(Path.Combine(folder, $"dump.log"),
                true, true, -1, false);

            log = new FasterLog(new FasterLogSettings { LogDevice = device });
        }

        public void Dump(T value)
        {
            var packet = MessageSerializer.SerializeCompatible(value);
            while (!log.TryEnqueue(packet, out _)) ;
            log.Commit();
        }

        public async Task DumpAsync(T value)
        {
            var packet = MessageSerializer.SerializeCompatible(value);
            await log.EnqueueAndWaitForCommitAsync(packet);
        }

        public IEnumerable<T> ReadAndTruncate()
        {
            byte[] result;
            using (var iter = log.Scan(log.BeginAddress, log.TailAddress))
            {
                while (iter.GetNext(out result, out int length))
                {
                    yield return MessageSerializer.DeserializeCompatible<T>(result);
                }
                log.TruncateUntil(iter.NextAddress);
            }
        }
    }
}
