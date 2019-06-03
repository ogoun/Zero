using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network.FileTransfer.Model
{
    public sealed class FileStartFrame
        : IBinarySerializable, IFileTransferInfo
    {
        private static long _uploadTaskIdCounter = 0;

        public FileTransferInfoType TransferInfoType => FileTransferInfoType.Start;

        public long UploadFileTaskId { get; set; }
        public string FilePath;
        public long Size;

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this.UploadFileTaskId);
            writer.WriteString(this.FilePath);
            writer.WriteLong(this.Size);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.UploadFileTaskId = reader.ReadLong();
            this.FilePath = reader.ReadString();
            this.Size = reader.ReadLong();
        }

        public static FileStartFrame GetTransferFileInfo(string path)
        {
            var fi = new System.IO.FileInfo(path);
            return new FileStartFrame
            {
                FilePath = fi.FullName,
                UploadFileTaskId = Interlocked.Increment(ref _uploadTaskIdCounter),
                Size = fi.Length
            };
        }
    }
}
