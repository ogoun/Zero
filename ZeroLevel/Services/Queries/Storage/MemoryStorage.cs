using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZeroLevel.Services.ObjectMapping;

namespace ZeroLevel.Patterns.Queries
{
    public class MemoryStorage<T> :
       IStorage<T>
    {
        private readonly MemoryStorageQueryBuilder<T> _queryBuilder;
        private readonly HashSet<T> _memory;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public MemoryStorage()
        {
            _queryBuilder = new MemoryStorageQueryBuilder<T>();
            _memory = new HashSet<T>();
        }

        public QueryResult Count()
        {
            _lock.EnterReadLock();
            try
            {
                return QueryResult.Result(_memory.Count);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public QueryResult Count(IQuery query)
        {
            _lock.EnterReadLock();
            try
            {
                var q = _queryBuilder.Build(query);
                return QueryResult.Result(_memory.Count(q.Query));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }


        public IEnumerable<T> Get()
        {
            _lock.EnterReadLock();
            try
            {
                return _memory.Select(i => (T)TypeMapper.CopyDTO(i));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IEnumerable<T> Get(IQuery query)
        {
            _lock.EnterReadLock();
            try
            {
                var q = _queryBuilder.Build(query);
                return _memory.Where(q.Query).Select(i => (T)TypeMapper.CopyDTO(i));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public QueryResult Post(T obj)
        {
            _lock.EnterWriteLock();
            try
            {
                var insert = (T)TypeMapper.CopyDTO(obj);
                if (_memory.Add(insert))
                {
                    return QueryResult.Result(1);
                }
                return QueryResult.Fault("Already exists");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public QueryResult Remove(IQuery query)
        {
            _lock.EnterWriteLock();
            try
            {
                var q = _queryBuilder.Build(query);
                var removed = _memory.RemoveWhere(i => q.Query(i));
                if (removed > 0)
                {
                    return QueryResult.Result(removed);
                }
                return QueryResult.Fault("Not found");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public QueryResult Remove(T obj)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_memory.Remove(obj))
                {
                    return QueryResult.Result(1);
                }
                return QueryResult.Fault("Not found");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Drop()
        {
            _lock.EnterWriteLock();
            try
            {
                _memory.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
