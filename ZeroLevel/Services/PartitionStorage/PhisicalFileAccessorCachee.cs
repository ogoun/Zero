using System;
using System.IO;
using System.Threading;
using ZeroLevel.Collections;
using ZeroLevel.Services.Cache;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Memory;

namespace ZeroLevel.Services.PartitionStorage
{
    internal sealed class PhisicalFileAccessorCachee
        : IDisposable
    {
        private readonly TimerCachee<ParallelFileReader> _indexReadersCachee;
        private readonly TimerCachee<ParallelFileReader> _dataReadersCachee;

        private readonly ConcurrentHashSet<string> _lockedFiles = new ConcurrentHashSet<string>();

        public PhisicalFileAccessorCachee(TimeSpan dataExpirationPeriod, TimeSpan indexExpirationPeriod)
        {
            _dataReadersCachee = new TimerCachee<ParallelFileReader>(dataExpirationPeriod, s => new ParallelFileReader(s), i => i.Dispose(), 8192);
            _indexReadersCachee = new TimerCachee<ParallelFileReader>(indexExpirationPeriod, s => new ParallelFileReader(s), i => i.Dispose(), 8192);
        }

        #region DATA
        public void DropDataReader(string filePath)
        {
            _dataReadersCachee.Drop(filePath);
        }

        private ParallelFileReader GetDataReader(string filePath)
        {
            if (File.Exists(filePath) == false)
                throw new FileNotFoundException(filePath);
            return _dataReadersCachee.Get(filePath);
        }
        public IViewAccessor GetDataAccessor(string filePath, long offset)
        {
            if (_lockedFiles.Contains(filePath)) return null!;
            var reader = GetDataReader(filePath);
            IViewAccessor view;
            try
            {
                view = reader.GetAccessor(offset);
            }
            catch (ObjectDisposedException)
            {
                _dataReadersCachee.Drop(filePath);
                reader = _dataReadersCachee.Get(filePath);
                view = reader.GetAccessor(offset);
            }
            // race re-check: if a LockFile slipped in between Contains and GetAccessor,
            // release this view so the writer's WaitForRelease can proceed
            if (_lockedFiles.Contains(filePath))
            {
                view?.Dispose();
                return null!;
            }
            return view;
        }

        public IViewAccessor GetDataAccessor(string filePath, long offset, int length)
        {
            if (_lockedFiles.Contains(filePath)) return null!;
            var reader = GetDataReader(filePath);
            IViewAccessor view;
            try
            {
                view = reader.GetAccessor(offset, length);
            }
            catch (ObjectDisposedException)
            {
                _dataReadersCachee.Drop(filePath);
                reader = _dataReadersCachee.Get(filePath);
                view = reader.GetAccessor(offset, length);
            }
            if (_lockedFiles.Contains(filePath))
            {
                view?.Dispose();
                return null!;
            }
            return view;
        }
        public void DropAllDataReaders()
        {
            _dataReadersCachee.DropAll();
        }
        #endregion

        #region Indexes
        public void DropIndexReader(string filePath)
        {
            _indexReadersCachee.Drop(filePath);
        }

        private ParallelFileReader GetIndexReader(string filePath)
        {
            if (File.Exists(filePath) == false)
                throw new FileNotFoundException(filePath);
            return _indexReadersCachee.Get(filePath);
        }
        public IViewAccessor GetIndexAccessor(string filePath, long offset)
        {
            if (_lockedFiles.Contains(filePath)) return null!;
            var reader = GetIndexReader(filePath);
            IViewAccessor view;
            try
            {
                view = reader.GetAccessor(offset);
            }
            catch (ObjectDisposedException)
            {
                _indexReadersCachee.Drop(filePath);
                reader = _indexReadersCachee.Get(filePath);
                view = reader.GetAccessor(offset);
            }
            if (_lockedFiles.Contains(filePath))
            {
                view?.Dispose();
                return null!;
            }
            return view;
        }

        public IViewAccessor GetIndexAccessor(string filePath, long offset, int length)
        {
            if (_lockedFiles.Contains(filePath)) return null!;
            var reader = GetIndexReader(filePath);
            IViewAccessor view;
            try
            {
                view = reader.GetAccessor(offset, length);
            }
            catch (ObjectDisposedException)
            {
                _indexReadersCachee.Drop(filePath);
                reader = _indexReadersCachee.Get(filePath);
                view = reader.GetAccessor(offset, length);
            }
            if (_lockedFiles.Contains(filePath))
            {
                view?.Dispose();
                return null!;
            }
            return view;
        }
        public void DropAllIndexReaders()
        {
            _indexReadersCachee.DropAll();
        }
        #endregion

        private static readonly TimeSpan _defaultLockTimeout = TimeSpan.FromSeconds(60);

        public void LockFile(string filePath)
        {
            LockFile(filePath, _defaultLockTimeout);
        }

        public void LockFile(string filePath, TimeSpan timeout)
        {
            AcquireFileLock(filePath, timeout);
            DropDataReader(filePath);
            DropIndexReader(filePath);
        }

        /// <summary>
        /// Locks the file, evicts cached readers, and waits until any in-flight views
        /// hand back their references before returning. Use before destructive file operations
        /// (delete, rename, truncate) to avoid Windows sharing violations and Linux silent
        /// stale-data reads.
        /// </summary>
        public void LockFileAndWait(string filePath, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            AcquireFileLock(filePath, timeout);
            if (_dataReadersCachee.TryRemove(filePath, out var dataReader) && dataReader != null!)
            {
                dataReader.Dispose();
                var remaining = deadline - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero) dataReader.WaitForRelease(remaining);
            }
            if (_indexReadersCachee.TryRemove(filePath, out var indexReader) && indexReader != null!)
            {
                indexReader.Dispose();
                var remaining = deadline - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero) indexReader.WaitForRelease(remaining);
            }
        }

        private void AcquireFileLock(string filePath, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!_lockedFiles.Add(filePath))
            {
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException($"Failed to acquire file lock on '{filePath}' within {timeout}");
                Thread.Sleep(10);
            }
        }

        public void UnlockFile(string filePath)
        {
            _lockedFiles.TryRemove(filePath);
        }

        public void Dispose()
        {
            _lockedFiles.Clear();
            _dataReadersCachee.Dispose();
            _indexReadersCachee.Dispose();
        }
    }
}
