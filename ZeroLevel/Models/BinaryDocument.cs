using System;
using System.Collections.Generic;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Models
{
    /// <summary>
    /// Документ в бинарном представлении
    /// </summary>
    public class BinaryDocument :
        IBinarySerializable,
        IEquatable<BinaryDocument>,
        ICloneable
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Тип содержимого (pdf, doc и т.п.)
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// Содержимое
        /// </summary>
        public byte[] Document { get; set; }
        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// Опциональные заголовки
        /// </summary>
        public List<Header> Headers { get; set; }
        /// <summary>
        /// Категории
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
        #endregion

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
        #endregion

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
        #endregion

        public object Clone()
        {
            return new BinaryDocument(this);
        }
    }
}
