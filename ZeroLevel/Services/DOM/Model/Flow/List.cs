using DOM.Services;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public class List :
        ContentElement
    {
        public List<IContentElement> Items = new List<IContentElement>();

        public List() :
            base(ContentElementType.List)
        {
        }

        public List(IBinaryReader reader) :
            base(ContentElementType.List)
        {
            Deserialize(reader);
        }

        public override void Serialize(IBinaryWriter writer)
        {
            ContentElementSerializer.WriteCollection(writer, this.Items);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            this.Items = ContentElementSerializer.ReadCollection(reader);
        }
    }
}