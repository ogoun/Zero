using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public sealed class Category :
        IBinarySerializable,
        IEquatable<Category>,
        ICloneable
    {
        #region Ctors

        public Category()
        {
        }

        public Category(string title, string code, string direction_code, string description = null,
            bool is_system = false)
        {
            this.Title = title;
            this.Description = description;
            this.Code = code;
            this.DirectionCode = direction_code;
            this.IsSystem = is_system;
        }

        public Category(Category other)
        {
            this.Title = other.Title;
            this.Description = other.Description;
            this.Code = other.Code;
            this.DirectionCode = other.DirectionCode;
            this.IsSystem = other.IsSystem;
        }

        public Category(IBinaryReader reader)
        {
            Deserialize(reader);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Название
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Код категории
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Код направления
        /// </summary>
        public string DirectionCode { get; set; }

        /// <summary>
        /// Указывает на принадлежность категории внутренней части системы
        /// Категория не предназначенна для публичного доступа
        /// </summary>
        public bool IsSystem { get; set; }

        #endregion

        #region IBinarySerializable

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Title);
            writer.WriteString(this.Code);
            writer.WriteString(this.Description);
            writer.WriteString(this.DirectionCode);
            writer.WriteBoolean(this.IsSystem);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Title = reader.ReadString();
            this.Code = reader.ReadString();
            this.Description = reader.ReadString();
            this.DirectionCode = reader.ReadString();
            this.IsSystem = reader.ReadBoolean();
        }

        #endregion

        #region ICloneable

        public object Clone()
        {
            return new Category(this);
        }

        #endregion

        #region IEquatable

        public bool Equals(Category other)
        {
            if (other == null) return false;
            if (string.Compare(this.Title, other.Title, StringComparison.Ordinal) != 0) return false;
            if (string.Compare(this.Code, other.Code, StringComparison.Ordinal) != 0) return false;
            if (string.Compare(this.Description, other.Description, StringComparison.Ordinal) != 0) return false;
            if (string.Compare(this.DirectionCode, other.DirectionCode, StringComparison.Ordinal) != 0) return false;
            if (this.IsSystem != other.IsSystem) return false;
            return true;
        }

        #endregion

        #region Equals & Hash

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Category);
        }

        public override int GetHashCode()
        {
            return Title?.GetHashCode() ?? 0 ^
                   Description?.GetHashCode() ?? 0 ^
                   Code?.GetHashCode() ?? 0 ^
                   DirectionCode?.GetHashCode() ?? 0 ^
                   IsSystem.GetHashCode();
        }

        #endregion
    }
}