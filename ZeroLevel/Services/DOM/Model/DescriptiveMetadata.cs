using System;
using System.Collections.Generic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class DescriptiveMetadata :
        IBinarySerializable
    {
        public DescriptiveMetadata()
        {
            Initialize();
        }

        public DescriptiveMetadata(IBinaryReader reader)
        {
            Deserialize(reader);
        }

        private void Initialize()
        {
            Created = DateTime.Now;
            Headers = new List<Header>();
            Priority = Priority.Normal;
            Source = new Agency();
            Publisher = new Agency();
            Original = new Tag();
            Language = "ru";
        }

        #region Fields

        /// <summary>
        /// Authors
        /// </summary>
        public string Byline;

        /// <summary>
        /// Copyright
        /// </summary>
        public string CopyrightNotice;

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime Created;

        /// <summary>
        /// Main language
        /// </summary>
        public string Language;

        /// <summary>
        /// Priority
        /// </summary>
        public Priority Priority;

        /// <summary>
        /// Document source
        /// </summary>
        public Agency Source;

        /// <summary>
        /// Document publisher
        /// </summary>
        public Agency Publisher;

        /// <summary>
        /// Reference to original document
        /// </summary>
        public Tag Original;

        /// <summary>
        /// Headers (optional)
        /// </summary>
        public List<Header> Headers;

        #endregion Fields

        #region IBinarySerializable

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Byline);
            writer.WriteString(this.CopyrightNotice);
            writer.WriteDateTime(this.Created);
            writer.WriteCollection<Header>(this.Headers);
            writer.WriteString(this.Language);
            this.Original.Serialize(writer);
            writer.WriteInt32((Int32)this.Priority);
            this.Publisher.Serialize(writer);
            this.Source.Serialize(writer);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Byline = reader.ReadString();
            this.CopyrightNotice = reader.ReadString();
            this.Created = reader.ReadDateTime()!.Value;
            this.Headers = reader.ReadCollection<Header>();
            this.Language = reader.ReadString();
            this.Original = new Tag();
            this.Original.Deserialize(reader);
            this.Priority = (Priority)reader.ReadInt32();
            this.Publisher = new Agency();
            this.Publisher.Deserialize(reader);
            this.Source = new Agency();
            this.Source.Deserialize(reader);
        }

        #endregion IBinarySerializable
    }
}