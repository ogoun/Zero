using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public class Column :
        ContentElement
    {
        public string Caption { get; set; }

        public Column() : base(ContentElementType.Column)
        {
        }

        public Column(string caption) : base(ContentElementType.Column)
        {
            this.Caption = caption;
        }

        public Column(IBinaryReader reader) : base(ContentElementType.Column)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Caption = reader.ReadString();
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(Caption);
        }
    }
}