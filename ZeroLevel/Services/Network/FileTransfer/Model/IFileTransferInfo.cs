namespace ZeroLevel.Services.Network.FileTransfer.Model
{
    public enum FileTransferInfoType
    {
        Start,
        Frame,
        End
    }

    public interface IFileTransferInfo
    {
        long UploadFileTaskId { get; }
        FileTransferInfoType TransferInfoType { get; }
    }
}
