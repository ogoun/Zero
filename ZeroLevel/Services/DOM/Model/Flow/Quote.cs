using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public sealed class Quote :
        Text
    {
        public Quote()
        {
            _type = ContentElementType.Quote;
        }

        public Quote(string text)
        {
            _type = ContentElementType.Quote;
            Value = text;
        }

        public Quote(IBinaryReader reader)
        {
            Deserialize(reader);
            _type = ContentElementType.Quote;
        }
    }
}