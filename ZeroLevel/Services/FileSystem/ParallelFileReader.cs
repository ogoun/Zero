using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using ZeroLevel.Services.Memory;

namespace ZeroLevel.Services.FileSystem
{
    public sealed class ParallelFileReader
        : IDisposable
    {
        private MemoryMappedFile _mmf;
        private readonly long _fileLength;
        public long FileLength => _fileLength;

        public ParallelFileReader(string filePath)
        {
            _fileLength = new FileInfo(filePath).Length;
            _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, Guid.NewGuid().ToString(), 0, MemoryMappedFileAccess.Read);
        }

        public IViewAccessor GetAccessor(long offset)
        {
            var length = _fileLength - offset;
            return new MMFViewAccessor(_mmf.CreateViewStream(offset, length, MemoryMappedFileAccess.Read), offset);
        }

        public IViewAccessor GetAccessor(long offset, int length)
        {
            if ((offset + length) > _fileLength)
            {
                throw new OutOfMemoryException($"Offset + Length ({offset + length}) more than file length ({_fileLength})");
            }
            return new MMFViewAccessor(_mmf.CreateViewStream(offset, length, MemoryMappedFileAccess.Read), offset);
        }

        public void Dispose()
        {
            _mmf?.Dispose();
        }
    }
}
