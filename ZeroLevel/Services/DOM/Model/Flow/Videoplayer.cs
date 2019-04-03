using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public sealed class Videoplayer :
        ContentElement
    {
        public Text Title;
        public List<Video> Playlist = new List<Video>();

        public Videoplayer() :
            base(ContentElementType.Videoplayer)
        {
        }

        public Videoplayer(IBinaryReader reader) :
            base(ContentElementType.Videoplayer)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Title = reader.Read<Text>();
            this.Playlist = reader.ReadCollection<Video>();
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.Write(Title);
            writer.WriteCollection<Video>(this.Playlist);
        }
    }
}