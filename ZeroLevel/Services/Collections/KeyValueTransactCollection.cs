using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZeroLevel.Services.Collections
{
    /// <summary>
    /// Класс обертывает коллекцию вида ключ-значение и позволяет проводить над ней транзакционные обновления
    /// </summary>
    /// <typeparam name="TKey">Тип ключа коллекции</typeparam>
    /// <typeparam name="TValue">Тип значения коллекции</typeparam>
    public class KeyValueTransactCollection<TKey, TValue> :
        ITransactable
    {
        private ReaderWriterLockSlim _rwLock =
            new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        /// <summary>
        /// Коллекция
        /// </summary>
        readonly Dictionary<TKey, TValue> _collection = new Dictionary<TKey, TValue>();

        public KeyValueTransactCollection() { }
        /// <summary>
        /// Проверка наличия ключа
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasKey(TKey key)
        {
            try
            {
                _rwLock.EnterReadLock();
                return _collection.ContainsKey(key);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }
        /// <summary>
        /// Получение значения коллекции по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Значение</returns>
        public TValue this[TKey key]
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    TValue value;
                    if (_collection.TryGetValue(key, out value))
                        return (value);
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
                throw new KeyNotFoundException();
            }
        }
        /// <summary>
        /// Количество записей
        /// </summary>
        public int Count
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _collection.Count;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }
        /// <summary>
        /// Коллекция ключей
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _collection.Keys;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }
        /// <summary>
        /// Список ключ-значений
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> Items
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _collection;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }
        /// <summary>
        /// Коллекция значений
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                try
                {
                    _rwLock.EnterReadLock();
                    return _collection.Values;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        #region Transaction update

        /// <summary>
        /// Список не обновленных данных (т.е. тех которые удалены в базе)
        /// </summary>
        readonly List<TKey> _removingDate = new List<TKey>();
        /// <summary>
        /// Обновленные данные
        /// </summary>
        readonly Dictionary<TKey, TValue> _updatedRecords = new Dictionary<TKey, TValue>();
        /// <summary>
        /// Новые данные
        /// </summary>
        readonly Dictionary<TKey, TValue> _newRecords = new Dictionary<TKey, TValue>();

        void ClearTransactionDate()
        {
            _removingDate.Clear();
            _updatedRecords.Clear();
            _newRecords.Clear();
        }
        /// <summary>
        /// Добавление или обновления записи
        /// </summary>
        /// <param name="id">Идентификатор записи</param>
        /// <param name="value">Значение</param>
        public void Post(TKey id, TValue value)
        {
            if (_isUpdating.State == false)
            {
                throw new Exception("Method Post allowed only in transaction");
            }
            if (!HasKey(id))
            {
                if (_newRecords.ContainsKey(id) == false)
                {
                    _newRecords.Add(id, value);
                }
                else
                {
                    _newRecords[id] = value;
                }
            }
            else
            {
                if (!_collection[id]!.Equals(value))
                {
                    if (false == _updatedRecords.ContainsKey(id))
                    {
                        _updatedRecords.Add(id, value);
                    }
                    else
                    {
                        _updatedRecords[id] = value;
                    }
                }
                if (_removingDate.Contains(id))
                    _removingDate.Remove(id);
            }
            return;
        }
        #endregion

        readonly AtomicBoolean _isUpdating = new AtomicBoolean();

        public bool StartTransction()
        {
            if (_isUpdating.Set())
            {
                _removingDate.AddRange(_collection.Keys.ToArray());
                return true;
            }
            return false;
        }
        public bool Commit()
        {
            if (_isUpdating.State == false) return false;
            try
            {
                _rwLock.EnterWriteLock();
                foreach (TKey id in _removingDate)
                {
                    _collection.Remove(id);
                }
                foreach (TKey key in _newRecords.Keys)
                {
                    _collection.Add(key, _newRecords[key]);
                }
                foreach (TKey key in _updatedRecords.Keys)
                {
                    _collection[key] = _updatedRecords[key];
                }
            }
            finally
            {
                _rwLock.ExitWriteLock();
                ClearTransactionDate();
                _isUpdating.Reset();
            }
            return true;
        }
        public bool Rollback()
        {
            if (_isUpdating.State == false) return false;
            ClearTransactionDate();
            _isUpdating.Reset();
            return true;
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_collection != null!)
                {
                    foreach (TKey key in _collection.Keys)
                    {
                        var disposable = _collection[key] as IDisposable;
                        if (disposable != null!)
                            disposable.Dispose();
                    }
                    _collection.Clear();
                }
            }
        }
        #endregion
    }
}
