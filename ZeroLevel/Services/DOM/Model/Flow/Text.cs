using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public class Text : 
        ContentElement
    {
        public string Value { get; set; }
        public TextStyle Style = new TextStyle();

        public Text() : base(ContentElementType.Text)
        {
        }
        public Text(string value) : 
            base(ContentElementType.Text)
        {
            this.Value = value;
        }
        public Text(IBinaryReader reader) : 
            base(ContentElementType.Text)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Value = reader.ReadString();
            Style.Deserialize(reader);
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(Value);
            Style.Serialize(writer);
        }
    }
}
