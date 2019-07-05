using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Models;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public sealed class ExClientSet
        : IClientSet, IDisposable
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #region IMultiClient
        public InvokeResult Request<Tresponse>(string alias, Action<Tresponse> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult Request<Tresponse>(string alias, string inbox, Action<Tresponse> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult Request<Trequest, Tresponse>(string alias, Trequest request, Action<Tresponse> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult Request<Trequest, Tresponse>(string alias, string inbox, Trequest request, Action<Tresponse> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcast<Tresponse>(string alias, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcast<Tresponse>(string alias, string inbox, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcast<Trequest, Tresponse>(string alias, Trequest data, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcast<Trequest, Tresponse>(string alias, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByGroup<Tresponse>(string serviceGroup, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByGroup<Tresponse>(string serviceGroup, string inbox, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, Trequest data, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByType<Tresponse>(string serviceType, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByType<Tresponse>(string serviceType, string inbox, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByType<Trequest, Tresponse>(string serviceType, Trequest data, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult RequestBroadcastByType<Trequest, Tresponse>(string serviceType, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback)
        {
            throw new NotImplementedException();
        }

        public InvokeResult Send<T>(string alias, T data)
        {
            throw new NotImplementedException();
        }

        public InvokeResult Send<T>(string alias, string inbox, T data)
        {
            throw new NotImplementedException();
        }

        public InvokeResult SendBroadcast<T>(string alias, T data)
        {
            throw new NotImplementedException();
        }

        public InvokeResult SendBroadcast<T>(string alias, string inbox, T data)
        {
            throw new NotImplementedException();
        }

        public InvokeResult SendBroadcastByGroup<T>(string serviceGroup, T data)
        {
            throw new NotImplementedException();
        }

        public InvokeResult SendBroadcastByGroup<T>(string serviceGroup, string inbox, T data)
        {
            throw new NotImplementedException();
        }

        public InvokeResult SendBroadcastByType<T>(string serviceType, T data)
        {
            throw new NotImplementedException();
        }

        public InvokeResult SendBroadcastByType<T>(string serviceType, string inbox, T data)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Private
        private IEnumerable<Tresp> _RequestBroadcast<Treq, Tresp>(List<ExClient> clients, string inbox, Treq data)
        {
            var response = new List<Tresp>();
            using (var waiter = new CountdownEvent(clients.Count))
            {
                foreach (var client in clients)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (false == client.Request<Treq, Tresp>(inbox, data, resp => { waiter.Signal(); response.Add(resp); }).Success)
                            {
                                waiter.Signal();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[ExClientSet._RequestBroadcast] Error direct request to service '{client.EndPoint}' in broadcast request. Inbox '{inbox}'");
                            waiter.Signal();
                        }
                    });
                }
                waiter.Wait(BaseSocket.MAX_REQUEST_TIME_MS);
            }
            return response;
        }

        private IEnumerable<Tresp> _RequestBroadcast<Tresp>(List<ExClient> clients, string inbox)
        {
            var response = new List<Tresp>();
            using (var waiter = new CountdownEvent(clients.Count))
            {
                foreach (var client in clients)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (false == client.Request<Tresp>(inbox, resp => { waiter.Signal(); response.Add(resp); }).Success)
                            {
                                waiter.Signal();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[ExClientSet._RequestBroadcast] Error direct request to service '{client.EndPoint}' in broadcast request. Inbox '{inbox}'");
                            waiter.Signal();
                        }
                    });
                }
                waiter.Wait(BaseSocket.MAX_REQUEST_TIME_MS);
            }
            return response;
        }
        #endregion
    }

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

        public InvokeResult Send<T>(T message)
        {
            try
            {
                _client.Send(Frame.FromPool(BaseSocket.DEFAULT_MESSAGE_INBOX, MessageSerializer.SerializeCompatible<T>(message)));
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
                _client.Request(Frame.FromPool(inbox), f => callback(f));
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
                _client.Request(Frame.FromPool(inbox, data), f => callback(f));
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
                _client.Request(Frame.FromPool(inbox), f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Request<Tresponse>(Action<Tresponse> callback)
        {
            try
            {
                _client.Request(Frame.FromPool(BaseSocket.DEFAULT_REQUEST_INBOX), f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
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
                    f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[NetworkNode.Request]");
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Request<Trequest, Tresponse>(Trequest request, Action<Tresponse> callback)
        {
            try
            {
                _client.Request(Frame.FromPool(BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, MessageSerializer.SerializeCompatible<Trequest>(request)),
                    f => callback(MessageSerializer.DeserializeCompatible<Tresponse>(f)));
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
