using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services
{
    public static class Dbg
    {
        private static BlockingCollection<Tuple<int, long, string>> _timestamps =
            new BlockingCollection<Tuple<int, long, string>>();

        private static Thread _writeThread;
        private static readonly bool _started;

        static Dbg()
        {
            if (Configuration.Default.Contains("dbg"))
            {
                try
                {
                    if (false == Directory.Exists(Configuration.Default.First("dbg")))
                    {
                        Directory.CreateDirectory(Configuration.Default.First("dbg"));
                    }
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[Dbg] Fault initialize, dbg files directory not exists and not may be created");
                    _started = false;
                    return;
                }
                _writeThread = new Thread(HandleQueue);
                _writeThread.IsBackground = true;
                _writeThread.Start();
                _started = true;
            }
            else
            {
                _started = false;
            }
        }

        private static void HandleQueue()
        {
            using (var fs = new FileStream(Path.Combine(Configuration.Default.First("dbg"), $"{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.dbg"), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new MemoryStreamWriter(fs))
                {
                    while (_timestamps.IsCompleted == false)
                    {
                        var pair = _timestamps.Take();
                        writer.WriteInt32(pair.Item1);
                        writer.WriteLong(pair.Item2);
                        writer.WriteString(pair.Item3);
                    }
                    fs.Flush();
                }
            }
        }

        internal static void Shutdown()
        {
            if (_started)
            {
                _timestamps.CompleteAdding();
            }
        }

        internal static void Timestamp(int eventType, string description)
        {
            if (_started && _timestamps.IsAddingCompleted == false)
            {
                _timestamps.Add(Tuple.Create<int, long, string>(eventType, DateTime.UtcNow.Ticks, description));
            }
        }
    }
}
