using System;
using System.IO;
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
            if (false == _lockedFiles.Contains(filePath))
            {
                var reader = GetDataReader(filePath);
                try
                {
                    return reader.GetAccessor(offset);
                }
                catch (ObjectDisposedException)
                {
                    _dataReadersCachee.Drop(filePath);
                    reader = _dataReadersCachee.Get(filePath);
                }
                return reader.GetAccessor(offset);
            }
            return null;
        }

        public IViewAccessor GetDataAccessor(string filePath, long offset, int length)
        {
            if (false == _lockedFiles.Contains(filePath))
            {
                var reader = GetDataReader(filePath);
                try
                {
                    return reader.GetAccessor(offset, length);
                }
                catch (ObjectDisposedException)
                {
                    _dataReadersCachee.Drop(filePath);
                    reader = _dataReadersCachee.Get(filePath);
                }
                return reader.GetAccessor(offset, length);
            }
            return null;
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
            if (false == _lockedFiles.Contains(filePath))
            {
                var reader = GetIndexReader(filePath);
                try
                {
                    return reader.GetAccessor(offset);
                }
                catch (ObjectDisposedException)
                {
                    _indexReadersCachee.Drop(filePath);
                    reader = _indexReadersCachee.Get(filePath);
                }
                return reader.GetAccessor(offset);
            }
            return null;
        }

        public IViewAccessor GetIndexAccessor(string filePath, long offset, int length)
        {
            if (false == _lockedFiles.Contains(filePath))
            {
                var reader = GetIndexReader(filePath);
                try
                {
                    return reader.GetAccessor(offset, length);
                }
                catch (ObjectDisposedException)
                {
                    _indexReadersCachee.Drop(filePath);
                    reader = _indexReadersCachee.Get(filePath);
                }
                return reader.GetAccessor(offset, length);
            }
            return null;
        }
        public void DropAllIndexReaders()
        {
            _indexReadersCachee.DropAll();
        }
        #endregion

        public void LockFile(string filePath)
        {
            _lockedFiles.Add(filePath);
            DropDataReader(filePath);
            DropIndexReader(filePath);
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
