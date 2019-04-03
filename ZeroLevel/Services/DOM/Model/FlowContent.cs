using System.Collections.Generic;
using ZeroLevel.DocumentObjectModel.Flow;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public class FlowContent :
        ContentElement
    {
        public List<Section> Sections = new List<Section>();

        public FlowContent() :
            base(ContentElementType.Content)
        { }

        public FlowContent(IBinaryReader reader) :
            base(ContentElementType.Content)
        {
            Deserialize(reader);
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteCollection<Section>(this.Sections);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            this.Sections = reader.ReadCollection<Section>();
        }
    }
}