using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public interface IContentElement : 
        IBinarySerializable
    {
        ContentElementType Type { get; }
    }
}
