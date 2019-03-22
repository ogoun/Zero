using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class TagMetadata 
        : IBinarySerializable
    {
        public TagMetadata() { Initialize(); }
        public TagMetadata(IBinaryReader reader) { Deserialize(reader); }

        private void Initialize()
        {
            Places = new List<Tag>();
            Companies = new List<Tag>();
            Persons = new List<Tag>();
            Keywords = new List<string>();
        }

        #region Fields
        /// <summary>
        /// Упоминаемые места
        /// </summary>
        public List<Tag> Places;
        /// <summary>
        /// Упоминаемые компании
        /// </summary>
        public List<Tag> Companies;
        /// <summary>
        /// Упоминаемые персоны
        /// </summary>
        public List<Tag> Persons;
        /// <summary>
        /// Ключевые слова
        /// </summary>
        public List<string> Keywords;
        #endregion

        #region IBinarySerializable
        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteCollection<Tag>(this.Companies);
            writer.WriteCollection(this.Keywords);
            writer.WriteCollection<Tag>(this.Places);
            writer.WriteCollection<Tag>(this.Persons);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Companies = reader.ReadCollection<Tag>();
            this.Keywords = reader.ReadStringCollection();
            this.Places = reader.ReadCollection<Tag>();
            this.Persons = reader.ReadCollection<Tag>();
        }
        #endregion
    }
}
