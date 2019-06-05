using System;
using System.Net;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    public interface IExClient
        : IDisposable
    {
        event Action Connected;

        void ForceConnect();

        ZTransportStatus Status { get; }

        IPEndPoint Endpoint { get; }

        InvokeResult Send<T>(T obj);

        InvokeResult Send<T>(string inbox, T obj);

        InvokeResult Request<Treq, Tresp>(Treq obj, Action<Tresp> callback);

        InvokeResult Request<Treq, Tresp>(string inbox, Treq obj, Action<Tresp> callback);

        InvokeResult Request<Tresp>(Action<Tresp> callback);

        InvokeResult Request<Tresp>(string inbox, Action<Tresp> callback);

        void RegisterInbox<T>(string inbox, Action<T, long, IZBackward> handler);

        void RegisterInbox<T>(Action<T, long, IZBackward> handler);
    }
}