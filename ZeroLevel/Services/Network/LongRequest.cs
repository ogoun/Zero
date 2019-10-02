using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network
{
    public class LongRequest<T>
        : IBinarySerializable
    {
        static long _index = 0;

        public LongRequest() { }

        public LongRequest<T> Create(T value, string serviceKey, string inbox) => new LongRequest<T>
        {
            Identity = Interlocked.Increment(ref _index),
            Body = value,
            ServiceKey = serviceKey,
            Inbox = inbox
        };

        public long Identity { get; set; }
        public T Body { get; set; }
        public string ServiceKey { get; set; }
        public string Inbox { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Identity = reader.ReadLong();
            this.Inbox = reader.ReadString();
            this.ServiceKey = reader.ReadString();
            this.Body = reader.ReadCompatible<T>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this.Identity);
            writer.WriteString(this.Inbox);
            writer.WriteString(this.ServiceKey);
            writer.WriteCompatible<T>(this.Body);
        }
    }
}
