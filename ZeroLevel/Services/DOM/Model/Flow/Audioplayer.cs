using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public sealed class Audioplayer :
        ContentElement
    {
        public Text Title;
        public List<Audio> Tracks = new List<Audio>();

        public Audioplayer() :
            base(ContentElementType.Audioplayer)
        {
        }

        public Audioplayer(IBinaryReader reader) :
            base(ContentElementType.Audioplayer)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Title = reader.Read<Text>();
            this.Tracks = reader.ReadCollection<Audio>();
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.Write(Title);
            writer.WriteCollection<Audio>(this.Tracks);
        }
    }
}