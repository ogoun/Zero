using System;
using System.Net;

namespace ZeroLevel.Network
{
    public interface IClient
        : IDisposable
    {
        IPEndPoint EndPoint { get; }
        SocketClientStatus Status { get; }
        IRouter Router { get; }
        ISocketClient Socket { get; }

        bool Send<T>(T message);
        bool Send(string inbox);
        bool Send(string inbox, byte[] data);
        bool Send<T>(string inbox, T message);

        bool Request(string inbox, Action<byte[]> callback);
        bool Request(string inbox, byte[] data, Action<byte[]> callback);
        bool Request<Tresponse>(string inbox, Action<Tresponse> callback);
        bool Request<Trequest, Tresponse>(string inbox, Trequest request, Action<Tresponse> callback);
    }
}
