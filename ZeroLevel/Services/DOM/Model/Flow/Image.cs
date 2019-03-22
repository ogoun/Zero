using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public sealed class Image : 
        ContentElement
    {
        public SourceType Source { get; set; }
        public FlowAlign Align = FlowAlign.None;
        /// <summary>
        /// Название
        /// </summary>
        public string Title;
        /// <summary>
        /// Ссылка или идентификатор вложения
        /// </summary>
        public string Identifier;


        public Image() : base(ContentElementType.Image)
        {
        }

        public Image(IBinaryReader reader) : base(ContentElementType.Image)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Source = (SourceType)reader.ReadInt32();
            Align = (FlowAlign)reader.ReadInt32();
            Title = reader.ReadString();
            Identifier = reader.ReadString();
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32((Int32)Source);
            writer.WriteInt32((Int32)Align);
            writer.WriteString(Title);
            writer.WriteString(Identifier);
        }
    }
}
