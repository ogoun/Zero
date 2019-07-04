using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using ZeroLevel.Services.Pools;

namespace ZeroLevel.Network.FileTransfer
{
    public abstract class BaseFileTransfer
    {
        private readonly FileReceiver _receiver;
        internal FileReceiver Receiver => _receiver;

        private ObjectPool<FileTransferTask> _taskPool = new ObjectPool<FileTransferTask>(() => new FileTransferTask(), 100);
        private BlockingCollection<FileTransferTask> _tasks = new BlockingCollection<FileTransferTask>();
        private readonly Thread _uploadFileThread;

        internal BaseFileTransfer(string baseFolder)
        {
            _receiver = new FileReceiver(baseFolder);
            _uploadFileThread = new Thread(UploadFileProcessing);
            _uploadFileThread.IsBackground = true;
            _uploadFileThread.Start();
        }

        protected void PushTransferTask(string filePath, Action<string> completeHandler = null, Action<string, string> errorHandler = null, ExClient client = null)
        {
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
                    ExecuteSendFile(GetReaderFor(task.FilePath), task);
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

        internal abstract void ExecuteSendFile(FileReader reader, FileTransferTask task);

        private FileReader GetReaderFor(string filePath)
        {
            return new FileReader(filePath);
        }
    }
}
