using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.UnitTests.Models
{
    public class TestSerializableDTO
        : IBinarySerializable, IEquatable<TestSerializableDTO>
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public long Timestamp { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Id = reader.ReadLong();
            this.Title = reader.ReadString();
            this.Timestamp = reader.ReadLong();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TestSerializableDTO);
        }

        public bool Equals(TestSerializableDTO other)
        {
            if (other == null) return false;
            if (this.Id != other.Id) return false;
            if (this.Timestamp != other.Timestamp) return false;
            if (string.Compare(this.Title, other.Title, false) != 0) return false;
            return true;
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this.Id);
            writer.WriteString(this.Title);
            writer.WriteLong(this.Timestamp);
        }
    }
}
