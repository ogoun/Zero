using ZeroLevel.Network;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.Network.FileTransfer
{
    public static class FileServerFactory
    {
        public static IFileServer Create(int port, string baseFolder, ServerFolderNameMapperDelegate nameMapper = null)
        {
            return null;// CreateFileServer(ExchangeTransportFactory.GetServer(port), baseFolder, nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), true);
        }

        public static IFileServer Create(IZeroService service, string baseFolder, ServerFolderNameMapperDelegate nameMapper = null)
        {
            return null;// CreateFileServer(service, baseFolder, nameMapper ?? (c => FSUtils.FileNameCorrection($"{c.Endpoint.Address}_{c.Endpoint.Port}")), false);
        }

        private static IFileServer CreateFileServer(IZeroService service, string baseFolder, ServerFolderNameMapperDelegate nameMapper, bool disposeService)
        {
            return null;// new FileServer(service, baseFolder, nameMapper, disposeService);
        }
    }
}
