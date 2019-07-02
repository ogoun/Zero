using System;
using System.Net;
using ZeroLevel.Models;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public sealed class ExClient
        : IClient, IDisposable
    {
        private readonly ISocketClient _client;
        public IPEndPoint EndPoint => _client?.Endpoint;
        public SocketClientStatus Status => _client.Status;
        public IRouter Router => _client.Router;

        public ExClient(ISocketClient client)
        {
            _client = client;
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

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
