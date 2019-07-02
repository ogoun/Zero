namespace ZeroLevel.Network
{
    public interface IServer
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
}
