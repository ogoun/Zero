﻿using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network.FileTransfer
{
    public sealed class FileEndFrame
        : IBinarySerializable, IFileTransferInfo
    {
        public FileTransferInfoType TransferInfoType => FileTransferInfoType.End;
        public long UploadFileTaskId { get; set; }

        public void Serialize(IBinaryWriter writer)
        {
            writer.WriteLong(this.UploadFileTaskId);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.UploadFileTaskId = reader.ReadLong();
        }
    }
}
