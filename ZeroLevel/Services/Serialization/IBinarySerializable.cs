using System.Threading.Tasks;

namespace ZeroLevel.Services.Serialization
{
    public interface IBinarySerializable
    {
        void Serialize(IBinaryWriter writer);

        void Deserialize(IBinaryReader reader);
    }

    public interface IAsyncBinarySerializable
    {
        Task SerializeAsync(IBinaryWriter writer);

        Task DeserializeAsync(IBinaryReader reader);
    }
}