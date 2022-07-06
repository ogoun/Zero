using ZeroLevel.Services.Serialization;

namespace ZeroLevel.NN.Models
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
        public string Label { get; set; }

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

        public void Deserialize(IBinaryReader reader)
        {
            this.Cx = reader.ReadFloat();
            this.Cy = reader.ReadFloat();
            this.W = reader.ReadFloat();
            this.H = reader.ReadFloat();
            this.Class = reader.ReadInt32();
            this.Score = reader.ReadFloat();
            this.Label = reader.ReadString();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteFloat(this.Cx);
            writer.WriteFloat(this.Cy);
            writer.WriteFloat(this.W);
            writer.WriteFloat(this.H);
            writer.WriteInt32(this.Class);
            writer.WriteFloat(this.Score);
            writer.WriteString(this.Label);
        }
    }
}
