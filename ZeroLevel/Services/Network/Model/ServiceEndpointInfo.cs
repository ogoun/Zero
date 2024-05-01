using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    /// <summary>
    /// Endpoint
    /// </summary>
    public class ServiceEndpointInfo :
        IBinarySerializable, IEquatable<ServiceEndpointInfo>
    {
        public string Endpoint { get; set; }
        public ZeroServiceInfo ServiceInfo { get; set; }

        public bool Equals(ServiceEndpointInfo other)
        {
            if (other == null!) return false;
            if (string.Compare(this.Endpoint, other.Endpoint, true) != 0) return false;
            return this.ServiceInfo?.Equals(other.ServiceInfo) ?? other != null ? false : true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((obj as ServiceEndpointInfo)!);
        }

        public override int GetHashCode()
        {
            return this.ServiceInfo?.GetHashCode() ?? 0 ^ Endpoint?.GetHashCode() ?? 0;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Endpoint);
            writer.Write(this.ServiceInfo);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Endpoint = reader.ReadString();
            this.ServiceInfo = reader.Read<ZeroServiceInfo>();
        }
    }
}