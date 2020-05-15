using System;
using System.Data.SQLite;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.SqLite
{
    /// <summary>
    /// Промежуточное/временное хранилище пакетов данных, для случаев сбоя доставок через шину данных
    /// </summary>
    public sealed class SqLitePacketBuffer<T>
        : BaseSqLiteDB, IDisposable
        where T : IBinarySerializable
    {
        private sealed class PacketBufferRecord
        {
            public int Id { get; set; }
            public byte[] Body { get; set; }
        }

        #region Fields

        private readonly SQLiteConnection _db;
        private readonly string _table_name;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        public SqLitePacketBuffer(string database_file_path)
        {
            var path = PrepareDb(database_file_path);
            _table_name = "packets";
            _db = new SQLiteConnection($"Data Source={path};Version=3;");
            _db.Open();
            Execute($"CREATE TABLE IF NOT EXISTS {_table_name} (id INTEGER PRIMARY KEY AUTOINCREMENT, body BLOB, created INTEGER)", _db);
            Execute($"CREATE INDEX IF NOT EXISTS created_index ON {_table_name} (created)", _db);
        }

        public void Push(T frame)
        {
            long id = -1;
            _rwLock.EnterWriteLock();
            var creationTime = DateTime.Now.Ticks;
            try
            {
                Execute($"INSERT INTO {_table_name} ('body', 'created') values (@body, @created)", _db,
                    new SQLiteParameter[]
                    {
                        new SQLiteParameter("body", MessageSerializer.Serialize(frame)),
                        new SQLiteParameter("created", creationTime)
                    });
                id = (long)ExecuteScalar("select last_insert_rowid();", _db);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLitePacketBuffer] Fault insert record in buffer storage.");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public bool Pop(Func<T, bool> pop_callback)
        {
            bool success = false;
            long id = -1;
            SQLiteDataReader reader;
            _rwLock.EnterReadLock();
            try
            {
                reader = Read($"SELECT id, body FROM {_table_name} ORDER BY created ASC LIMIT 1", _db);
                if (reader.Read())
                {
                    id = reader.GetInt64(0);
                    var body = (byte[])reader.GetValue(1);
                    try
                    {
                        success = pop_callback(MessageSerializer.Deserialize<T>(body));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Fault handle buffered data");
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLitePacketBuffer] Fault preload datafrom db");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            if (success)
            {
                RemoveRecordById(id);
            }
            reader = null;
            return success;
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
                Log.Error(ex, $"[SqLitePacketBuffer] Fault remove record by id '{id}'");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            try
            {
                _db?.Close();
                _db?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLitePacketBuffer] Fault close db connection");
            }
        }
    }
}
