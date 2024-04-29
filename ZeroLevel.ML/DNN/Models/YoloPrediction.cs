using ZeroLevel.Services.Serialization;

namespace ZeroLevel.ML.DNN.Models
{
    public class YoloPrediction
        : IBinarySerializable
    {
        public int Class { get; set; }
        public float Cx { get; set; }
        public float Cy { get; set; }
        public float W { get; set; }
        public float H { get; set; }
        public float Score { get; set; }
        public string Label { get; set; } = string.Empty;

        public float X { get { return Cx - W / 2.0f; } }
        public float Y { get { return Cy - W / 2.0f; } }

        public float Area { get { return W * H; } }

        public string Description
        {
            get
            {
                return $"{Label} ({(int)(Score * 100)} %)";
            }
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Cx;
                    case 1: return Cy;
                    case 2: return Cx + W;
                    case 3: return Cy + H;
                }
                return 0;
            }
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(Class);
            writer.WriteFloat(Cx);
            writer.WriteFloat(Cy);
            writer.WriteFloat(W);
            writer.WriteFloat(H);
            writer.WriteFloat(Score);
            writer.WriteString(Label);
        }

        public void Deserialize(IBinaryReader reader)
        {
            Class = reader.ReadInt32();
            Cx = reader.ReadFloat();
            Cy = reader.ReadFloat();
            W = reader.ReadFloat();
            H = reader.ReadFloat();
            Score = reader.ReadFloat();
            Label = reader.ReadString();
        }
    }
}
