namespace ZeroLevel.Network
{
    public interface IServer
    {
        #region Messages
        IServer RegisterInbox(string inbox, MessageHandler handler);
        IServer RegisterInbox<T>(string inbox, MessageHandler<T> handler);

        // Default inboxe
        IServer RegisterInbox(MessageHandler handler);
        IServer RegisterInbox<T>(MessageHandler<T> handler);
        #endregion

        #region Requests
        IServer RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler);
        IServer RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler);

        // Default inboxe
        IServer RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler);
        IServer RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler);
        #endregion
    }
}
