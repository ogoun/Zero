using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace ZeroLevel.Network
{
    internal sealed class ExClientServerCachee
        : IDisposable
    {
        private static readonly ConcurrentDictionary<string, ExClient> _clientInstances = new ConcurrentDictionary<string, ExClient>();
        private static readonly ConcurrentDictionary<string, object> _clientLocks = new ConcurrentDictionary<string, object>();

        private readonly ConcurrentDictionary<string, SocketServer> _serverInstances = new ConcurrentDictionary<string, SocketServer>();

        internal IEnumerable<SocketServer> ServerList => _serverInstances.Values;

        public ExClient GetClient(IPEndPoint endpoint, bool use_cachee, IRouter router = null)
        {
            if (use_cachee)
            {
                string key = $"{endpoint.Address}:{endpoint.Port}";
                if (false == _clientLocks.ContainsKey(key))
                {
                    _clientLocks.TryAdd(key, new object());
                }
                lock (_clientLocks[key])
                {
                    try
                    {
                        ExClient instance = null;
                        if (_clientInstances.ContainsKey(key))
                        {
                            instance = _clientInstances[key];
                            if (instance.Status == SocketClientStatus.Working)
                            {
                                return instance;
                            }
                            _clientInstances.TryRemove(key, out instance);
                            instance.Dispose();
                            instance = null;
                        }
                        instance = new ExClient(new SocketClient(endpoint, router ?? new Router()));
                        instance.ForceConnect();
                        if (instance.Status == SocketClientStatus.Initialized
                            || instance.Status == SocketClientStatus.Working)
                        {
                            _clientInstances[key] = instance;
                            instance.Socket.UseKeepAlive(TimeSpan.FromMilliseconds(BaseSocket.MINIMUM_HEARTBEAT_UPDATE_PERIOD_MS));
                            return instance;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[ExClientServerCachee.GetClient] Can't create ExClient for {key}");
                    }
                }
            }
            else
            {
                var instance = new ExClient(new SocketClient(endpoint, router ?? new Router()));
                if (instance.Status == SocketClientStatus.Initialized
                   || instance.Status == SocketClientStatus.Working)
                {
                    return instance;
                }
            }
            return null;
        }

        public SocketServer GetServer(IPEndPoint endpoint, IRouter router)
        {
            string key = $"{endpoint.Address}:{endpoint.Port}";
            if (_serverInstances.ContainsKey(key))
            {
                return _serverInstances[key];
            }
            var instance = new SocketServer(endpoint, router);
            _serverInstances[key] = instance;
            return instance;
        }

        public void Dispose()
        {
            ExClient removed;
            var clients = new HashSet<string>(_clientInstances.Keys);
            foreach (var client in clients)
            {
                try
                {
                    if (_clientInstances.TryRemove(client, out removed))
                    {
                        removed.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[ExClientServerCachee.Dispose()] Dispose SocketClient to endpoint {client}");
                }
            }
            foreach (var server in _serverInstances)
            {
                try
                {
                    server.Value.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[ExClientServerCachee.Dispose()] Dispose SocketServer with endpoint {server.Key}");
                }
            }
            _serverInstances.Clear();
        }
    }
}
