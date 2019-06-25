using System;
using ZeroLevel._Network;

namespace ZeroLevel.Services._Network
{
    public class NetworkNode
        : IClient, IRouter
    {
        private FrameParser _parser = new FrameParser();
        private readonly ISocketClient _client;
        private readonly IRouter _router;
        private DateTime _lastConnectionTime;

        public NetworkNode(ISocketClient client, IRouter router)
        {
            _lastConnectionTime = DateTime.UtcNow;
            _client = client;
            _router = router;
            _parser.OnIncoming += _parser_OnIncoming;
            _client.OnIncomingData += _readerWriter_OnIncomingData;
        }

        private void _readerWriter_OnIncomingData(byte[] data, int length)
        {
            _parser.Push(data, length);
        }

        private void _parser_OnIncoming(FrameType type, int identity, byte[] data)
        {
            switch (type)
            {
                case FrameType.KeepAlive:
                    _lastConnectionTime = DateTime.UtcNow;
                    break;
                case FrameType.Message:
                    break;
                case FrameType.Request:
                    break;
                case FrameType.Response:
                    break;
            }
        }

        public void Send(string inbox)
        {
            throw new System.NotImplementedException();
        }

        public void Send(string inbox, byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public void Send<T>(string inbox, T message)
        {
            throw new System.NotImplementedException();
        }

        public byte[] Request(string inbox)
        {
            throw new System.NotImplementedException();
        }

        public byte[] Request(string inbox, byte[] data)
        {
            throw new System.NotImplementedException();
        }

        public Tresponse Request<Tresponse>(string inbox)
        {
            throw new System.NotImplementedException();
        }

        public Tresponse Request<Tresponse, Trequest>(string inbox, Trequest request)
        {
            throw new System.NotImplementedException();
        }

        #region IRouter
        public void RegisterInbox(string inbox, MessageHandler handler)
        {
            _router.RegisterInbox(inbox, handler);
        }

        public void RegisterInbox<T>(string inbox, MessageHandler<T> handler)
        {
            _router.RegisterInbox<T>(inbox, handler);
        }

        public void RegisterInbox(MessageHandler handler)
        {
            _router.RegisterInbox(handler);
        }

        public void RegisterInbox<T>(MessageHandler<T> handler)
        {
            _router.RegisterInbox<T>(handler);
        }

        public void RegisterInbox<Tresponse>(string inbox, RequestHandler<Tresponse> handler)
        {
            _router.RegisterInbox<Tresponse>(inbox, handler);
        }

        public void RegisterInbox<Trequest, Tresponse>(string inbox, RequestHandler<Trequest, Tresponse> handler)
        {
            _router.RegisterInbox<Trequest, Tresponse>(inbox, handler);
        }

        public void RegisterInbox<Tresponse>(RequestHandler<Tresponse> handler)
        {
            _router.RegisterInbox<Tresponse>(handler);
        }

        public void RegisterInbox<Trequest, Tresponse>(RequestHandler<Trequest, Tresponse> handler)
        {
            _router.RegisterInbox<Trequest, Tresponse>(handler);
        }
        #endregion
    }
}
