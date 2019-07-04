using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Network.FileTransfer
{
    public static class FileClientFactory
    {
        public static IFileClient Create(string serverEndpoint, string baseFolder, ClientFolderNameMapper nameMapper = null)
        {
            var client = new ExClient(new SocketClient(NetUtils.CreateIPEndPoint(serverEndpoint), new Router()));
            return CreateFileServerClient(client, baseFolder,
               nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), true);
        }

        public static IFileClient Create(ExClient client, string baseFolder, ClientFolderNameMapper nameMapper = null)
        {
            return CreateFileServerClient(client, baseFolder, nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), false);
        }

        private static IFileClient CreateFileServerClient(ExClient client, string baseFolder, ClientFolderNameMapper nameMapper, bool disposeClient)
        {
            return new FileClient(client, baseFolder, nameMapper, disposeClient);
        }
    }
}
