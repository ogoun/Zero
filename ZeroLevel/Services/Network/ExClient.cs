using System;
using System.Net;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    internal sealed class ExClient
        : IClient, IDisposable
    {
        private readonly ISocketClient _client;
        public IPEndPoint EndPoint => _client?.Endpoint;
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
                _client.Send(Frame.FromPool(inbox));
                return true;
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
                _client.Send(Frame.FromPool(inbox, data));
                return true;
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
                _client.Send(Frame.FromPool(BaseSocket.DEFAULT_MESSAGE_INBOX, MessageSerializer.SerializeCompatible<T>(message)));
                return true;
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
                _client.Send(Frame.FromPool(inbox, MessageSerializer.SerializeCompatible<T>(message)));
                return true;
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
                _client.Request(Frame.FromPool(inbox), f => callback(f));
                return true;
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
                _client.Request(Frame.FromPool(inbox, data), f => callback(f));
                return true;
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
                _client.Request(Frame.FromPool(inbox), f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
                return true;
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
                _client.Request(Frame.FromPool(BaseSocket.DEFAULT_REQUEST_INBOX), f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
                return true;
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
                _client.Request(Frame.FromPool(inbox, MessageSerializer.SerializeCompatible<Trequest>(request)),
                    f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
                return true;
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
                _client.Request(Frame.FromPool(BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, MessageSerializer.SerializeCompatible<Trequest>(request)),
                    f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
                return true;
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
