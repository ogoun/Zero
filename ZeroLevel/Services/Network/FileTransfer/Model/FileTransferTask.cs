using System;

namespace ZeroLevel.Network.FileTransfer
{
    internal class FileTransferTask
    {
        public string FilePath;
        public Action<string> CompletedHandler;
        public Action<string, string> ErrorHandler;
        public IClient Client;
    }
}
