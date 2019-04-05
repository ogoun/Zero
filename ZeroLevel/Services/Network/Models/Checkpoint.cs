using System;
using System.Runtime.Serialization;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    [DataContract]
    public class Checkpoint :
        ICloneable,
        IEquatable<Checkpoint>,
        IBinarySerializable
    {
        public Guid Id { get; set; }
        public string SourceAppKey { get; set; }
        public string DestinationAppKey { get; set; }
        public string ReasonPhrase { get; set; }
        public long Timestamp { get; set; }
        public CheckpointType CheckpointType { get; set; }
        public byte[] Payload { get; set; }

        #region IBinarySerializable

        public void Deserialize(IBinaryReader reader)
        {
            this.Id = reader.ReadGuid();
            this.Timestamp = reader.ReadLong();
            this.SourceAppKey = reader.ReadString();
            this.DestinationAppKey = reader.ReadString();
            this.ReasonPhrase = reader.ReadString();
            this.CheckpointType = (CheckpointType)reader.ReadInt32();
            this.Payload = reader.ReadBytes();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteGuid(this.Id);
            writer.WriteLong(this.Timestamp);
            writer.WriteString(this.SourceAppKey);
            writer.WriteString(this.DestinationAppKey);
            writer.WriteString(this.ReasonPhrase);
            writer.WriteInt32((int)this.CheckpointType);
            writer.WriteBytes(this.Payload);
        }

        #endregion IBinarySerializable

        #region Ctors

        public Checkpoint()
        {
            this.Id = Guid.NewGuid();
            this.Timestamp = DateTime.Now.Ticks;
        }

        public Checkpoint(Guid id)
        {
            this.Timestamp = DateTime.Now.Ticks;
            this.Id = id;
        }

        public Checkpoint(Checkpoint other)
        {
            this.Id = other.Id;
            this.Timestamp = other.Timestamp;
            this.SourceAppKey = other.SourceAppKey;
            this.DestinationAppKey = other.DestinationAppKey;
            this.CheckpointType = other.CheckpointType;
            this.Payload = other.Payload;
            this.ReasonPhrase = other.ReasonPhrase;
        }

        #endregion Ctors

        #region Equals & Hash

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Checkpoint);
        }

        #endregion Equals & Hash

        #region ICloneable

        public object Clone()
        {
            return new Checkpoint(this);
        }

        #endregion ICloneable

        #region IEquatable

        public bool Equals(Checkpoint other)
        {
            if (this.Id != other.Id) return false;
            if (this.Timestamp != other.Timestamp) return false;
            if (this.CheckpointType != other.CheckpointType) return false;
            if (string.Compare(this.SourceAppKey, other.SourceAppKey, StringComparison.OrdinalIgnoreCase) != 0) return false;
            if (string.Compare(this.DestinationAppKey, other.DestinationAppKey, StringComparison.OrdinalIgnoreCase) != 0) return false;
            if (string.Compare(this.ReasonPhrase, other.ReasonPhrase, StringComparison.OrdinalIgnoreCase) != 0) return false;
            if (false == ArrayExtensions.Equals(this.Payload, other.Payload)) return false;
            return true;
        }

        #endregion IEquatable
    }
}