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
        private FileStream _stream;
        private readonly long _fileLength;
        public long FileLength => _fileLength;

        public ParallelFileReader(string filePath)
        {
            _fileLength = new FileInfo(filePath).Length;
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _mmf = MemoryMappedFile.CreateFromFile(_stream, null, _stream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                }
                else
                {
                    //_stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    //_mmf = MemoryMappedFile.CreateFromFile(_stream, null, _stream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                    _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, Guid.NewGuid().ToString(), 0, MemoryMappedFileAccess.Read);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Fault to create MMF for {filePath}");
            }
            //_stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public IViewAccessor GetAccessor(long offset)
        {
            var length = _fileLength - offset;
            return new MMFViewAccessor(_mmf.CreateViewStream(offset, length, MemoryMappedFileAccess.Read), offset);
            //return new StreamVewAccessor(_stream);
        }

        public IViewAccessor GetAccessor(long offset, int length)
        {
            if ((offset + length) > _fileLength)
            {
                throw new OutOfMemoryException($"Offset + Length ({offset + length}) more than file length ({_fileLength})");
            }
            //return new StreamVewAccessor(_stream);
            return new MMFViewAccessor(_mmf.CreateViewStream(offset, length, MemoryMappedFileAccess.Read), offset);
        }

        public void Dispose()
        {
            _mmf?.Dispose();
            _stream?.Dispose();
        }
    }
}
