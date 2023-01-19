using System;
using System.IO;

namespace ZeroLevel.Services.Memory
{
    internal sealed class StreamVewAccessor
        : IViewAccessor
    {
        private readonly Stream _stream;
        public StreamVewAccessor(Stream stream)
        {
            _stream = stream;
        }

        public bool EOV => _stream.Position >= _stream.Length;

        public long Position => _stream.Position;

        public bool CheckOutOfRange(int offset)
        {
            return offset < 0 || (_stream.Position + offset) > _stream.Length;
        }

        public byte[] ReadBuffer(int count)
        {
            if (count == 0) return null;
            var buffer = new byte[count];
            var readedCount = _stream.Read(buffer, 0, count);
            if (count != readedCount)
                throw new InvalidOperationException($"The stream returned less data ({count} bytes) than expected ({readedCount} bytes)");
            return buffer;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void Seek(long offset)
        {
            _stream.Seek(offset, SeekOrigin.Begin);
        }
    }
}
