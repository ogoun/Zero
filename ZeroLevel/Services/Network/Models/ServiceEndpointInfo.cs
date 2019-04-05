using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    /// <summary>
    /// Endpoint
    /// </summary>
    public class ServiceEndpointInfo :
        IEquatable<ServiceEndpointInfo>, IBinarySerializable
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

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Protocol);
            writer.WriteString(this.Endpoint);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Protocol = reader.ReadString();
            this.Endpoint = reader.ReadString();
        }
    }
}