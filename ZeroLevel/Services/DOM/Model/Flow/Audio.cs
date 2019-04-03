using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public sealed class Audio :
        ContentElement
    {
        public SourceType Source { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title;

        /// <summary>
        /// Link or Attachment ID
        /// </summary>
        public string Identifier;

        public Audio() : base(ContentElementType.Audio)
        {
        }

        public Audio(IBinaryReader reader) :
            base(ContentElementType.Audio)
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