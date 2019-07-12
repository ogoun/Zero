using System;
using ZeroLevel.Models;

namespace ZeroLevel.Network.FileTransfer
{
    public sealed class FileReceiver
    {
        private readonly string _baseFolder;
        private readonly ClientFolderNameMapper _nameMapper;
        private readonly FileWriter _receiver;

        public FileReceiver(IRouter router, string baseFolder, ClientFolderNameMapper nameMapper)
        {
            _baseFolder = baseFolder ?? throw new Exception(nameof(baseFolder));
            _nameMapper = nameMapper ?? throw new Exception(nameof(nameMapper));
            _receiver = new FileWriter(baseFolder);

            if (false == router.ContainsRequestorInbox("__file_transfer_start_transfer__"))
            {
                router.RegisterInbox<FileStartFrame, InvokeResult>("__file_transfer_start_transfer__",
                    (c, f) => _receiver.Incoming(f, nameMapper(c)));
            }
            if (false == router.ContainsRequestorInbox("__file_transfer_frame__"))
            {
                router.RegisterInbox<FileFrame, InvokeResult>("__file_transfer_frame__", 
                    (_, f) => _receiver.Incoming(f));
            }
            if (false == router.ContainsRequestorInbox("__file_transfer_complete_transfer__"))
            {
                router.RegisterInbox<FileEndFrame, InvokeResult>("__file_transfer_complete_transfer__", 
                    (_, f) => _receiver.Incoming(f));
            }
            if (false == router.ContainsRequestorInbox("__file_transfer_ping__"))
            {
                router.RegisterInbox<bool>("__file_transfer_ping__", (_) => true);
            }
        }
    }
}
