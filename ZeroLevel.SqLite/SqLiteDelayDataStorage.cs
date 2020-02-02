using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading;
using ZeroLevel.Services.Serialization;
using ZeroLevel.Services.Shedulling;

namespace ZeroLevel.SqLite
{
    public sealed class SqLiteDelayDataStorage<T>
        : BaseSqLiteDB, IDisposable
        where T : IBinarySerializable
    {
        #region Fields

        private readonly IExpirationSheduller _sheduller;
        private readonly Func<T, DateTime> _expire_date_calc_func;
        private readonly SQLiteConnection _db;
        private readonly string _table_name;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        #region Ctor

        public SqLiteDelayDataStorage(string database_file_path,
            Func<T, bool> expire_callback,
            Func<T, DateTime> expire_date_calc_func)
        {
            this._expire_date_calc_func = expire_date_calc_func;
            var path = PrepareDb(database_file_path);
            _table_name = "expiration";
            _db = new SQLiteConnection($"Data Source={path};Version=3;");
            _db.Open();
            Execute($"CREATE TABLE IF NOT EXISTS {_table_name} (id INTEGER PRIMARY KEY AUTOINCREMENT, body BLOB, expirationtime INTEGER)", _db);
            Execute($"CREATE INDEX IF NOT EXISTS expirationtime_index ON {_table_name} (expirationtime)", _db);
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
                Execute($"INSERT INTO {_table_name} ('body', 'expirationtime') values (@body, @expirationtime)", _db,
                    new SQLiteParameter[]
                    {
                        new SQLiteParameter("body", MessageSerializer.Serialize(packet)),
                        new SQLiteParameter("expirationtime", expirationTime)
                    });
                id = (long)ExecuteScalar("select last_insert_rowid();", _db);
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
            SQLiteDataReader reader;
            _rwLock.EnterReadLock();
            try
            {
                reader = Read($"SELECT id, expirationtime FROM {_table_name}", _db);
                while (reader.Read())
                {
                    var id = reader.GetInt64(0);
                    _sheduller.Push(new DateTime(reader.GetInt64(1), DateTimeKind.Local), (k) => Pop(id));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLiteDelayDataStorage] Fault preload datafrom db");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            reader = null;
        }

        private void Pop(long id)
        {
            try
            {
                byte[] body;
                _rwLock.EnterReadLock();
                try
                {
                    body = (byte[])ExecuteScalar($"SELECT body FROM {_table_name} WHERE id=@id", _db, new SQLiteParameter[] { new SQLiteParameter("id", id) });
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
                Execute($"DELETE FROM {_table_name} WHERE id = @id", _db,
                    new SQLiteParameter[] { new SQLiteParameter("id", id) });
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

        public void Dispose()
        {
            try
            {
                _db?.Close();
                _db?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLiteDelayDataStorage] Fault close db connection");
            }
            _sheduller.Dispose();
        }

        #endregion IDisposable
    }
}
