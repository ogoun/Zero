using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using ZeroLevel.Services.Pools;

namespace ZeroLevel.Network.FileTransfer
{
    public sealed class FileSender
    {
        private BlockingCollection<FileTransferTask> _tasks = new BlockingCollection<FileTransferTask>();
        private ObjectPool<FileTransferTask> _taskPool = new ObjectPool<FileTransferTask>(() => new FileTransferTask(), 100);
        private readonly Thread _uploadFileThread;

        public FileSender()
        {
            _uploadFileThread = new Thread(UploadFileProcessing);
            _uploadFileThread.IsBackground = true;
            _uploadFileThread.Start();
        }

        public void Send(ExClient client, string fileName, Action<string> completeHandler = null, Action<string, string> errorHandler = null)
        {
            PushTransferTask(client, fileName, completeHandler, errorHandler);
        }

        private void PushTransferTask(ExClient client, string filePath, Action<string> completeHandler = null, Action<string, string> errorHandler = null)
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

        public bool Connected(ExClient client, TimeSpan timeout)
        {
            bool connected = false;
            using (var waiter = new ManualResetEvent(false))
            {
                client.Request<bool>("__file_transfer_ping__", (response) => { connected = response; waiter.Set(); });
                waiter.WaitOne(timeout);
            }
            return connected;
        }

        internal void ExecuteSendFile(FileReader reader, FileTransferTask task)
        {
            Log.Info($"Start upload file {reader.Path}");
            var startinfo = reader.GetStartInfo();
            if (false == task.Client.Send<FileStartFrame>("__file_transfer_start_transfer__", startinfo).Success)
            {
                return;
            }
            foreach (var chunk in reader.Read())
            {
                if (task.Client.Send<FileFrame>("__file_transfer_frame__", chunk).Success == false)
                {
                    return;
                }
            }
            task.Client.Send<FileEndFrame>("__file_transfer_complete_transfer__", reader.GetCompleteInfo());
            Log.Debug($"Stop upload file {reader.Path}");
        }
    }
}
