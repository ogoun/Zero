using DOM.Services;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    /// <summary>
    /// Paragraph
    /// </summary>
    public class Paragraph : 
        ContentElement
    {
        public List<IContentElement> Parts;

        public Paragraph() : base(ContentElementType.Paragraph)
        {
            Initialize();
        }

        public Paragraph(IBinaryReader reader) : 
            base(ContentElementType.Paragraph)
        {
            Deserialize(reader);
        }

        private void Initialize()
        {
            Parts = new List<IContentElement>();
        }

        public Paragraph Append(ContentElement part)
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
        #endregion
    }
}
