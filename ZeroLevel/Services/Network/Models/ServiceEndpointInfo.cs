using System;

namespace ZeroLevel.Network.Microservices
{
    /// <summary>
    /// Endpoint
    /// </summary>
    public class ServiceEndpointInfo :
        IEquatable<ServiceEndpointInfo>
    {
        public string Endpoint { get; set; }
        public string Protocol { get; set; }

        public bool Equals(ServiceEndpointInfo other)
        {
            if (other == null) return false;
            if (string.Compare(this.Endpoint, other.Endpoint, true) != 0) return false;
            if (string.Compare(this.Protocol, other.Protocol, true) != 0) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ServiceEndpointInfo);
        }

        public override int GetHashCode()
        {
            return Endpoint?.GetHashCode() ?? 0 ^ Protocol?.GetHashCode() ?? 0;
        }
    }
}