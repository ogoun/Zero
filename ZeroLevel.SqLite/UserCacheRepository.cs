using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.SqLite
{
    public sealed class UserCacheRepository<T>
         : BaseSqLiteDB, IDisposable
             where T : IBinarySerializable
    {
        #region Fields

        private readonly SQLiteConnection _db;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly string _tableName;

        #endregion Fields

        #region Ctor

        public UserCacheRepository()
        {
            _tableName = typeof(T).Name;

            var path = PrepareDb($"{_tableName}_user_cachee.db");
            _db = new SQLiteConnection($"Data Source={path};Version=3;");
            _db.Open();

            Execute($"CREATE TABLE IF NOT EXISTS {_tableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, key TEXT, body BLOB)", _db);
            Execute($"CREATE INDEX IF NOT EXISTS key_index ON {_tableName} (key)", _db);
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
                var count_obj = ExecuteScalar($"SELECT COUNT(*) FROM {_tableName} WHERE key=@key", _db, new SQLiteParameter[] { new SQLiteParameter("key", key) });
                if (count_obj != null && (long)count_obj > 0)
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
                    Execute($"UPDATE {_tableName} SET body=@body WHERE key=@key", _db,
                    new SQLiteParameter[]
                    {
                            new SQLiteParameter("key", key),
                            new SQLiteParameter("body", body)
                    });
                }
                else
                {
                    Execute($"INSERT INTO {_tableName} ('key', 'body') values (@key, @body)", _db,
                    new SQLiteParameter[]
                    {
                            new SQLiteParameter("key", key),
                            new SQLiteParameter("body", body)
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
                Execute($"DELETE FROM {_tableName} WHERE key=@key", _db, new SQLiteParameter[]
                {
                        new SQLiteParameter("key", key)
                });
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
                return Convert.ToInt64(ExecuteScalar($"SELECT COUNT(*) FROM {_tableName} WHERE key=@key", _db, new SQLiteParameter[]
                {
                        new SQLiteParameter("key", key)
                }));
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
            SQLiteDataReader reader;
            _rwLock.EnterReadLock();
            try
            {
                reader = Read($"SELECT [body] FROM {_tableName} WHERE key=@key", _db, new SQLiteParameter[]
                {
                        new SQLiteParameter("key", key)
                });
                while (reader.Read())
                {
                    var data = Read<byte[]>(reader, 0);
                    if (data != null)
                    {
                        result.Add(MessageSerializer.Deserialize<T>(data));
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[UserCacheRepository] Fault read all records by name: {name}. UserId: {userid}. Data: {typeof(T).Name}.");
            }
            finally
            {
                _rwLock.ExitReadLock();
                reader = null;
            }
            return result;
        }

        #endregion API

        #region IDisposable

        public void Dispose()
        {
            try
            {
                _db?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UserCacheRepository] Fault close db connection");
            }
        }

        #endregion IDisposable
    }
}
