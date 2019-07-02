namespace ZeroLevel.Network
{
    public interface IRouter
        : IServer
    {
        void HandleMessage(Frame frame, ISocketClient client);
        byte[] HandleRequest(Frame frame, ISocketClient client);
    }
}
