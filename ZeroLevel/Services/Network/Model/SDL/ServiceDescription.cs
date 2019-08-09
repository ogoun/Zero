using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network.SDL
{
    public class ServiceDescription
        : IBinarySerializable
    {
        public ZeroServiceInfo ServiceInfo { get; set; }
        public IEnumerable<InboxServiceDescription> Inboxes { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.ServiceInfo = reader.Read<ZeroServiceInfo>();
            this.Inboxes = reader.ReadCollection<InboxServiceDescription>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.Write<ZeroServiceInfo>(this.ServiceInfo);
            writer.WriteCollection<InboxServiceDescription>(this.Inboxes);
        }
    }
}
