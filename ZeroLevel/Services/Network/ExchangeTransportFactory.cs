using System.Collections.Concurrent;
using System.Net;

namespace ZeroLevel.Network
{
    public static class ExchangeTransportFactory
    {
        private static readonly ConcurrentDictionary<string, IExClient> _clientInstances = new ConcurrentDictionary<string, IExClient>();

        /// <summary>
        /// Creates a server to receive messages using the specified protocol
        /// </summary>
        /// <param name="protocol">Protocol</param>
        /// <returns>Server</returns>
        public static IExService GetServer(int port = -1)
        {
            return new ExService(new ZExSocketObservableServer(new System.Net.IPEndPoint(IPAddress.Any, port == -1 ? NetUtils.GetFreeTcpPort() : port)));
        }
        /// <summary>
        /// Creates a client to access the server using the specified protocol
        /// </summary>
        /// <param name="protocol">Protocol</param>
        /// <param name="endpoint">Server endpoint</param>
        /// <returns>Client</returns>
        public static IExClient GetClientWithCache(string endpoint)
        {
            IExClient instance = null;
            if (_clientInstances.ContainsKey(endpoint))
            {
                instance = _clientInstances[endpoint];
                if (instance.Status == ZTransportStatus.Working)
                {
                    return instance;
                }
                _clientInstances.TryRemove(endpoint, out instance);
                instance.Dispose();
                instance = null;
            }
            instance = GetClient(endpoint);
            _clientInstances[endpoint] = instance;
            return instance;
        }

        public static IExClient GetClient(string endpoint)
        {
            return new ExClient(new ZSocketClient(NetUtils.CreateIPEndPoint(endpoint)));
        }
    }
}