using System;

namespace ZeroLevel.Services.Memory
{
    public interface IViewAccessor
        : IDisposable
    {
        /// <summary>
        /// End of view
        /// </summary>
        bool EOV { get; }
        long Position { get; }
        byte[] ReadBuffer(int count);
        bool CheckOutOfRange(int offset);
        void Seek(long offset);
    }
}
