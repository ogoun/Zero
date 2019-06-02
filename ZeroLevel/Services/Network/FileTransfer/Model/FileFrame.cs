using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network.FileTransfer.Model
{
    public sealed class FileFrame :
        IBinarySerializable
    {
        public int UploadTaskId { get; set; }
        public long Offset { get; set; }
        public byte[] Payload { get; set; }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this.UploadTaskId);
            writer.WriteLong(this.Offset);
            writer.WriteBytes(this.Payload);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.UploadTaskId = reader.ReadInt32();
            this.Offset = reader.ReadLong();
            this.Payload = reader.ReadBytes();
        }
    }
}
