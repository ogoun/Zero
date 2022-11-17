using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.Collections
{
    public sealed class BatchProcessor<T>
            : IDisposable
    {
        private readonly List<T> _batch;
        private readonly int _batchSize;
        private readonly Action<IReadOnlyList<T>> _insertAction;
        public BatchProcessor(int batchSize, Action<IReadOnlyList<T>> insertAction)
        {
            _batch = new List<T>(batchSize);
            _insertAction = insertAction;
            _batchSize = batchSize;
        }

        public void Add(T val)
        {
            _batch.Add(val);
            if (_batch.Count >= _batchSize)
            {
                try
                {
                    _insertAction.Invoke(_batch);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[BatchProcessor.Add] Fault insert");
                }
                _batch.Clear();
            }
        }

        public void Dispose()
        {
            if (_batch.Count > 0)
            {
                try
                {
                    _insertAction.Invoke(_batch);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[BatchProcessor.Dispose] Fault insert");
                }
            }
            _batch.Clear();
        }
    }
}
