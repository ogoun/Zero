using System;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public class Document 
        : IBinarySerializable
    {
        private static readonly Document _empty = new Document();
        public static Document Empty
        {
            get
            {
                var data = MessageSerializer.Serialize(_empty);
                return MessageSerializer.Deserialize<Document>(data);
            }
        }

        public Document() { Id = Guid.NewGuid(); Initialize(); }
        public Document(Guid id) { Id = id; Initialize(); }
        public Document(IBinaryReader reader) { Deserialize(reader); }
        public Document(Document other)
        {
            var data = MessageSerializer.Serialize(other);
            using (var reader = new MemoryStreamReader(data))
            {
                Deserialize(reader);
            }
        }

        private void Initialize()
        {
            Identifier = new Identifier();
            Content = new FlowContent();
            TagMetadata = new TagMetadata();
            DescriptiveMetadata = new DescriptiveMetadata();
            Attachments = new List<AttachContent>();
            Assotiations = new List<Assotiation>();
            Categories = new List<Category>();
        }

        /// <summary>
        /// ID
        /// </summary>
        public Guid Id;
        /// <summary>
        /// Short description
        /// </summary>
        public string Summary;
        /// <summary>
        /// Title
        /// </summary>
        public string Header;
        /// <summary>
        /// Identification block
        /// </summary>
        public Identifier Identifier;
        /// <summary>
        /// Content
        /// </summary>
        public FlowContent Content;
        /// <summary>
        /// Tags
        /// </summary>
        public TagMetadata TagMetadata;
        /// <summary>
        /// Metadata
        /// </summary>
        public DescriptiveMetadata DescriptiveMetadata;
        /// <summary>
        /// Attachments
        /// </summary>
        public List<AttachContent> Attachments;
        /// <summary>
        /// Binded documents
        /// </summary>
        public List<Assotiation> Assotiations;
        /// <summary>
        /// Categories
        /// </summary>
        public List<Category> Categories;


        #region IBinarySerializable
        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteGuid(this.Id);
            writer.WriteString(this.Summary);
            writer.WriteString(this.Header);

            writer.Write(this.Identifier);
            writer.Write(this.Content);
            writer.Write(this.TagMetadata);
            writer.Write(this.DescriptiveMetadata);

            writer.WriteCollection<AttachContent>(this.Attachments);
            writer.WriteCollection<Assotiation>(this.Assotiations);
            writer.WriteCollection<Category>(this.Categories);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Id = reader.ReadGuid();
            this.Summary = reader.ReadString();
            this.Header = reader.ReadString();

            this.Identifier = reader.Read<Identifier>();
            this.Content = reader.Read<FlowContent>();
            this.TagMetadata = reader.Read<TagMetadata>();
            this.DescriptiveMetadata = reader.Read<DescriptiveMetadata>();

            this.Attachments = reader.ReadCollection<AttachContent>();
            this.Assotiations = reader.ReadCollection<Assotiation>();
            this.Categories = reader.ReadCollection<Category>();
        }
        #endregion
    }
}
