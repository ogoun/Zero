using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using ZeroLevel.Models;
using ZeroLevel.Services.Pools;

namespace ZeroLevel.Network.FileTransfer
{
    public sealed class FileSender
    {
        private BlockingCollection<FileTransferTask> _tasks = new BlockingCollection<FileTransferTask>();
        private ObjectPool<FileTransferTask> _taskPool = new ObjectPool<FileTransferTask>(() => new FileTransferTask(), 100);
        private readonly Thread _uploadFileThread;
        private bool _resendWhenServerError = false;
        private bool _resendWhenClientError = false;

        public void ResendWhenServerError(bool resend = true) => _resendWhenServerError = resend;
        public void ResendWhenClientError(bool resend = true) => _resendWhenClientError = resend;

        public FileSender()
        {
            _uploadFileThread = new Thread(UploadFileProcessing);
            _uploadFileThread.IsBackground = true;
            _uploadFileThread.Start();
        }

        public void Send(IClient client, string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null)
        {
            if (client == null) return;
            PushTransferTask(client, fileName, completeHandler, errorHandler);
        }

        private void PushTransferTask(IClient client, string filePath, Action<string> completeHandler = null, Action<string, string> errorHandler = null)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (false == File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }
            var task = _taskPool.Allocate();
            task.CompletedHandler = completeHandler;
            task.ErrorHandler = errorHandler;
            task.FilePath = filePath;
            task.Client = client;
            _tasks.Add(task);
        }

        private void UploadFileProcessing()
        {
            while (true)
            {
                var task = _tasks.Take();
                try
                {
                    ExecuteSendFile(new FileReader(task.FilePath), task);
                    task.CompletedHandler?.Invoke(task.FilePath);
                }
                catch (Exception ex)
                {
                    task.ErrorHandler?.Invoke(task.FilePath, ex.ToString());
                }
                finally
                {
                    _taskPool.Free(task);
                }
            }
        }

        public bool Connected(IClient client, TimeSpan timeout)
        {
            bool connected = false;
            using (var waiter = new ManualResetEvent(false))
            {
                client.Request<bool>("__file_transfer_ping__", (response) => { connected = response; waiter.Set(); });
                waiter.WaitOne(timeout);
            }
            return connected;
        }

        private static bool Send<T>(IClient client, string inbox, T frame,
            bool resendWhenConnectionError, bool resendWhenServerError)
        {
            bool sended = false;
            var handle = new Action<InvokeResult>(ir =>
            {
                if (ir.Success == false && resendWhenServerError)
                {
                    Send<T>(client, inbox, frame, resendWhenConnectionError, false);
                }
            });
            sended = client.Request<T, InvokeResult>(inbox, frame, handle).Success;
            if (sended == false && resendWhenConnectionError)
            {
                sended = client.Request<T, InvokeResult>(inbox, frame, handle).Success;
            }
            return sended;
        }

        internal void ExecuteSendFile(FileReader reader, FileTransferTask task)
        {
            Log.Info($"Start upload file {reader.Path}");
            var startinfo = reader.GetStartInfo();

            if (!Send(task.Client, "__file_transfer_start_transfer__", startinfo, _resendWhenClientError, _resendWhenServerError))
            {
                Log.Debug($"Upload file {reader.Path} interrupted");
                return;
            }
            foreach (var chunk in reader.Read())
            {
                if (!Send(task.Client, "__file_transfer_frame__", chunk, _resendWhenClientError, _resendWhenServerError))
                {
                    Log.Debug($"Upload file {reader.Path} interrupted");
                    return;
                }
            }
            Send(task.Client, "__file_transfer_complete_transfer__", reader.GetCompleteInfo(), _resendWhenClientError, _resendWhenServerError);
            GC.Collect();
        }
    }
}
