using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.DocumentObjectModel
{
    public class Header :
        IBinarySerializable,
        IEquatable<Header>,
        ICloneable
    {
        #region Ctors
        public Header() { }
        public Header(string name) { this.Name = name; }
        public Header(string name, string value) { this.Name = name; this.Value = value; }
        public Header(string name, string value, string type) { this.Name = name; this.Value = value; this.Type = type; }
        public Header(string name, string value, string type, string tag) { this.Name = name; this.Value = value; this.Type = type; this.Tag = tag; }
        public Header(Header other)
        {
            this.Name = other.Name;
            this.Tag = other.Tag;
            this.Type = other.Type;
            this.Value = other.Value;
        }
        #endregion

        #region Fields
        public string Name;
        public string Value;
        public string Type;
        public string Tag;
        #endregion

        #region IBinarySerializable
        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteString(Value);
            writer.WriteString(Type);
            writer.WriteString(Tag);
        }

        public void Deserialize(IBinaryReader reader)
        {
            Name = reader.ReadString();
            Value = reader.ReadString();
            Type = reader.ReadString();
            Tag = reader.ReadString();
        }
        #endregion

        #region IEquatable
        public bool Equals(Header other)
        {
            if (other == null) return false;
            if (string.Compare(this.Name, other.Name, StringComparison.Ordinal) != 0) return false;
            if (string.Compare(this.Value, other.Value, StringComparison.Ordinal) != 0) return false;
            if (string.Compare(this.Type, other.Type, StringComparison.Ordinal) != 0) return false;
            if (string.Compare(this.Tag, other.Tag, StringComparison.Ordinal) != 0) return false;
            return true;
        }
        #endregion

        #region Equals & Hash
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Header);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^
                Value.GetHashCode() ^
                Type.GetHashCode() ^
                Tag.GetHashCode();
        }
        #endregion

        #region ICloneable
        public object Clone()
        {
            return new Header(this);
        }
        #endregion
    }
}
