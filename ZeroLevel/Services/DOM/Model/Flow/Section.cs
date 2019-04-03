using DOM.Services;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    /// <summary>
    /// Document block
    /// </summary>
    public class Section :
        ContentElement
    {
        public List<IContentElement> Parts;

        public Section() : base(ContentElementType.Section)
        {
            Initialize();
        }

        public Section(IBinaryReader reader) :
            base(ContentElementType.Section)
        {
            Deserialize(reader);
        }

        private void Initialize()
        {
            Parts = new List<IContentElement>();
        }

        public Section Append(ContentElement part)
        {
            Parts.Add(part);
            return this;
        }

        #region Serialization

        public override void Serialize(IBinaryWriter writer)
        {
            ContentElementSerializer.WriteCollection(writer, this.Parts);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            this.Parts = ContentElementSerializer.ReadCollection(reader);
        }

        #endregion Serialization
    }
}