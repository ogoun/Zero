using System;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Memory
{
    public interface IViewAccessor
        : IDisposable
    {
        bool IsMemoryStream { get; }
        /// <summary>
        /// End of view
        /// </summary>
        bool EOV { get; }
        long Position { get; }
        Task<byte[]> ReadBuffer(int count);
        bool CheckOutOfRange(int offset);
        void Seek(long offset);
    }
}
