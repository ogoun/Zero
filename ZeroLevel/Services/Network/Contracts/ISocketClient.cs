using System;
using System.Net;

namespace ZeroLevel.Network
{
    public interface ISocketClient:
        IDisposable
    {
        event Action<ISocketClient, byte[], int> OnIncomingData;
        event Action<ISocketClient> OnConnect;
        event Action<ISocketClient> OnDisconnect;
        IPEndPoint Endpoint { get; }
        SocketClientStatus Status { get; }

        IRouter Router { get; }

        void ForceConnect();
        void UseKeepAlive(TimeSpan period);
        void Send(Frame data);
        void Request(Frame data, Action<byte[]> callback, Action<string> fail = null);
        void Response(byte[] data, int identity);
    }
}
