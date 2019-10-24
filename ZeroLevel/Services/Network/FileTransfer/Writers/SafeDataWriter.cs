using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ZeroLevel.Network.FileTransfer;

namespace ZeroLevel.Services.Network.FileTransfer.Writers
{
    internal sealed class SafeDataWriter
        : IDisposable
    {
        private readonly IDataWriter _writer;
        private readonly Action _complete;
        private BlockingCollection<FileFrame> _chunks = new
            BlockingCollection<FileFrame>();
        private volatile bool _disposed = false;

        public SafeDataWriter(IDataWriter writer, Action complete)
        {
            _writer = writer;
            _complete = complete;
            Task.Run(() =>
            {
                try
                {
                    FileFrame frame;
                    while (!_chunks.IsCompleted)
                    {
                        if (_chunks.TryTake(out frame, 200))
                        {
                            writer.Write(frame.Offset, frame.Payload);
                        }
                    }
                    _writer.CompleteReceiving();
                    _complete?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[SafeDataWriter.ctor receive loop] Fault receive data");
                }
            });
        }

        public void CompleteReceiving()
        {
            Sheduller.RemindAfter(TimeSpan.FromSeconds(1), _chunks.CompleteAdding);
        }

        public void Dispose()
        {
            _disposed = true;
            _chunks.Dispose();
            _writer.Dispose();
        }

        public bool IsTimeoutBy(TimeSpan period) => _writer.IsTimeoutBy(period);

        public void Write(FileFrame frame)
        {
            if (!_disposed)
            {
                _chunks.Add(frame);
            }
        }
    }
}
