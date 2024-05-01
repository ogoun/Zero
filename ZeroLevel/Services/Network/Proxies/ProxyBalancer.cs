using System.Collections.Generic;
using System.Net;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Services.Network.Proxies
{
    internal sealed class ProxyBalancer
    {
        private RoundRobinCollection<IPEndPoint> _servers;

        public ProxyBalancer()
        {
            _servers = new RoundRobinCollection<IPEndPoint>();
        }

        public ProxyBalancer(IEnumerable<IPEndPoint> endpoints)
        {
            _servers = new RoundRobinCollection<IPEndPoint>(endpoints);
        }

        public void AddEndpoint(IPEndPoint ep) => _servers.Add(ep);

        public IPEndPoint GetServerProxy()
        {
            if (_servers.MoveNext())
            {
                return _servers.Current;
            }
            return null!;
        }
    }
}
