using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Network.FileTransfer.Model
{
    public sealed class FileEndFrame
        : IBinarySerializable
    {
        public int FileUploadTaskId;

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteInt32(this.FileUploadTaskId);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.FileUploadTaskId = reader.ReadInt32();
        }
    }
}
