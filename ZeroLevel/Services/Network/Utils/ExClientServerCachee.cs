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

        private readonly ConcurrentDictionary<string, SocketServer> _serverInstances = new ConcurrentDictionary<string, SocketServer>();
        private HashSet<string> _clients = new HashSet<string>();

        public ExClient GetClient(IPEndPoint endpoint, bool use_cachee, IRouter router = null)
        {
            if (use_cachee)
            {
                string key = $"{endpoint.Address}:{endpoint.Port}";
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
                _clientInstances[key] = instance;
                return instance;
            }
            return new ExClient(new SocketClient(endpoint, router ?? new Router()));
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
            foreach (var client in _clients)
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
            _clients.Clear();
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
