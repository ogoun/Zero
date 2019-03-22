using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public class Link : 
        ContentElement
    {
        public Link() : 
            base(ContentElementType.Link)
        {
        }

        public Link(string href, string value) : 
            base(ContentElementType.Link)
        {
            this.Href = href;
            this.Value = value;
        }

        public Link(IBinaryReader reader) : 
            base(ContentElementType.Link)
        {
            Deserialize(reader);
        }

        public string Value { get; set; }
        public string Href { get; set; }

        public override void Deserialize(IBinaryReader reader)
        {
            Value = reader.ReadString();
            Href = reader.ReadString();
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(Value);
            writer.WriteString(Href);
        }
    }
}
