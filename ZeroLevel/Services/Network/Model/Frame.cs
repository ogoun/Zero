using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ZeroLevel.Services.Pools;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    /*
    [Serializable]
    [DataContract]
    public sealed class Frame :
        IEquatable<Frame>,
        IBinarySerializable
    {
        private static ObjectPool<Frame> _pool = new ObjectPool<Frame>(() => new Frame(), 256);

        public static Frame FromPool()
        {
            var frame = _pool.Allocate();
            frame.Inbox = null;
            frame.Payload = null;
            return frame;
        }

        public static Frame FromPool(string inbox)
        {
            var frame = _pool.Allocate();
            frame.Inbox = inbox;
            frame.Payload = null;
            return frame;
        }

        public static Frame FromPool(string inbox, byte[] payload)
        {
            var frame = _pool.Allocate();
            frame.Inbox = inbox;
            frame.Payload = payload;
            return frame;
        }

        public void Release()
        {
            _pool.Free(this);
        }

        [DataMember]
        public string Inbox { get; set; }

        [DataMember]
        public byte[] Payload { get; set; }

        public Frame()
        {
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Inbox = reader.ReadString();
            this.Payload = reader.ReadBytes();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Inbox);
            writer.WriteBytes(this.Payload);
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

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Frame);
        }

        public bool Equals(Frame other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other))
                return true;
            if (string.Compare(this.Inbox, other.Inbox, true) != 0) return false;
            if (ArrayExtensions.UnsafeEquals(this.Payload, other.Payload) == false) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    */

    public struct Frame :
        IBinarySerializable
    {
        public string Inbox { get; set; }

        public byte[] Payload { get; set; }

        public void Deserialize(IBinaryReader reader)
        {
            this.Inbox = reader.ReadString();
            this.Payload = reader.ReadBytes();
        }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteString(this.Inbox);
            writer.WriteBytes(this.Payload);
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

        public bool Equals(Frame other)
        {
            if (string.Compare(this.Inbox, other.Inbox, true) != 0) return false;
            if (ArrayExtensions.UnsafeEquals(this.Payload, other.Payload) == false) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class FrameFactory
    {
        public static Frame Create()
        {
            return new Frame
            {
                Inbox = null,
                Payload = null
            };
        }

        public static Frame Create(string inbox)
        {
            return new Frame
            {
                Inbox = inbox,
                Payload = null
            };
        }

        public static Frame Create(string inbox, byte[] payload)
        {
            return new Frame
            {
                Inbox = inbox,
                Payload = payload
            };
        }
    }

}
