using SQLite;
using System;
using System.Threading;
using ZeroLevel.Services.Serialization;
using ZeroLevel.Services.Shedulling;

namespace ZeroLevel.SqLite
{
    public sealed class ExpirationRecord
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        [Indexed]
        public long Expiration { get; set; }
        public byte[] Data { get; set; }
    }

    public sealed class SqLiteDelayDataStorage<T>
        : BaseSqLiteDB<ExpirationRecord>
        where T : class, IBinarySerializable, new()
    {
        #region Fields

        private readonly IExpirationSheduller _sheduller;
        private readonly Func<T, DateTime> _expire_date_calc_func;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        #region Ctor

        public SqLiteDelayDataStorage(string database_file_path,
            Func<T, bool> expire_callback,
            Func<T, DateTime> expire_date_calc_func)
            : base(database_file_path)
        {
            this._expire_date_calc_func = expire_date_calc_func;
            CreateTable();
            _sheduller = Sheduller.CreateExpirationSheduller();
            OnExpire += expire_callback;
            Preload();
        }

        #endregion Ctor

        #region API

        public event Func<T, bool> OnExpire;

        public bool Push(T packet)
        {
            DateTime expirationDate;
            try
            {
                expirationDate = _expire_date_calc_func(packet);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLiteDelayDataStorage] Fault append data to storage");
                return false;
            }
            var expirationTime = expirationDate.Ticks;
            _rwLock.EnterWriteLock();
            long id = -1;
            try
            {
                var r = Append(new ExpirationRecord { Expiration = expirationTime, Data = MessageSerializer.Serialize(packet) });
                id = r.Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteDelayDataStorage] Fault insert record in delay storage. Expiration time: '{expirationTime}'.");
                return false;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            _sheduller.Push(expirationDate, (k) => Pop(id));
            return true;
        }

        #endregion API

        #region Private members

        private void Preload()
        {
            _rwLock.EnterReadLock();
            try
            {
                foreach (var record in SelectAll())
                {
                    _sheduller.Push(new DateTime(record.Expiration, DateTimeKind.Local), (k) => Pop(record.Id));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLiteDelayDataStorage] Fault preload datafrom db");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        private void Pop(long id)
        {
            try
            {
                byte[] body;
                _rwLock.EnterReadLock();
                try
                {
                    body = Single(r=>r.Id == id)?.Data;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[SqLiteDelayDataStorage] Fault get body by id '{id}'");
                    RemoveRecordById(id);
                    return;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
                T packet;
                try
                {
                    packet = MessageSerializer.Deserialize<T>(body);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[SqLiteDelayDataStorage] Fault deserialize body. Id '{id}'");
                    RemoveRecordById(id);
                    return;
                }
                if (OnExpire?.Invoke(packet) ?? false)
                {
                    RemoveRecordById(id);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLiteDelayDataStorage] Сбой обработки отложенной записи из DB");
            }
        }

        private void RemoveRecordById(long id)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Delete(r => r.Id == id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteDelayDataStorage] Fault remove record by id '{id}'");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        #endregion Private members

        #region IDisposable

        protected override void DisposeStorageData()
        {
            _sheduller.Dispose();
        }

        #endregion IDisposable
    }
}
