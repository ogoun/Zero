using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    /// <summary>
    /// Endpoint
    /// </summary>
    public class ServiceEndpointInfo :
        IEquatable<ServiceEndpointInfo>
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string Group { get; set; }
        public string Endpoint { get; set; }

        public bool Equals(ServiceEndpointInfo other)
        {
            if (other == null) return false;
            if (string.Compare(this.Endpoint, other.Endpoint, true) != 0) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ServiceEndpointInfo);
        }

        public override int GetHashCode()
        {
            return Endpoint?.GetHashCode() ?? 0;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Endpoint);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Endpoint = reader.ReadString();
        }
    }
}