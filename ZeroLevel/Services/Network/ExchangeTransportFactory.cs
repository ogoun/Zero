using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using ZeroLevel.Network.Microservices;
using ZeroLevel.Services.Network;
using ZeroLevel.Services.Network.Contract;

namespace ZeroLevel.Network
{
    public static class ExchangeTransportFactory
    {
        private static readonly Dictionary<string, Type> _customServers = new Dictionary<string, Type>();
        private static readonly Dictionary<string, Type> _customClients = new Dictionary<string, Type>();
        private static readonly ConcurrentDictionary<string, IExClient> _clientInstances = new ConcurrentDictionary<string, IExClient>();

        /// <summary>
        /// Scanning the specified assembly to find the types that implement the IExchangeServer or IExchangeClient interfaces
        /// </summary>
        internal static void ScanAndRegisterCustomTransport(Assembly asm)
        {
            foreach (var type in asm.GetExportedTypes())
            {
                var serverAttr = type.GetCustomAttribute<ExchangeServerAttribute>();
                if (serverAttr != null &&
                    string.IsNullOrWhiteSpace(serverAttr.Protocol) == false &&
                    typeof(IZObservableServer).IsAssignableFrom(type))
                {
                    _customServers[serverAttr.Protocol] = type;
                }
                var clientAttr = type.GetCustomAttribute<ExchangeClientAttribute>();
                if (clientAttr != null &&
                    string.IsNullOrWhiteSpace(clientAttr.Protocol) == false &&
                    typeof(IZTransport).IsAssignableFrom(type))
                {
                    _customClients[clientAttr.Protocol] = type;
                }
            }
        }

        /// <summary>
        /// Creates a server to receive messages using the specified protocol
        /// </summary>
        /// <param name="protocol">Protocol</param>
        /// <returns>Server</returns>
        public static IExService GetServer(string protocol, int port = -1)
        {
            ExService instance = null;
            if (protocol.Equals("socket", StringComparison.OrdinalIgnoreCase))
            {
                instance = new ExService(new ZExSocketObservableServer(new System.Net.IPEndPoint(NetUtils.GetNonLoopbackAddress(), port == -1 ? NetUtils.GetFreeTcpPort() : port)));
            }
            else
            {
                var key = protocol.Trim().ToLowerInvariant();
                if (_customServers.ContainsKey(key))
                {
                    instance = new ExService((IZObservableServer)Activator.CreateInstance(_customServers[key]));
                }
            }
            if (instance != null)
            {
                return instance;
            }
            throw new NotSupportedException($"Protocol {protocol} not supported");
        }
        /// <summary>
        /// Creates a client to access the server using the specified protocol
        /// </summary>
        /// <param name="protocol">Protocol</param>
        /// <param name="endpoint">Server endpoint</param>
        /// <returns>Client</returns>
        public static IExClient GetClientWithCache(string protocol, string endpoint)
        {
            IExClient instance = null;
            var cachee_key = $"{protocol}:{endpoint}";
            if (_clientInstances.ContainsKey(cachee_key))
            {
                instance = _clientInstances[cachee_key];
                if (instance.Status == ZTransportStatus.Working)
                {
                    return instance;
                }
                _clientInstances.TryRemove(cachee_key, out instance);
                instance.Dispose();
                instance = null;
            }
            instance = GetClient(protocol, endpoint);
            _clientInstances[cachee_key] = instance;
            return instance;
        }

        public static IExClient GetClient(string protocol, string endpoint)
        {
            ExClient instance = null;
            if (protocol.Equals("socket", StringComparison.OrdinalIgnoreCase))
            {
                instance = new ExClient(new ZSocketClient(NetUtils.CreateIPEndPoint(endpoint)));
            }
            else
            {
                var key = protocol.Trim().ToLowerInvariant();
                if (_customClients.ContainsKey(key))
                {
                    instance = new ExClient((IZTransport)Activator.CreateInstance(_customClients[key], new object[] { endpoint }));
                }
            }
            if (instance != null)
            {
                return instance;
            }
            throw new NotSupportedException($"Protocol {protocol} not supported");
        }
    }
}