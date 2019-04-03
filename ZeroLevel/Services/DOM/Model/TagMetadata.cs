using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class TagMetadata
        : IBinarySerializable
    {
        public TagMetadata()
        {
            Initialize();
        }

        public TagMetadata(IBinaryReader reader)
        {
            Deserialize(reader);
        }

        private void Initialize()
        {
            Places = new List<Tag>();
            Companies = new List<Tag>();
            Persons = new List<Tag>();
            Keywords = new List<string>();
        }

        #region Fields

        /// <summary>
        /// Placec (city, country, etc)
        /// </summary>
        public List<Tag> Places;

        /// <summary>
        /// Companies
        /// </summary>
        public List<Tag> Companies;

        /// <summary>
        /// Persons
        /// </summary>
        public List<Tag> Persons;

        /// <summary>Keywords
        /// </summary>
        public List<string> Keywords;

        #endregion Fields

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

        #endregion IBinarySerializable
    }
}