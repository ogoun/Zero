using ZeroLevel.Network;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Services.Network.FileTransfer
{
    public static class FileClientFactory
    {
        public static IFileClient Create(string serverEndpoint, string baseFolder, ClientFolderNameMapper nameMapper = null)
        {
            return CreateFileServerClient(ExchangeTransportFactory.GetClient(serverEndpoint), baseFolder,
                nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), true);
        }

        public static IFileClient Create(IExClient client, string baseFolder, ClientFolderNameMapper nameMapper = null)
        {
            return CreateFileServerClient(client, baseFolder, nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), false);
        }

        private static IFileClient CreateFileServerClient(IExClient client, string baseFolder, ClientFolderNameMapper nameMapper, bool disposeClient)
        {
            return new FileClient(client, baseFolder, nameMapper, disposeClient);
        }
    }
}
