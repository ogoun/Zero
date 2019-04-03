using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public class TextStyle : IBinarySerializable
    {
        public TextFormatting Formatting = TextFormatting.None;
        public TextSize Size = TextSize.Normal;
        public string HexColor = "#000000";
        public string HexMarkerColor = "#ffaaff";

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32((Int32)Formatting);
            writer.WriteInt32((Int32)Size);
            writer.WriteString(HexColor);
            writer.WriteString(HexMarkerColor);
        }

        public void Deserialize(IBinaryReader reader)
        {
            Formatting = (TextFormatting)reader.ReadInt32();
            Size = (TextSize)reader.ReadInt32();
            HexColor = reader.ReadString();
            HexMarkerColor = reader.ReadString();
        }
    }
}