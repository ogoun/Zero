using ZeroLevel.Network;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Services.Network.FileTransfer
{
    public static class FileServerFactory
    {
        public static IFileServer Create(int port, string baseFolder, ServerFolderNameMapperDelegate nameMapper = null)
        {
            return CreateFileServer(ExchangeTransportFactory.GetServer(port), baseFolder, nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), true);
        }

        public static IFileServer Create(IExService service, string baseFolder, ServerFolderNameMapperDelegate nameMapper = null)
        {
            return CreateFileServer(service, baseFolder, nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), false);
        }

        private static IFileServer CreateFileServer(IExService service, string baseFolder, ServerFolderNameMapperDelegate nameMapper, bool disposeService)
        {
            return new FileServer(service, baseFolder, nameMapper, disposeService);
        }
    }
}
