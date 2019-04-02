using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    /// <summary>
    /// Attached document or content by external link
    /// </summary>
    public sealed class FormContent : ContentElement
    {
        public SourceType Source { get; set; }
        public string Title;
        public string Identifier;

        public FormContent() : base(ContentElementType.Form)
        {
        }

        public FormContent(IBinaryReader reader) : base(ContentElementType.Form)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Source = (SourceType)reader.ReadInt32();
            Title = reader.ReadString();
            Identifier = reader.ReadString();
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32((Int32)Source);
            writer.WriteString(Title);
            writer.WriteString(Identifier);
        }
    }
}
