using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network.FileTransfer.Model
{
    public sealed class FileStartFrame
        : IBinarySerializable
    {
        private static int _uploadTaskIdCounter = 0;

        public int FileUploadTaskId;
        public string FilePath;
        public long Size;

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this.FileUploadTaskId);
            writer.WriteString(this.FilePath);
            writer.WriteLong(this.Size);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.FileUploadTaskId = reader.ReadInt32();
            this.FilePath = reader.ReadString();
            this.Size = reader.ReadLong();
        }

        public static FileStartFrame GetTransferFileInfo(string path)
        {
            var fi = new System.IO.FileInfo(path);
            return new FileStartFrame
            {
                FilePath = fi.FullName,
                FileUploadTaskId = Interlocked.Increment(ref _uploadTaskIdCounter),
                Size = fi.Length
            };
        }
    }
}
