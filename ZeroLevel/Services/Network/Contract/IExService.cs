using System;
using System.Net;

namespace ZeroLevel.Network
{
    public interface IExService
        : IDisposable
    {
        IPEndPoint Endpoint { get; }
        event Action<IZBackward> OnConnect;
        event Action<IZBackward> OnDisconnect;

        void RegisterInbox<T>(string inbox, Action<T, long, IZBackward> handler);

        void RegisterInbox<Treq, Tresp>(string inbox, Func<Treq, long, IZBackward, Tresp> handler);

        /// <summary>
        /// Replier without request
        /// </summary>
        void RegisterInbox<Tresp>(string inbox, Func<long, IZBackward, Tresp> handler);

/*
DEFAULT INBOXES         
*/
        void RegisterInbox<T>(Action<T, long, IZBackward> handler);
        void RegisterInbox<Treq, Tresp>(Func<Treq, long, IZBackward, Tresp> handler);
        void RegisterInbox<Tresp>(Func<long, IZBackward, Tresp> handler);
    }
}