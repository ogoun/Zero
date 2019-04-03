using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public sealed class Video :
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

        public Video() : base(ContentElementType.Video)
        {
        }

        public Video(IBinaryReader reader) :
            base(ContentElementType.Video)
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