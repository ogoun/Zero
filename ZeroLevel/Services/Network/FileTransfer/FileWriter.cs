using System;
using System.Collections.Generic;
using System.IO;
using ZeroLevel.Models;
using ZeroLevel.Services.HashFunctions;
using ZeroLevel.Services.Network.FileTransfer.Writers;

namespace ZeroLevel.Network.FileTransfer
{
    internal sealed class FileWriter
    {
        private string _basePath;
        private readonly Dictionary<long, SafeDataWriter> _incoming = new Dictionary<long, SafeDataWriter>();
        private readonly object _locker = new object();
        private long _cleanErrorsTaskId;

        public FileWriter(string path)
        {
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
                            _incoming.Add(info.UploadFileTaskId, new SafeDataWriter(new DiskFileWriter(path, info.Size)
                                , () => Remove(info.UploadFileTaskId)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[FileWriter.Incoming(FileStartFrame)]", ex);
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        public InvokeResult Incoming(FileFrame chunk)
        {
            try
            {
                SafeDataWriter writer;
                if (_incoming.TryGetValue(chunk.UploadFileTaskId, out writer))
                {
                    var hash = Murmur3.ComputeHash(chunk.Payload);
                    var checksumL = BitConverter.ToUInt64(hash, 0);
                    var checksumH = BitConverter.ToUInt64(hash, 8);
                    if (chunk.ChecksumH != checksumH
                        || chunk.ChecksumL != checksumL)
                        return InvokeResult.Fault("Checksum incorrect");

                    writer.Write(chunk);
                    return InvokeResult.Succeeding();
                }
                return InvokeResult.Fault("File not expected.");
            }
            catch (Exception ex)
            {
                Log.Error("[FileWriter.Incoming(FileFrame)]", ex);
                return InvokeResult.Fault(ex.Message);
            }
        }

        public InvokeResult Incoming(FileEndFrame info)
        {
            try
            {
                lock (_locker)
                {
                    SafeDataWriter writer;
                    if (_incoming.TryGetValue(info.UploadFileTaskId, out writer) && writer != null!)
                    {
                        writer.CompleteReceiving();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[FileWriter.Incoming(FileEndFrame)]", ex);
                return InvokeResult.Fault(ex.Message);
            }
            return InvokeResult.Succeeding();
        }

        private void Remove(long uploadTaskId)
        {
            SafeDataWriter stream;
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
