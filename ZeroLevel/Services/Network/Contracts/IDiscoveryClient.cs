using System;
using System.Collections.Generic;

namespace ZeroLevel.Network
{
    public interface IDiscoveryClient
        : IDisposable
    {
        bool Register(ZeroServiceInfo info);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpoints(string serviceKey);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByGroup(string serviceGroup);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByType(string serviceType);

        ServiceEndpointInfo GetService(string serviceKey, string endpoint);
    }
}