using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly Dictionary<int, FileWriter> _incoming = new Dictionary<int, FileWriter>();
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
                    if (pair.Value.IsTimeoutBy(TimeSpan.FromMinutes(3)))
                    {
                        Remove(pair.Key);
                    }
                }
            }
        }

        public void Incoming(FileStartFrame info, string clientFolderName)
        {
            if (false == _incoming.ContainsKey(info.FileUploadTaskId))
            {
                lock (_locker)
                {
                    if (false == _incoming.ContainsKey(info.FileUploadTaskId))
                    {
                        string path = BuildFilePath(clientFolderName, info.FilePath);
                        _incoming.Add(info.FileUploadTaskId, new FileWriter(path));
                    }
                }
            }
        }

        public void Incoming(FileFrame chunk)
        {
            FileWriter stream;
            if (_incoming.TryGetValue(chunk.UploadTaskId, out stream))
            {
                stream.Write(chunk.Offset, chunk.Payload);
            }
        }

        public void Incoming(FileEndFrame info)
        {
            lock (_locker)
            {
                Remove(info.FileUploadTaskId);
            }
        }

        private void Remove(int uploadTaskId)
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
