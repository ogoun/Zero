using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class Tag :
        IBinarySerializable
    {
        #region Fields

        public string Name;
        public string Value;

        #endregion Fields

        #region IBinarySerializable

        public void Deserialize(IBinaryReader reader)
        {
            this.Name = reader.ReadString();
            this.Value = reader.ReadString();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Name);
            writer.WriteString(this.Value);
        }

        #endregion IBinarySerializable
    }
}