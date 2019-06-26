using System;
using ZeroLevel.Models;

namespace ZeroLevel.Network
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
        InvokeResult Send(string inbox);
        InvokeResult Send(string inbox, byte[] data);
        InvokeResult Send<T>(string inbox, T message);

        InvokeResult Request(string inbox, Action<byte[]> callback);
        InvokeResult Request(string inbox, byte[] data, Action<byte[]> callback);
        InvokeResult Request<Tresponse>(string inbox, Action<Tresponse> callback);
        InvokeResult Request<Trequest, Tresponse>(string inbox, Trequest request, Action<Tresponse> callback);
    }
}
