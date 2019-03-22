using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public sealed class Gallery : ContentElement
    {
        public Text Title;
        public List<Image> Images = new List<Image>();

        public Gallery() : base(ContentElementType.Gallery)
        {
        }

        public Gallery(IBinaryReader reader) : base(ContentElementType.Gallery)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Title = reader.Read<Text>();
            var items_count = reader.ReadInt32();
            for (int i = 0; i < items_count; i++)
            {
                var item = new Image();
                item.Deserialize(reader);
                Images.Add(item);
            }
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.Write(Title);
            writer.WriteInt32(Images.Count);
            foreach (var item in Images)
            {
                item.Serialize(writer);
            }
        }
    }
}
