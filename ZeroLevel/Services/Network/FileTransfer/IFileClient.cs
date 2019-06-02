using System;

namespace ZeroLevel.Services.Network.FileTransfer
{
    public interface IFileClient
        : IDisposable
    {
        void Send(string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null);
    }
}
