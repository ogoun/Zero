﻿using System;
using System.Net;

namespace ZeroLevel.Network
{
    public interface IExchange
        : IClientSet, IDisposable
    {
        bool UseDiscovery();
        bool UseDiscovery(string endpoint);
        bool UseDiscovery(IPEndPoint endpoint);

        IRouter UseHost();
        IRouter UseHost(int port);
        IRouter UseHostV6();
        IRouter UseHostV6(int port);

        IRouter UseHost(IPEndPoint endpoint);

        IServiceRoutesStorage RoutesStorage { get; }
        IServiceRoutesStorage DiscoveryStorage { get; }

        IClient GetConnection(ISocketClient client);
        IClient GetConnection(string alias);
        IClient GetConnection(IPEndPoint endpoint);
    }
}
