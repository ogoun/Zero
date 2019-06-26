using System;
using ZeroLevel.Network;

namespace ZeroLevel.Network.FileTransfer
{
    public interface IFileServer
        : IDisposable
    {
        void Send(ISocketClient client, string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null);
    }
}
