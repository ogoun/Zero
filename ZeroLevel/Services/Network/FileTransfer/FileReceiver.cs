using System;
using System.Collections.Generic;
using System.IO;
using ZeroLevel.Models;
using ZeroLevel.Services.Network.FileTransfer.Model;

namespace ZeroLevel.Services.Network.FileTransfer
{
    public class FileReceiver
    {
        private class FileWriter
            : IDisposable
        {
            private readonly FileStream _stream;
            internal DateTime _writeTime { get; private set; } = DateTime.UtcNow;
            private bool _gotCompleteMessage = false;

            public bool GotCompleteMessage() => _gotCompleteMessage = true;

            public bool ReadyToRemove()
            {
                if (_gotCompleteMessage)
                {
                    return (DateTime.UtcNow - _writeTime).TotalSeconds > 15;
                }
                return false;
            }

            public FileWriter(string path)
            {
                _stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            }

            public void Write(long offset, byte[] data)
            {
                _stream.Position = offset;
                _stream.Write(data, 0, data.Length);
                _writeTime = DateTime.Now;
            }

            public bool IsTimeoutBy(TimeSpan period)
            {
                return (DateTime.Now - _writeTime) > period;
            }

            public void Dispose()
            {
                _stream.Flush();
                _stream.Close();
                _stream.Dispose();
            }
        }
        private string _basePath;
        private string _disk_prefix;

        private readonly Dictionary<long, FileWriter> _incoming = new Dictionary<long, FileWriter>();
        private readonly object _locker = new object();
        private long _cleanErrorsTaskId;

        public FileReceiver(string path, string disk_prefix = "DRIVE_")
        {
            _disk_prefix = disk_prefix;
            _basePath = path;
            _cleanErrorsTaskId = Sheduller.RemindEvery(TimeSpan.FromMinutes(1), CleanBadFiles);
        }


        private void CleanBadFiles()
        {
            lock (_locker)
            {
                foreach (var pair in _incoming)
                {
                    if (pair.Value.IsTimeoutBy(TimeSpan.FromMinutes(3)) || pair.Value.ReadyToRemove())
                    {
                        Remove(pair.Key);
                    }
                }
            }
        }

        public InvokeResult Incoming(FileStartFrame info, string clientFolderName)
        {
            try
            {
                if (false == _incoming.ContainsKey(info.UploadFileTaskId))
                {
                    lock (_locker)
                    {
                        if (false == _incoming.ContainsKey(info.UploadFileTaskId))
                        {
                            string path = BuildFilePath(clientFolderName, info.FilePath);
                            _incoming.Add(info.UploadFileTaskId, new FileWriter(path));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[FileReceiver]", ex);
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Incoming(FileFrame chunk)
        {
            try
            {
                FileWriter stream;
                if (_incoming.TryGetValue(chunk.UploadFileTaskId, out stream))
                {
                    stream.Write(chunk.Offset, chunk.Payload);
                    return InvokeResult.Succeeding();
                }
                return InvokeResult.Fault("File not expected.");
            }
            catch (Exception ex)
            {
                Log.Error("[FileReceiver]", ex);
                return InvokeResult.Fault(ex.Message);
            }
        }

        public InvokeResult Incoming(FileEndFrame info)
        {
            try
            {
                lock (_locker)
                {
                    FileWriter stream;
                    if (_incoming.TryGetValue(info.UploadFileTaskId, out stream))
                    {
                        stream.GotCompleteMessage();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[FileReceiver]", ex);
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        private void Remove(long uploadTaskId)
        {
            FileWriter stream;
            if (_incoming.TryGetValue(uploadTaskId, out stream))
            {
                _incoming.Remove(uploadTaskId);
                stream?.Dispose();
            }
        }

        private string BuildFilePath(string client_folder_name, string clientPath)
        {
            if (string.IsNullOrEmpty(client_folder_name))
            {
                if (false == Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                }
                return Path.Combine(_basePath, Path.GetFileName(clientPath));
            }
            else
            {
                string folder = Path.Combine(Path.Combine(_basePath, client_folder_name), Path.GetDirectoryName(clientPath).Replace(":", "_DRIVE"));
                if (false == System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                }
                return Path.Combine(folder, Path.GetFileName(clientPath));
            }
        }
    }
}
