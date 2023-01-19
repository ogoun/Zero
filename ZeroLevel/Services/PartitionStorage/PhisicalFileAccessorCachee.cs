using System;
using System.IO;
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

        public IViewAccessor GetDataAccessor(string filePath, long offset, int length)
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

        public IViewAccessor GetIndexAccessor(string filePath, long offset, int length)
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
        public void DropAllIndexReaders()
        {
            _indexReadersCachee.DropAll();
        }
        #endregion

        public void Dispose()
        {
            _dataReadersCachee.Dispose();
            _indexReadersCachee.Dispose();
        }
    }
}
