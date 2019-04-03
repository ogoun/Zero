namespace ZeroLevel.Services.Serialization
{
    public interface IBinarySerializable
    {
        void Serialize(IBinaryWriter writer);

        void Deserialize(IBinaryReader reader);
    }
}