using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network
{
    public class LongRequest<T>
        : IBinarySerializable
    {
        public LongRequest() { }

        public LongRequest<T> Create(T value, string inbox) => new LongRequest<T>
        {
            Body = value,
            Inbox = inbox
        };

        public T Body { get; set; }
        public string Inbox { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Inbox = reader.ReadString();
            this.Body = reader.ReadCompatible<T>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Inbox);
            writer.WriteCompatible<T>(this.Body);
        }
    }
}
