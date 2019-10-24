using System;
using System.Collections.Generic;
using System.IO;
using ZeroLevel.Services.HashFunctions;

namespace ZeroLevel.Network.FileTransfer
{
    internal sealed class FileReader
    {
        private readonly FileStartFrame _startInfo;
        public string Path { get; }
        private const int CHUNK_SIZE = 512 * 1024;

        public FileReader(string path)
        {
            Path = path;
            _startInfo = FileStartFrame.GetTransferFileInfo(path);
        }

        public FileStartFrame GetStartInfo()
        {
            return _startInfo;
        }

        public IEnumerable<FileFrame> Read()
        {
            long offset = 0;
            using (FileStream stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int bytesRead;
                var buffer = new byte[CHUNK_SIZE];
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var fragment = new FileFrame
                    {
                        UploadFileTaskId = _startInfo.UploadFileTaskId,
                        Offset = offset * CHUNK_SIZE,
                        Payload = new byte[bytesRead]
                    };
                    Array.Copy(buffer, 0, fragment.Payload, 0, bytesRead);
                    var hash = Murmur3.ComputeHash(fragment.Payload);

                    fragment.ChecksumL = BitConverter.ToUInt64(hash, 0);
                    fragment.ChecksumH = BitConverter.ToUInt64(hash, 8);

                    offset = offset + 1;
                    yield return fragment;
                }
            }            
        }

        public FileEndFrame GetCompleteInfo()
        {
            return new FileEndFrame { UploadFileTaskId = _startInfo.UploadFileTaskId };
        }
    }
}
