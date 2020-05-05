using System;
using System.Net;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    internal sealed class ExClient
        : IClient, IDisposable
    {
        private readonly ISocketClient _client;
        public IPEndPoint Endpoint => _client?.Endpoint;
        public SocketClientStatus Status => _client.Status;
        public IRouter Router => _client.Router;
        public ISocketClient Socket => _client;

        public ExClient(ISocketClient client)
        {
            _client = client;
        }

        public bool Send(string inbox)
        {
            try
            {
                return _client.Send(FrameFactory.Create(inbox));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Send(inbox)]");
            }
            return false;
        }

        public bool Send(string inbox, byte[] data)
        {
            try
            {
                return _client.Send(FrameFactory.Create(inbox, data));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Send(inbox, data)]");
            }
            return false;
        }

        public bool Send<T>(T message)
        {
            try
            {
                return _client.Send(FrameFactory.Create(BaseSocket.DEFAULT_MESSAGE_INBOX, MessageSerializer.SerializeCompatible<T>(message)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Send(message)]");
            }
            return false;
        }

        public bool Send<T>(string inbox, T message)
        {
            try
            {
                return _client.Send(FrameFactory.Create(inbox, MessageSerializer.SerializeCompatible<T>(message)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Send(inbox, message)]");
            }
            return false;
        }

        public bool Request(string inbox, Action<byte[]> callback)
        {
            try
            {
                return _client.Request(FrameFactory.Create(inbox), f => callback(f));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request(inbox, callback)]");
            }
            return false;
        }

        public bool Request(string inbox, byte[] data, Action<byte[]> callback)
        {
            try
            {
                return _client.Request(FrameFactory.Create(inbox, data), f => callback(f));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request(inbox, data, callback)]");
            }
            return false;
        }

        public bool Request<Tresponse>(string inbox, Action<Tresponse> callback)
        {
            try
            {
                return _client.Request(FrameFactory.Create(inbox), f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request(inbox, callback)]");
            }
            return false;
        }

        public bool Request<Tresponse>(Action<Tresponse> callback)
        {
            try
            {
                return _client.Request(FrameFactory.Create(BaseSocket.DEFAULT_REQUEST_INBOX), f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request(callback)]");
            }
            return false;
        }

        public bool Request<Trequest, Tresponse>(string inbox, Trequest request, Action<Tresponse> callback)
        {
            try
            {
                return _client.Request(FrameFactory.Create(inbox, MessageSerializer.SerializeCompatible<Trequest>(request)),
                    f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request(inbox, request, callback)]");
            }
            return false;
        }

        public bool Request<Trequest, Tresponse>(Trequest request, Action<Tresponse> callback)
        {
            try
            {
                return _client.Request(FrameFactory.Create(BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, MessageSerializer.SerializeCompatible<Trequest>(request)),
                    f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request(request, callback)]");
            }
            return false;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
