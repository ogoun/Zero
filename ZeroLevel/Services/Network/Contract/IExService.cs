using System;
using System.Net;

namespace ZeroLevel.Services.Network
{
    public interface IExService
        : IDisposable
    {
        IPEndPoint Endpoint { get; }
        void RegisterInbox<T>(string inbox, Action<T, long, IZBackward> handler);
        void RegisterInbox<Treq, Tresp>(string inbox, Func<Treq, long, IZBackward, Tresp> handдer);
        /// <summary>
        /// Replier не принимающий данных
        /// </summary>
        void RegisterInbox<Tresp>(string inbox, Func<long, IZBackward, Tresp> handдer);
    }
}
