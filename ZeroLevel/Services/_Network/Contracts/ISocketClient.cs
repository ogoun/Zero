using System;

namespace ZeroLevel.Services._Network
{
    public interface ISocketClient
    {
        event Action<byte[], int> OnIncomingData;
        void UseKeepAlive(TimeSpan period);
        void Send(byte[] data);
        byte[] Request(byte[] data);
    }
}
