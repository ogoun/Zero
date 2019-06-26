namespace ZeroLevel.Network.FileTransfer
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
