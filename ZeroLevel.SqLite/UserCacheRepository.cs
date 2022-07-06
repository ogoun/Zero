using SQLite;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.SqLite
{
    public sealed class DataRecord
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        [Indexed]
        public string Key { get; set; }
        public byte[] Data { get; set; }
    }
    public sealed class UserCacheRepository<T>
         : BaseSqLiteDB<DataRecord>
             where T : IBinarySerializable
    {
        #region Fields

        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        #region Ctor

        public UserCacheRepository()
            : base(typeof(T).Name)
        {
            CreateTable();
        }

        #endregion Ctor

        #region API

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string KEY(long userId, string name) => $"{userId}.{name}";

        public bool AddOrUpdate(long userid, string name, T data)
        {
            var key = KEY(userid, name);
            bool update = false;
            _rwLock.EnterReadLock();
            try
            {
                var count_obj = Count(r => r.Key == key);
                if (count_obj > 0)
                {
                    update = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[UserCacheRepository] Fault search existing records by name ({name})");
                // no return!
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            _rwLock.EnterWriteLock();
            try
            {
                var body = MessageSerializer.Serialize(data);
                if (update)
                {
                    var r = Single(r => r.Key == key);
                    r.Data = body;
                    Update(r);
                }
                else
                {
                    Append(new DataRecord
                    {
                        Data = body,
                        Key = key
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[UserCacheRepository] Fault insert record in storage. UserId: {userid}. Name '{name}'. Data: {typeof(T).Name}.");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            return false;
        }

        public void Remove(long userid, string name)
        {
            var key = KEY(userid, name);
            _rwLock.EnterWriteLock();
            try
            {
                Delete(r => r.Key == key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[UserCacheRepository] Fault remove record from db by name '{name}'. UserId: {userid}. Data: {typeof(T).Name}.");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public long Count(long userid, string name)
        {
            var key = KEY(userid, name);
            _rwLock.EnterWriteLock();
            try
            {
                return Count(r => r.Key == key);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[UserCacheRepository] Fault get count record from db by name '{name}'. UserId: {userid}. Data: {typeof(T).Name}.");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            return 0;
        }

        public IEnumerable<T> GetAll(long userid, string name)
        {
            var key = KEY(userid, name);
            var result = new List<T>();
            _rwLock.EnterReadLock();
            try
            {
                foreach (var r in SelectBy(r=>r.Key == key))
                {
                    var data = r.Data;
                    if (data != null)
                    {
                        result.Add(MessageSerializer.Deserialize<T>(data));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[UserCacheRepository] Fault read all records by name: {name}. UserId: {userid}. Data: {typeof(T).Name}.");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return result;
        }

        #endregion API

        #region IDisposable
        
        protected override void DisposeStorageData()
        {
        }

        #endregion IDisposable
    }
}
