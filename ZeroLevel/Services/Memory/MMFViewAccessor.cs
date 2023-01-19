using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ZeroLevel.Services.Memory
{
    internal class MMFViewAccessor
        : IViewAccessor
    {
        private readonly MemoryMappedViewStream _accessor;
        private readonly long _absoluteOffset;

        public MMFViewAccessor(MemoryMappedViewStream accessor, long offset)
        {
            _accessor = accessor;
            _absoluteOffset = offset;
        }

        public bool EOV => _accessor.Position >= _accessor.Length;

        public long Position => _absoluteOffset + _accessor.Position;

        public bool CheckOutOfRange(int offset)
        {
            return offset < 0 || (_accessor.Position + offset) > _accessor.Length;
        }

        public void Dispose()
        {
            _accessor?.Dispose();
        }

        public byte[] ReadBuffer(int count)
        {
            if (count == 0) return null;
            var buffer = new byte[count];
            var readedCount = _accessor.Read(buffer, 0, count);
            if (count != readedCount)
                throw new InvalidOperationException($"The stream returned less data ({count} bytes) than expected ({readedCount} bytes)");
            return buffer;
        }

        public void Seek(long offset)
        {
            _accessor.Seek(offset, SeekOrigin.Begin);
        }
    }
}
