using DOM.Services;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public class Row : 
        ContentElement
    {
        public List<IContentElement> Cells = new List<IContentElement>();

        public Row() : base(ContentElementType.Row)
        {
        }

        public Row(IBinaryReader reader) : 
            base(ContentElementType.Row)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            this.Cells = ContentElementSerializer.ReadCollection(reader);
        }

        public override void Serialize(IBinaryWriter writer)
        {
            ContentElementSerializer.WriteCollection(writer, Cells);
        }
    }
}
