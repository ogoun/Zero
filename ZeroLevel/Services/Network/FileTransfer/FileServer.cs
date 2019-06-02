using System;
using System.IO;
using ZeroLevel.Network;
using ZeroLevel.Services.Network.FileTransfer.Model;

namespace ZeroLevel.Services.Network.FileTransfer
{
    public sealed class FileServer
        : BaseFileTransfer, IFileServer
    {
        private readonly IExService _service;
        private readonly string _baseFolder;
        private readonly ServerFolderNameMapperDelegate _nameMapper;
        private readonly bool _disposeService;

        internal FileServer(IExService service, string baseFolder, ServerFolderNameMapperDelegate nameMapper, bool disposeService)
            : base(baseFolder)
        {
            _service = service ?? throw new Exception(nameof(service));
            _baseFolder = baseFolder ?? throw new Exception(nameof(baseFolder));
            _nameMapper = nameMapper ?? throw new Exception(nameof(nameMapper));
            _disposeService = disposeService;

            _service.RegisterInbox<FileStartFrame>("__upload_file_start", (f, _, client) => Receiver.Incoming(f, nameMapper(client)));
            _service.RegisterInbox<FileFrame>("__upload_file_frame", (f, _, __) => Receiver.Incoming(f));
            _service.RegisterInbox<FileEndFrame>("__upload_file_complete", (f, _, __) => Receiver.Incoming(f));
        }

        public void Dispose()
        {
            if (_disposeService)
            {
                _service?.Dispose();
            }
        }

        public void Send(IZBackward client, string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null)
        {
            PushTransferTask(fileName, completeHandler, errorHandler, client);
        }

        internal override void ExecuteSendFile(FileReader reader, FileTransferTask task)
        {
            Log.Info($"Start upload file {reader.Path}");
            var startinfo = reader.GetStartInfo();
            startinfo.FilePath = Path.GetFileName(startinfo.FilePath);
            task.Client.SendBackward("__upload_file_start", startinfo);
            foreach (var chunk in reader.Read())
            {
                task.Client.SendBackward("__upload_file_frame", chunk);
            }
            task.Client.SendBackward("__upload_file_complete", reader.GetCompleteInfo());
            Log.Info($"Stop upload file {reader.Path}");
        }
    }
}
