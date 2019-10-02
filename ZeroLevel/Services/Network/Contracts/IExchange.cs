using System;
using System.Net;

namespace ZeroLevel.Network
{
    public interface IExchange
        : IClientSet, IDisposable
    {
        void UseDiscovery();
        void UseDiscovery(string endpoint);
        void UseDiscovery(IPEndPoint endpoint);

        IRouter UseHost();
        IRouter UseHost(int port);
        IRouter UseHost(IPEndPoint endpoint);

        IServiceRoutesStorage RoutesStorage { get; }
        IServiceRoutesStorage DiscoveryStorage { get; }

        IClient GetConnection(string alias);
        IClient GetConnection(IPEndPoint endpoint);
    }
}
