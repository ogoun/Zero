using System;
using System.Collections.Generic;

namespace ZeroLevel.Network.Microservices
{
    /// <summary>
    /// Информация о точках подключения для сервиса
    /// </summary>
    public class ServiceEndpointsInfo :
        IEquatable<ServiceEndpointsInfo>
    {
        public string ServiceKey { get; set; }
        public string Version { get; set; }
        public string ServiceGroup { get; set; }
        public string ServiceType { get; set; }

        public List<ServiceEndpointInfo> Endpoints { get; set; }

        public bool Equals(ServiceEndpointsInfo other)
        {
            if (other == null) return false;
            if (string.Compare(this.ServiceKey, other.ServiceKey, true) != 0) return false;
            if (string.Compare(this.Version, other.Version, true) != 0) return false;
            if (string.Compare(this.ServiceGroup, other.ServiceGroup, true) != 0) return false;
            if (string.Compare(this.ServiceType, other.ServiceType, true) != 0) return false;
            if (false == CollectionComparsionExtensions.OrderingEquals(this.Endpoints, other.Endpoints, (a, b) => a.Equals(b))) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ServiceEndpointsInfo);
        }

        public override int GetHashCode()
        {
            return ServiceKey?.GetHashCode() ?? 0 ^ Version?.GetHashCode() ?? 0 ^ ServiceGroup?.GetHashCode() ?? 0 ^ ServiceType?.GetHashCode() ?? 0;
        }
    }
}
