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

        public AttachContent(string identity, string caption, string description)
        { Identity = identity; Summary = description; Caption = caption; }

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