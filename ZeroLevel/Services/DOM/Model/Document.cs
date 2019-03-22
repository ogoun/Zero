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
            Aside = new List<AsideContent>();
            Assotiations = new List<Assotiation>();
            Categories = new List<Category>();
        }

        /// <summary>
        /// Идентификатор документа
        /// </summary>
        public Guid Id;
        /// <summary>
        /// Краткое описание, лид
        /// </summary>
        public string Summary;
        /// <summary>
        /// Заголовок
        /// </summary>
        public string Header;
        /// <summary>
        /// Дополнительные идентификаторы
        /// </summary>
        public Identifier Identifier;
        /// <summary>
        /// Содержимое документа
        /// </summary>
        public FlowContent Content;
        /// <summary>
        /// Теги
        /// </summary>
        public TagMetadata TagMetadata;
        /// <summary>
        /// Метаданные документа
        /// </summary>
        public DescriptiveMetadata DescriptiveMetadata;
        /// <summary>
        /// Вложенные документы
        /// </summary>
        public List<AsideContent> Aside;
        /// <summary>
        /// Связанные документы
        /// </summary>
        public List<Assotiation> Assotiations;
        /// <summary>
        /// Категории
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

            writer.WriteCollection<AsideContent>(this.Aside);
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

            this.Aside = reader.ReadCollection<AsideContent>();
            this.Assotiations = reader.ReadCollection<Assotiation>();
            this.Categories = reader.ReadCollection<Category>();
        }
        #endregion
    }
}
