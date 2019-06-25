namespace ZeroLevel.Services._Network
{
    public delegate void MessageHandler(ISocketClient client);
    public delegate void MessageHandler<T>(ISocketClient client, T message);
    public delegate Tresponse RequestHandler<Tresponse>(ISocketClient client);
    public delegate Tresponse RequestHandler<Trequest, Tresponse>(ISocketClient client, Trequest request);
}
