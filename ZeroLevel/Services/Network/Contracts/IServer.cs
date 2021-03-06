﻿using System;
using System.Collections.Generic;
using ZeroLevel.Network.SDL;

namespace ZeroLevel.Network
{
    public interface IServer
    {
        #region Messages
        IServer RegisterInbox(string inbox, MessageHandler handler);
        IServer RegisterInbox<T>(string inbox, MessageHandler<T> handler);
        IServer RegisterInbox(MessageHandler handler);
        IServer RegisterInbox<T>(MessageHandler<T> handler);

        IServer RegisterInboxIfNoExists(string inbox, MessageHandler handler);
        IServer RegisterInboxIfNoExists<T>(string inbox, MessageHandler<T> handler);
        IServer RegisterInboxIfNoExists(MessageHandler handler);
        IServer RegisterInboxIfNoExists<T>(MessageHandler<T> handler);
        #endregion

        #region Requests
        IServer RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler);
        IServer RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler);

        // Default inboxe
        IServer RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler);
        IServer RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler);
        #endregion

        bool ContainsInbox(string inbox);
        bool ContainsHandlerInbox(string inbox);
        bool ContainsRequestorInbox(string inbox);

        IEnumerable<InboxServiceDescription> CollectInboxInfo();

        event Action<ISocketClient> OnDisconnect;
        event Action<IClient> OnConnect;
    }
}
