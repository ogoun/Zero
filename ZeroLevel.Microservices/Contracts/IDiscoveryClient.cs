using System.Collections.Generic;
using ZeroLevel.Network.Microservices;

namespace ZeroLevel.Microservices.Contracts
{
    public interface IDiscoveryClient
    {
        void Register(MicroserviceInfo info);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpoints(string serviceKey);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByGroup(string serviceGroup);

        IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByType(string serviceType);

        ServiceEndpointInfo GetService(string serviceKey, string endpoint);
    }
}