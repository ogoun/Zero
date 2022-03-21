using ZeroLevel.Services.Serialization;

namespace ZeroLevel.NN.Models
{
    public class FaceEmbedding
        : IBinarySerializable
    {
        public Face Face;
        public float[] Vector;
        public string Tag;

        public void Deserialize(IBinaryReader reader)
        {
            this.Face = reader.Read<Face>();
            this.Vector = reader.ReadFloatArray();
            this.Tag = reader.ReadString();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.Write(this.Face);
            writer.WriteArray(this.Vector);
            writer.WriteString(this.Tag);
        }
    }
}
