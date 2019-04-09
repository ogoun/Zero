using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class AttachContent
        : IBinarySerializable
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Identity;

        /// <summary>
        /// Title
        /// </summary>
        public string Caption;

        /// <summary>
        /// Description (optional)
        /// </summary>
        public string Summary;

        /// <summary>
        /// Content type
        /// </summary>
        public ContentType ContentType;

        /// <summary>
        /// Binary content
        /// </summary>
        public byte[] Payload;

        public AttachContent()
        {
        }

        public AttachContent(IBinaryReader reader)
        {
            Deserialize(reader);
        }

        public AttachContent(string identity, ContentType contentType)
        { Identity = identity; ContentType = contentType; }

        public AttachContent(string identity, string caption, ContentType contentType)
        { Identity = identity; Caption = caption; ContentType = contentType; }

        public AttachContent(string identity, string caption, string description)
        { Identity = identity; Summary = description; Caption = caption; }

        public AttachContent Write<T>(T value)
        {
            this.Payload = MessageSerializer.SerializeCompatible<T>(value);
            return this;
        }

        public T Read<T>()
        {
            if (this.Payload == null || this.Payload.Length == 0) return default(T);
            return MessageSerializer.DeserializeCompatible<T>(this.Payload);
        }

        #region IBinarySerializable

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Identity);
            writer.WriteString(this.Caption);
            writer.WriteString(this.Summary);
            writer.WriteInt32((Int32)this.ContentType);
            writer.WriteBytes(this.Payload);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Identity = reader.ReadString();
            this.Caption = reader.ReadString();
            this.Summary = reader.ReadString();
            this.ContentType = (ContentType)reader.ReadInt32();
            this.Payload = reader.ReadBytes();
        }

        #endregion IBinarySerializable
    }
}