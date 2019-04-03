using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public class Agency : IBinarySerializable
    {
        /// <summary>
        /// Agency name
        /// </summary>
        public string Title;

        /// <summary>
        /// Agency website
        /// </summary>
        public string Url;

        /// <summary>
        /// Description
        /// </summary>
        public string Description;

        public Agency()
        {
        }

        public Agency(IBinaryReader reader)
        {
            Deserialize(reader);
        }

        public Agency(string title)
        {
            this.Title = title;
        }

        public Agency(string title, string url)
        {
            this.Title = title; this.Url = url;
        }

        public Agency(string title, string url, string description)
        {
            this.Title = title; this.Url = url; this.Description = description;
        }

        #region IBinarySerializable

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Title);
            writer.WriteString(this.Url);
            writer.WriteString(this.Description);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Title = reader.ReadString();
            this.Url = reader.ReadString();
            this.Description = reader.ReadString();
        }

        #endregion IBinarySerializable
    }
}