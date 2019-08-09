using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network.SDL
{
    public class InboxServiceDescription
        : IBinarySerializable
    {
        public int Port { get; set; }
        /// <summary>
        ///  Inbox name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Invoke targer type name
        /// </summary>
        public string Target { get; set; }
        /// <summary>
        /// Inbox kind (handler or requestor)
        /// </summary>
        public InboxKind InboxKind { get; set; }
        /// <summary>
        /// Inbox Incoming data type
        /// </summary>
        public InboxType IncomingType { get; set; }
        /// <summary>
        /// Inbox Outcoming data type
        /// </summary>
        public InboxType OutcomingType { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Port = reader.ReadInt32();
            this.Name = reader.ReadString();
            this.Target = reader.ReadString();
            this.InboxKind = (InboxKind)reader.ReadInt32();
            this.IncomingType = reader.Read<InboxType>();
            this.OutcomingType = reader.Read<InboxType>();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this.Port);
            writer.WriteString(this.Name);
            writer.WriteString(this.Target);
            writer.WriteInt32((int)this.InboxKind);
            writer.Write<InboxType>(this.IncomingType);
            writer.Write<InboxType>(this.OutcomingType);
        }
    }
}
