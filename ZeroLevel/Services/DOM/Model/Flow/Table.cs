using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public class Table :
        ContentElement
    {
        public string Name;
        public string Abstract;
        public List<Column> Columns = new List<Column>();
        public List<Row> Rows = new List<Row>();

        public Table() : base(ContentElementType.Table)
        {
        }

        public Table(IBinaryReader reader)
            : base(ContentElementType.Table)
        {
            Deserialize(reader);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            Name = reader.ReadString();
            Abstract = reader.ReadString();
            Columns = reader.ReadCollection<Column>();
            Rows = reader.ReadCollection<Row>();
        }

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteString(Abstract);
            writer.WriteCollection(Columns);
            writer.WriteCollection(Rows);
        }
    }
}