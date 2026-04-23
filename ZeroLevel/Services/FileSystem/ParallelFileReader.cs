using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
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

        // Reference counting: each handed-out IViewAccessor increments _refCount.
        // Dispose() only physically releases the MMF when _refCount reaches 0.
        // This prevents AccessViolation/SIGSEGV on Windows/Linux when a cached
        // ParallelFileReader is evicted while reads are still in flight.
        private int _refCount;
        private volatile bool _disposeRequested;
        private readonly object _refLock = new object();

        public ParallelFileReader(string filePath)
        {
            _fileLength = new FileInfo(filePath).Length;
            if (_fileLength == 0)
            {
                // empty file — MMF.CreateFromFile rejects capacity 0 on both Win and Linux;
                // leave _mmf null and let GetAccessor return null gracefully
                return;
            }
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _mmf = MemoryMappedFile.CreateFromFile(_stream, null, _stream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                }
                else
                {
                    // null name → anonymous mapping (no kernel namespace object, faster)
                    _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Fault to create MMF for {filePath}");
                _stream?.Dispose();
                _stream = null!;
                _mmf = null!;
            }
        }

        public IViewAccessor GetAccessor(long offset)
        {
            if (_mmf == null! || offset < 0 || offset >= _fileLength) return null!;
            Acquire();
            try
            {
                var length = _fileLength - offset;
                return new MMFViewAccessor(_mmf.CreateViewStream(offset, length, MemoryMappedFileAccess.Read), offset, Release);
            }
            catch
            {
                Release();
                throw;
            }
        }

        public IViewAccessor GetAccessor(long offset, int length)
        {
            if (_mmf == null! || length == 0 || offset < 0 || offset >= _fileLength) return null!;
            if ((offset + length) > _fileLength)
            {
                throw new OutOfMemoryException($"Offset + Length ({offset + length}) more than file length ({_fileLength})");
            }
            Acquire();
            try
            {
                return new MMFViewAccessor(_mmf.CreateViewStream(offset, length, MemoryMappedFileAccess.Read), offset, Release);
            }
            catch
            {
                Release();
                throw;
            }
        }

        private void Acquire()
        {
            lock (_refLock)
            {
                if (_disposeRequested)
                    throw new ObjectDisposedException(nameof(ParallelFileReader));
                _refCount++;
            }
        }

        private void Release()
        {
            bool shouldDispose = false;
            lock (_refLock)
            {
                _refCount--;
                if (_refCount == 0)
                    Monitor.PulseAll(_refLock);
                if (_disposeRequested && _refCount == 0)
                    shouldDispose = true;
            }
            if (shouldDispose) PhysicalDispose();
        }

        /// <summary>
        /// Waits until all in-flight views have been released. Should be called after Dispose().
        /// Returns true if released within timeout, false on timeout.
        /// </summary>
        public bool WaitForRelease(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            lock (_refLock)
            {
                while (_refCount > 0)
                {
                    var remaining = deadline - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero) return false;
                    if (!Monitor.Wait(_refLock, remaining)) return false;
                }
                return true;
            }
        }

        private void PhysicalDispose()
        {
            try { _mmf?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
            _mmf = null!;
            _stream = null!;
        }

        public void Dispose()
        {
            bool shouldDispose = false;
            lock (_refLock)
            {
                if (_disposeRequested) return;
                _disposeRequested = true;
                if (_refCount == 0) shouldDispose = true;
            }
            if (shouldDispose) PhysicalDispose();
        }
    }
}
