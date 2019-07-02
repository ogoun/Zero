using System;
using System.Threading;
using ZeroLevel.Models;

namespace ZeroLevel.Network.FileTransfer
{
    public sealed class FileClient
        : BaseFileTransfer, IFileClient
    {
        private readonly ExClient _client;
        private readonly string _baseFolder;
        private readonly ClientFolderNameMapper _nameMapper;
        private readonly bool _disposeClient;

        internal FileClient(ExClient client, string baseFolder, ClientFolderNameMapper nameMapper, bool disposeClient)
            : base(baseFolder)
        {
            _client = client ?? throw new Exception(nameof(client));
            _baseFolder = baseFolder ?? throw new Exception(nameof(baseFolder));
            _nameMapper = nameMapper ?? throw new Exception(nameof(nameMapper));
            _disposeClient = disposeClient;

            _client.Router.RegisterInbox<FileStartFrame>("__upload_file_start", (c, f) => Receiver.Incoming(f, nameMapper(c)));
            _client.Router.RegisterInbox<FileFrame>("__upload_file_frame", (c, f) => Receiver.Incoming(f));
            _client.Router.RegisterInbox<FileEndFrame>("__upload_file_complete", (c, f) => Receiver.Incoming(f));
        }

        public void Dispose()
        {
            if (_disposeClient)
            {
                _client?.Dispose();
            }
        }

        public void Send(string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null)
        {
            PushTransferTask(fileName, completeHandler, errorHandler);
        }

        internal override void ExecuteSendFile(FileReader reader, FileTransferTask task)
        {
            Log.Info($"Start upload file {reader.Path}");
            var startinfo = reader.GetStartInfo();

            using (var signal = new ManualResetEvent(false))
            {
                bool next = false;
                if (false == _client.Request<FileStartFrame, InvokeResult>("__upload_file_start", startinfo,
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
                        if (_client.Request<FileFrame, InvokeResult>("__upload_file_frame", chunk, r =>
                        {
                            next = r.Success;
                            signal.Set();
                        }).Success == false)
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
                    _client.Request<FileEndFrame, InvokeResult>("__upload_file_complete", reader.GetCompleteInfo(), r =>
                    {
                        if (r.Success == false)
                        {
                            Log.Warning($"Unsuccess send file. {r.Comment}");
                        }
                    });
                }
            }
            Log.Debug($"Stop upload file {reader.Path}");
        }
    }
}
