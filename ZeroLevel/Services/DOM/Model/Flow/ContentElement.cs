using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public abstract class ContentElement :
        IContentElement
    {
        protected ContentElementType _type;

        public ContentElementType Type
        {
            get
            {
                return _type;
            }
        }

        protected ContentElement(ContentElementType type)
        {
            _type = type;
        }

        public abstract void Serialize(IBinaryWriter writer);

        public abstract void Deserialize(IBinaryReader reader);
    }
}