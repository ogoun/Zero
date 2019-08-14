using System;

namespace ZeroLevel.Network
{
    public interface IRouter
        : IServer
    {
        void HandleMessage(Frame frame, ISocketClient client);
        void HandleRequest(Frame frame, ISocketClient client, int identity, Action<int, byte[]> handler);
    }
}
