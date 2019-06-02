using System;
using ZeroLevel.Network;

namespace ZeroLevel.Services.Network.FileTransfer.Model
{
    internal class FileTransferTask
    {
        public string FilePath;
        public Action<string> CompletedHandler;
        public Action<string, string> ErrorHandler;
        public IZBackward Client;
    }
}
