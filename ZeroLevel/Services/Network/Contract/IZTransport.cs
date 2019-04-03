using System;
using System.Net;

namespace ZeroLevel.Services.Network.Contract
{
    public interface IZTransport
        : IDisposable
    {
        event Action OnConnect;

        event Action OnDisconnect;

        event EventHandler<Frame> OnServerMessage;

        IPEndPoint Endpoint { get; }
        ZTransportStatus Status { get; }

        void EnsureConnection();

        void Send(Frame frame);

        void Request(Frame frame, Action<Frame> callback, Action<string> fail = null);
    }
}