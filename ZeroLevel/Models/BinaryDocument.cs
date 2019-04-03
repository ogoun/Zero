using System;
using System.Collections.Generic;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Models
{
    /// <summary>
    /// Binary data represantation
    /// </summary>
    public class BinaryDocument :
        IBinarySerializable,
        IEquatable<BinaryDocument>,
        ICloneable
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Content type (pdf, doc, etc.)
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Content
        /// </summary>
        public byte[] Document { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Optional headers
        /// </summary>
        public List<Header> Headers { get; set; }

        /// <summary>
        /// Categories
        /// </summary>
        public List<Category> Categories { get; set; }

        #region Ctors

        public BinaryDocument()
        {
            Created = DateTime.Now;
            Headers = new List<Header>();
            Categories = new List<Category>();
        }

        public BinaryDocument(BinaryDocument other)
        {
            var data = MessageSerializer.Serialize(other);
            using (var reader = new MemoryStreamReader(data))
            {
                Deserialize(reader);
            }
        }

        #endregion Ctors

        #region IBinarySerializable

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteGuid(this.Id);
            writer.WriteString(this.FileName);
            writer.WriteString(this.ContentType);
            writer.WriteBytes(this.Document);
            writer.WriteDateTime(this.Created);
            writer.WriteCollection(this.Headers);
            writer.WriteCollection(this.Categories);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Id = reader.ReadGuid();
            this.FileName = reader.ReadString();
            this.ContentType = reader.ReadString();
            this.Document = reader.ReadBytes();
            this.Created = reader.ReadDateTime() ?? DateTime.Now;
            this.Headers = reader.ReadCollection<Header>();
            this.Categories = reader.ReadCollection<Category>();
        }

        #endregion IBinarySerializable

        #region Equals & Hash

        public override bool Equals(object obj)
        {
            return this.Equals(obj as BinaryDocument);
        }

        public bool Equals(BinaryDocument other)
        {
            if (this == null)
                throw new NullReferenceException();
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (this.GetType() != other.GetType()) return false;
            if (this.Id != other.Id) return false;
            if (DateTime.Compare(this.Created, other.Created) != 0) return false;
            if (ArrayExtensions.UnsafeEquals(this.Document, other.Document) == false) return false;
            if (string.Compare(this.ContentType, other.ContentType) != 0) return false;
            if (string.Compare(this.FileName, other.FileName) != 0) return false;
            if (this.Headers.NoOrderingEquals(other.Headers) == false) return false;
            if (this.Categories.NoOrderingEquals(other.Categories) == false) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion Equals & Hash

        public object Clone()
        {
            return new BinaryDocument(this);
        }
    }
}