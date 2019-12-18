namespace ZeroLevel.Services.Serialization
{
    public interface ISerializer
    {
        void Serialize(IBinaryWriter writer);
        object Deserialize(IBinaryReader writer);
    }
}