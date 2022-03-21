using ZeroLevel.Services.Serialization;

namespace ZeroLevel.NN.Models
{
    public class FacePoint
        : IBinarySerializable
    {
        public float X { get; set; }
        public float Y { get; set; }

        public FacePoint() { }
        public FacePoint(float x, float y) { X = x; Y = y; }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteFloat(this.X);
            writer.WriteFloat(this.Y);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.X = reader.ReadFloat();
            this.Y = reader.ReadFloat();
        }
    }
}
