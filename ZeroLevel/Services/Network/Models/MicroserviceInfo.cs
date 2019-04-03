using System;
using System.Runtime.Serialization;

namespace ZeroLevel.Network.Microservices
{
    [Serializable]
    [DataContract]
    public sealed class MicroserviceInfo :
       IEquatable<MicroserviceInfo>
    {
        public const string DEFAULT_GROUP_NAME = "__service_default_group__";
        public const string DEFAULT_TYPE_NAME = "__service_default_type__";

        /// <summary>
        /// Service key, must be unique within the business functionality.
        /// two services with same key will be horizontally balanced
        /// </summary>
        [DataMember]
        public string ServiceKey { get; set; }

        /// <summary>
        /// The group can determine the services working in the same domain
        /// </summary>
        [DataMember]
        public string ServiceGroup { get; set; } = DEFAULT_GROUP_NAME;

        /// <summary>
        /// The type of service, for filtering, determines membership in a subgroup.
        /// </summary>
        [DataMember]
        public string ServiceType { get; set; } = DEFAULT_TYPE_NAME;

        /// <summary>
        /// Protocol on which access to the service API is allowed
        /// </summary>
        [DataMember]
        public string Protocol { get; set; }

        /// <summary>
        /// Connection point, address
        /// </summary>
        [DataMember]
        public string Endpoint { get; set; }

        /// <summary>
        /// Service version
        /// </summary>
        [DataMember]
        public string Version { get; set; }

        public bool Equals(MicroserviceInfo other)
        {
            if (other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (string.Compare(this.ServiceKey, other.ServiceKey, true) != 0) return false;
            if (string.Compare(this.ServiceGroup, other.ServiceGroup, true) != 0) return false;
            if (string.Compare(this.ServiceType, other.ServiceType, true) != 0) return false;

            if (string.Compare(this.Endpoint, other.Endpoint, true) != 0) return false;
            if (string.Compare(this.Version, other.Version, true) != 0) return false;
            if (string.Compare(this.Protocol, other.Protocol, true) != 0) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.ServiceKey.GetHashCode() ^ this.Protocol.GetHashCode() ^ this.Endpoint.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ServiceKey} ({Version})";
        }
    }
}