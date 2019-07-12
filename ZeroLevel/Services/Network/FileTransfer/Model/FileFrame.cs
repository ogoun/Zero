using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network.FileTransfer
{
    public sealed class FileFrame :
        IBinarySerializable, IFileTransferInfo
    {
        public FileTransferInfoType TransferInfoType => FileTransferInfoType.Frame;

        public long UploadFileTaskId { get; set; }
        public long Offset { get; set; }
        public byte[] Payload { get; set; }
        public ulong ChecksumL { get; set; }
        public ulong ChecksumH { get; set; }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this.UploadFileTaskId);
            writer.WriteLong(this.Offset);
            writer.WriteBytes(this.Payload);

            writer.WriteULong(this.ChecksumL);
            writer.WriteULong(this.ChecksumH);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.UploadFileTaskId = reader.ReadLong();
            this.Offset = reader.ReadLong();
            this.Payload = reader.ReadBytes();

            this.ChecksumL = reader.ReadULong();
            this.ChecksumH = reader.ReadULong();
        }
    }
}
