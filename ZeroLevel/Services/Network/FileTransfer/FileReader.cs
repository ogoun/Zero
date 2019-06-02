using System;
using System.Collections.Generic;
using System.IO;
using ZeroLevel.Services.Network.FileTransfer.Model;

namespace ZeroLevel.Services.Network.FileTransfer
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
                        UploadTaskId = _startInfo.FileUploadTaskId,
                        Offset = offset * CHUNK_SIZE,
                        Payload = new byte[bytesRead]
                    };
                    Array.Copy(buffer, 0, fragment.Payload, 0, bytesRead);
                    offset = offset + 1;
                    yield return fragment;
                }
            }
            GC.Collect();
        }

        public FileEndFrame GetCompleteInfo()
        {
            return new FileEndFrame { FileUploadTaskId = _startInfo.FileUploadTaskId };
        }
    }
}
