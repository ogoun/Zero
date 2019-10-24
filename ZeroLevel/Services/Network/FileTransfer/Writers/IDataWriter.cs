using System;

namespace ZeroLevel.Services.Network.FileTransfer
{
    interface IDataWriter
        : IDisposable
    {
        void Write(long offset, byte[] data);
        bool IsTimeoutBy(TimeSpan period); 
        void CompleteReceiving();
    }
}
