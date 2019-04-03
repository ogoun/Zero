using System;
using System.Collections.Generic;
using System.Net;

namespace ZeroLevel.Services.Network.Contract
{
    public interface IZObservableServer
        : IDisposable
    {
        IPEndPoint Endpoint { get; }
        IEnumerable<IPEndPoint> ConnectionList { get; }

        event Action<IZBackward> OnConnect;

        event Action<IZBackward> OnDisconnect;

        event Action<Frame, IZBackward> OnMessage;

        event Func<Frame, IZBackward, Frame> OnRequest;
    }
}