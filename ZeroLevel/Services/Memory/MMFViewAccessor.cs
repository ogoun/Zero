using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Memory
{
    internal class MMFViewAccessor
        : IViewAccessor
    {
        private readonly MemoryMappedViewStream _accessor;
        private readonly long _absoluteOffset;
        private readonly Action _onDispose;
        private int _disposed;

        public MMFViewAccessor(MemoryMappedViewStream accessor, long offset)
        {
            _accessor = accessor;
            _absoluteOffset = offset;
        }

        public MMFViewAccessor(MemoryMappedViewStream accessor, long offset, Action onDispose)
        {
            _accessor = accessor;
            _absoluteOffset = offset;
            _onDispose = onDispose;
        }

        public bool EOV => _accessor.Position >= _accessor.Length;

        public long Position => _absoluteOffset + _accessor.Position;

        public bool IsMemoryStream => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckOutOfRange(int offset) => offset < 0 || (_accessor.Position + offset) > _accessor.Length;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _accessor?.Dispose();
            _onDispose?.Invoke();
        }

        public async Task<byte[]> ReadBuffer(int count)
        {
            if (count == 0) return null!;
            var buffer = new byte[count];
            int total = 0;
            while (total < count)
            {
                var read = await _accessor.ReadAsync(buffer, total, count - total);
                if (read == 0)
                    throw new InvalidOperationException($"The stream returned less data ({total} bytes) than expected ({count} bytes)");
                total += read;
            }
            return buffer;
        }

        public byte[] ReadBufferSync(int count)
        {
            if (count == 0) return null!;
            var buffer = new byte[count];
            int total = 0;
            while (total < count)
            {
                var read = _accessor.Read(buffer, total, count - total);
                if (read == 0)
                    throw new InvalidOperationException($"The stream returned less data ({total} bytes) than expected ({count} bytes)");
                total += read;
            }
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(long offset) => _accessor.Seek(offset, SeekOrigin.Begin);
    }
}
