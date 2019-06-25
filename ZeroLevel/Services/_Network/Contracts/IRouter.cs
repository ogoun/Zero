namespace ZeroLevel.Services._Network
{
    public interface IRouter
    {
        #region Messages
        void RegisterInbox(string inbox, MessageHandler handler);
        void RegisterInbox<T>(string inbox, MessageHandler<T> handler);

        // Default inboxe
        void RegisterInbox(MessageHandler handler);
        void RegisterInbox<T>(MessageHandler<T> handler);
        #endregion

        #region Requests
        void RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler);
        void RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler);

        // Default inboxe
        void RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler);
        void RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler);
        #endregion
    }

    public interface IClient
    {
        void Send(string inbox);
        void Send(string inbox, byte[] data);
        void Send<T>(string inbox, T message);

        byte[] Request(string inbox);
        byte[] Request(string inbox, byte[] data);
        Tresponse Request<Tresponse>(string inbox);
        Tresponse Request<Tresponse, Trequest>(string inbox, Trequest request);
    }
}
