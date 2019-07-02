using System;
using ZeroLevel.Models;

namespace ZeroLevel.Network.FileTransfer
{
    public sealed class FileServer
        : BaseFileTransfer, IFileServer
    {
        private readonly IRouter _service;
        private readonly string _baseFolder;
        private readonly ServerFolderNameMapperDelegate _nameMapper;
        private readonly bool _disposeService;

        internal FileServer(IRouter service, string baseFolder, ServerFolderNameMapperDelegate nameMapper, bool disposeService)
            : base(baseFolder)
        {
            _service = service ?? throw new Exception(nameof(service));
            _baseFolder = baseFolder ?? throw new Exception(nameof(baseFolder));
            _nameMapper = nameMapper ?? throw new Exception(nameof(nameMapper));
            _disposeService = disposeService;

            _service.RegisterInbox<FileStartFrame, InvokeResult>("__upload_file_start", (client, f) => Receiver.Incoming(f, nameMapper(client)));
            _service.RegisterInbox<FileFrame, InvokeResult>("__upload_file_frame", (client, f) => Receiver.Incoming(f));
            _service.RegisterInbox<FileEndFrame, InvokeResult>("__upload_file_complete", (client, f) => Receiver.Incoming(f));
        }

        public void Send(ExClient client, string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null)
        {
            PushTransferTask(fileName, completeHandler, errorHandler, client);
        }

        internal override void ExecuteSendFile(FileReader reader, FileTransferTask task)
        {
            Log.Info($"Start upload file {reader.Path}");
            var startinfo = reader.GetStartInfo();
            if (false == task.Client.Send<FileStartFrame>("__upload_file_start", startinfo).Success)
            {
                return;
            }
            foreach (var chunk in reader.Read())
            {
                if (task.Client.Send<FileFrame>("__upload_file_frame", chunk).Success == false)
                {
                    return;
                }
            }
            task.Client.Send<FileEndFrame>("__upload_file_complete", reader.GetCompleteInfo());
            Log.Debug($"Stop upload file {reader.Path}");
        }
    }
}
