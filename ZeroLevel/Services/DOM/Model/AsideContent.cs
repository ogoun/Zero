using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class AsideContent 
        : IBinarySerializable
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public string Identity;
        /// <summary>
        /// Название содержимого
        /// </summary>
        public string Caption;
        /// <summary>
        /// Описание (опционально)
        /// </summary>
        public string Summary;
        /// <summary>
        /// Тип содержимого
        /// </summary>
        public ContentType ContentType;
        /// <summary>
        /// Содержимое в бинарном представлении
        /// </summary>
        public byte[] Payload;

        public AsideContent() { }
        public AsideContent(IBinaryReader reader) { Deserialize(reader); }
        public AsideContent(string identity, string caption, string description)
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
        #endregion
    }
}
