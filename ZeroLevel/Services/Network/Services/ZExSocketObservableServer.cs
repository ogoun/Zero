using System;
using System.Net;
using ZeroLevel.Services.Network.Contract;

namespace ZeroLevel.Services.Network
{
    public class ZExSocketObservableServer :
        ZSocketServer, IZObservableServer
    {
        public ZExSocketObservableServer(IPEndPoint endpoint) 
            : base(endpoint)
        {
        }

        public IPEndPoint Endpoint => base.LocalEndpoint;

        public event Action<Frame, IZBackward> OnMessage = (f, c) => { };
        public event Func<Frame, IZBackward, Frame> OnRequest = (f, c) => null;

        protected override void Handle(Frame frame, IZBackward client)
        {
            OnMessage(frame, client);
        }

        protected override Frame HandleRequest(Frame frame, IZBackward client)
        {
            return OnRequest(frame, client);
        }
    }
}
