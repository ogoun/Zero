using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    public class ServiceRegisterInfo :
        IBinarySerializable, IEquatable<ServiceRegisterInfo>
    {
        public int Port { get; set; }
        public ZeroServiceInfo ServiceInfo { get; set; }

        public bool Equals(ServiceRegisterInfo other)
        {
            if (other == null!) return false;
            if (this.Port != other.Port) return false;
            return this.ServiceInfo?.Equals(other.ServiceInfo) ?? other != null ? false : true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((obj as ServiceRegisterInfo)!);
        }

        public override int GetHashCode()
        {
            return Port.GetHashCode() ^ this.ServiceInfo.GetHashCode();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this.Port);
            writer.Write(this.ServiceInfo);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Port = reader.ReadInt32();
            this.ServiceInfo = reader.Read<ZeroServiceInfo>();
        }
    }
}
