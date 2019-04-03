using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using ZeroLevel.Services.Pools;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network
{
    [Serializable]
    [DataContract]
    public sealed class Frame :
        IEquatable<Frame>,
        IBinarySerializable,
        ICloneable
    {
        private static ObjectPool<Frame> _pool = new ObjectPool<Frame>(() => new Frame(), 256);

        public static Frame FromPool()
        {
            var frame = _pool.Allocate();
            frame.Inbox = null;
            frame.Payload = null;
            frame.IsRequest = false;
            return frame;
        }

        public void Release()
        {
            _pool.Free(this);
        }

        private static long _id_counter = 0;

        internal static long GetMessageId() => Interlocked.Increment(ref _id_counter);

        [DataMember]
        public long FrameId { get; set; }

        [DataMember]
        public string Inbox { get; set; }

        [DataMember]
        public byte[] Payload { get; set; }

        [DataMember]
        public bool IsRequest { get; set; }

        public Frame()
        {
            FrameId = GetMessageId();
        }

        public Frame(Frame other)
        {
            var data = MessageSerializer.Serialize(other);
            using (var reader = new MemoryStreamReader(data))
            {
                Deserialize(reader);
            }
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.FrameId = reader.ReadLong();
            this.Inbox = reader.ReadString();
            this.Payload = reader.ReadBytes();
            this.IsRequest = reader.ReadBoolean();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this.FrameId);
            writer.WriteString(this.Inbox);
            writer.WriteBytes(this.Payload);
            writer.WriteBoolean(this.IsRequest);
        }

        public T Read<T>()
        {
            if (this.Payload == null || this.Payload.Length == 0) return default(T);
            return MessageSerializer.DeserializeCompatible<T>(this.Payload);
        }

        public IEnumerable<T> ReadCollection<T>() where T : IBinarySerializable
        {
            return MessageSerializer.DeserializeCollection<T>(this.Payload);
        }

        public string ReadText()
        {
            if (this.Payload == null || this.Payload.Length == 0) return null;
            return Encoding.UTF32.GetString(this.Payload);
        }

        public void Write<T>(T data) where T : IBinarySerializable
        {
            this.Payload = MessageSerializer.Serialize<T>(data);
        }

        public void Write<T>(IEnumerable<T> items) where T : IBinarySerializable
        {
            this.Payload = MessageSerializer.Serialize<T>(items);
        }

        public void Write(string data)
        {
            this.Payload = Encoding.UTF32.GetBytes(data);
        }

        public override int GetHashCode()
        {
            return this.FrameId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Frame);
        }

        public bool Equals(Frame other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetType() != other.GetType())
                return false;
            if (this.IsRequest != other.IsRequest) return false;
            if (this.FrameId != other.FrameId) return false;
            if (string.Compare(this.Inbox, other.Inbox, true) != 0) return false;
            if (ArrayExtensions.UnsafeEquals(this.Payload, other.Payload) == false) return false;
            return true;
        }

        public object Clone()
        {
            return new Frame(this);
        }
    }
}