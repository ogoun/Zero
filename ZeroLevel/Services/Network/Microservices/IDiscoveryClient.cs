using System.Collections.Generic;
using ZeroLevel.Network.Microservices;

namespace ZeroLevel.Network
{
    public interface IDiscoveryClient
    {
        bool Register(MicroserviceInfo info);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpoints(string serviceKey);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByGroup(string serviceGroup);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByType(string serviceType);

        ServiceEndpointInfo GetService(string serviceKey, string endpoint);
    }
}