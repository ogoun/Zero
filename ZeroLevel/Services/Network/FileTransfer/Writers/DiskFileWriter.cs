using System;
using System.IO;

namespace ZeroLevel.Services.Network.FileTransfer.Writers
{
    internal sealed class DiskFileWriter
            : IDataWriter
    {
        private readonly FileStream _stream;
        private DateTime _writeTime = DateTime.UtcNow;
        private readonly long _size;
        private long _receive_size = 0;

        public DiskFileWriter(string path, long size)
        {
            _size = size;
            _stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            _stream.SetLength(_size);
        }

        public void CompleteReceiving()
        {
            if (_receive_size != _size)
            {
                Log.Error("Incomplete file data");
            }
        }

        public void Write(long offset, byte[] data)
        {
            _receive_size += data.Length;
            _stream.Position = offset;
            _stream.Write(data, 0, data.Length);
            _writeTime = DateTime.Now;
        }

        public bool IsTimeoutBy(TimeSpan period)
        {
            return (DateTime.Now - _writeTime) > period;
        }

        public void Dispose()
        {
            _stream.Flush();
            _stream.Close();
            _stream.Dispose();
        }
    }
}
