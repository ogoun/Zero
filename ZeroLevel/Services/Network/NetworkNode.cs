using System;
using System.Net;
using ZeroLevel.Models;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public class NetworkNode
        : IClient, IRouter, IDisposable
    {
        private FrameParser _parser = new FrameParser();
        private readonly ISocketClient _client;
        private readonly Router _router;
        private DateTime _lastConnectionTime;
        public IPEndPoint EndPoint => _client?.Endpoint;
        public SocketClientStatus Status => _client.Status;

        public NetworkNode(ISocketClient client)
        {
            _lastConnectionTime = DateTime.UtcNow;
            _client = client;
            _router = new Router();
            _parser.OnIncoming += _parser_OnIncoming;
            _client.OnIncomingData += _readerWriter_OnIncomingData;
        }

        private void _readerWriter_OnIncomingData(ISocketClient client, byte[] data, int length)
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
                    _router.HandleMessage(MessageSerializer.Deserialize<Frame>(data), _client);
                    break;
                case FrameType.Request:
                    var response = _router.HandleRequest(MessageSerializer.Deserialize<Frame>(data), _client);
                    _client.Response(response, identity);
                    break;
            }
        }

        public void ForceConnect() => _client.ForceConnect();

        public InvokeResult Send(string inbox)
        {
            try
            {
                _client.Send(Frame.FromPool(inbox));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Send]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Send(string inbox, byte[] data)
        {
            try
            {
                _client.Send(Frame.FromPool(inbox, data));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Send]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Send<T>(string inbox, T message)
        {
            try
            {
                _client.Send(Frame.FromPool(inbox, MessageSerializer.SerializeCompatible<T>(message)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Send]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Request(string inbox, Action<byte[]> callback)
        {
            try
            {
                _client.Request(Frame.FromPool(inbox), f => callback(f.Payload));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Request(string inbox, byte[] data, Action<byte[]> callback)
        {
            try
            {
                _client.Request(Frame.FromPool(inbox, data), f => callback(f.Payload));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Request<Tresponse>(string inbox, Action<Tresponse> callback)
        {
            try
            {
                _client.Request(Frame.FromPool(inbox), f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f.Payload)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Request<Trequest, Tresponse>(string inbox, Trequest request, Action<Tresponse> callback)
        {
            try
            {
                _client.Request(Frame.FromPool(inbox, MessageSerializer.SerializeCompatible<Trequest>(request)), 
                    f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f.Payload)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
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

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
