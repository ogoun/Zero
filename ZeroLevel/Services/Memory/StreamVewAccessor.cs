using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

        public bool IsMemoryStream => _stream is MemoryStream;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckOutOfRange(int offset) => offset < 0 || (_stream.Position + offset) > _stream.Length;

        public async Task<byte[]> ReadBuffer(int count)
        {
            if (count == 0) return null!;
            var buffer = new byte[count];
            var readedCount = await _stream.ReadAsync(buffer, 0, count);
            if (count != readedCount)
                throw new InvalidOperationException($"The stream returned less data ({count} bytes) than expected ({readedCount} bytes)");
            return buffer;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(long offset) => _stream.Seek(offset, SeekOrigin.Begin);
    }
}
