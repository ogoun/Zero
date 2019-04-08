using System;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    /// <summary>
    /// Information about service connection points
    /// </summary>
    public class ServiceEndpointsInfo :
        IEquatable<ServiceEndpointsInfo>, IBinarySerializable
    {
        public string ServiceKey { get; set; }
        public string Version { get; set; }
        public string ServiceGroup { get; set; }
        public string ServiceType { get; set; }
        public List<string> Endpoints { get; set; }

        public bool Equals(ServiceEndpointsInfo other)
        {
            if (other == null) return false;
            if (string.Compare(this.ServiceKey, other.ServiceKey, true) != 0) return false;
            if (string.Compare(this.Version, other.Version, true) != 0) return false;
            if (string.Compare(this.ServiceGroup, other.ServiceGroup, true) != 0) return false;
            if (string.Compare(this.ServiceType, other.ServiceType, true) != 0) return false;
            if (!CollectionComparsionExtensions.NoOrderingEquals(this.Endpoints, other.Endpoints, (a, b) => a.Equals(b))) return false;
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

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.ServiceKey);
            writer.WriteString(this.Version);
            writer.WriteString(this.ServiceGroup);
            writer.WriteString(this.ServiceType);
            writer.WriteCollection(this.Endpoints);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.ServiceKey = reader.ReadString();
            this.Version = reader.ReadString();
            this.ServiceGroup = reader.ReadString();
            this.ServiceType = reader.ReadString();
            this.Endpoints = reader.ReadStringCollection();
        }
    }
}