using System;
using ZeroLevel.Network;

namespace ZeroLevel.Services.Network.FileTransfer
{
    public interface IFileServer
        : IDisposable
    {
        void Send(IZBackward client, string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null);
    }
}
