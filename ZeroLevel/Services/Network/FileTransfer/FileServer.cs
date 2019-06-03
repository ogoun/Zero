using System;
using System.IO;
using System.Threading;
using ZeroLevel.Models;
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

            _service.RegisterInbox<FileStartFrame, InvokeResult>("__upload_file_start", (f, _, client) => Receiver.Incoming(f, nameMapper(client)));
            _service.RegisterInbox<FileFrame, InvokeResult>("__upload_file_frame", (f, _, __) => Receiver.Incoming(f));
            _service.RegisterInbox<FileEndFrame, InvokeResult>("__upload_file_complete", (f, _, __) => Receiver.Incoming(f));
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
            
            /*
            Log.Info($"Start upload file {reader.Path}");
            var startinfo = reader.GetStartInfo();
            using (var signal = new ManualResetEvent(false))
            {
                bool next = false;

                if (false == task.Client.RequestBackward<FileStartFrame, InvokeResult>("__upload_file_start", startinfo,
                    r =>
                    {
                        next = r.Success;
                        signal.Set();
                    }).Success)
                {
                    next = false;
                    signal.Set();
                }
                signal.WaitOne(5000);
                if (next)
                {
                    foreach (var chunk in reader.Read())
                    {
                        signal.Reset();
                        if (task.Client.RequestBackward<FileFrame, InvokeResult>("__upload_file_frame", chunk, r => next = r.Success).Success == false)
                        {
                            next = false;
                            signal.Set();
                        }
                        signal.WaitOne();
                        if (!next)
                        {
                            break;
                        }
                    }
                }
                if (next)
                {
                    task.Client.RequestBackward<FileEndFrame, InvokeResult>("__upload_file_complete", reader.GetCompleteInfo(), r =>
                    {
                        if (r.Success == false)
                        {
                            Log.Warning($"Unsuccess send file. {r.Comment}");
                        }
                    });
                }
            }
            Log.Debug($"Stop upload file {reader.Path}");*/
        }
    }
}
