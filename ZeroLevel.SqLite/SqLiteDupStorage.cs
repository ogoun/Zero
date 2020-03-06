using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ZeroLevel.SqLite
{
    /// <summary>
    /// Хранит данные указанное число дней, и позволяет выполнить проверку наличия данных, для отбрасывания дубликатов
    /// </summary>
    public sealed class SqLiteDupStorage
        : BaseSqLiteDB, IDisposable
    {
        #region Fields

        private const string DEFAUL_TABLE_NAME = "History";

        private readonly SQLiteConnection _db;
        private readonly long _removeOldRecordsTaskKey;
        private readonly int _countDays;
        private readonly string _table_name;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        #region Private members

        private sealed class DuplicationStorageRecord
        {
            public string Hash { get; set; }
            public byte[] Body { get; set; }
            public long Timestamp { get; set; }
        }

        private void RemoveOldRecordsTask(long key)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Execute($"DELETE FROM {_table_name} WHERE timestamp < @limit", _db,
                    new SQLiteParameter[] { new SQLiteParameter("limit", DateTime.Now.AddDays(-_countDays).Ticks) });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SQLiteDupStorage] Fault remove old records from db");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        #endregion Private members

        #region Ctor

        public SqLiteDupStorage(string database_file_path, string tableName, int period)
        {
            var path = PrepareDb(database_file_path);
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _table_name = DEFAUL_TABLE_NAME;
            }
            else
            {
                _table_name = tableName;
            }
            _db = new SQLiteConnection($"Data Source={path};Version=3;");
            _db.Open();
            Execute($"CREATE TABLE IF NOT EXISTS {_table_name} (id INTEGER PRIMARY KEY AUTOINCREMENT, hash TEXT, body BLOB, timestamp INTEGER)", _db);
            Execute($"CREATE INDEX IF NOT EXISTS hash_index ON {_table_name} (hash)", _db);
            _countDays = period > 0 ? period : 1;
            _removeOldRecordsTaskKey = Sheduller.RemindEvery(TimeSpan.FromMinutes(1), RemoveOldRecordsTask);
        }

        #endregion Ctor

        #region API

        /// <summary>
        /// true в случае обнаружения дубликата
        /// </summary>
        public bool TestAndInsert(byte[] body)
        {
            var hash = GenerateSHA256String(body);
            var timestamp = DateTime.Now.Ticks;
            SQLiteDataReader reader;
            _rwLock.EnterReadLock();
            var exists = new List<byte[]>();
            try
            {
                reader = Read($"SELECT body FROM {_table_name} WHERE hash=@hash", _db, new SQLiteParameter[] { new SQLiteParameter("hash", hash) });
                while (reader.Read())
                {
                    exists.Add((byte[])reader.GetValue(0));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SQLiteDupStorage] Fault search existing records by hash ({hash})");
                // no return!
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            reader = null;
            if (exists.Any())
            {
                foreach (var candidate in exists)
                {
                    if (ArrayExtensions.UnsafeEquals(candidate, body))
                        return true;
                }
            }
            _rwLock.EnterWriteLock();
            try
            {
                Execute($"INSERT INTO {_table_name} ('hash', 'body', 'timestamp') values (@hash, @body, @timestamp)", _db,
                    new SQLiteParameter[]
                    {
                        new SQLiteParameter("hash", hash),
                        new SQLiteParameter("body", body),
                        new SQLiteParameter("timestamp", timestamp)
                    });
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SQLiteDupStorage] Fault insert record in duplications storage. Hash '{hash}'. Timestamp '{timestamp}'.");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            return false;
        }

        #endregion API

        #region IDisposable

        public void Dispose()
        {
            Sheduller.Remove(_removeOldRecordsTaskKey);
            try
            {
                _db?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SQLiteDupStorage] Fault close db connection");
            }
        }

        #endregion IDisposable

        #region Helpers

        private static string GenerateSHA256String(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256Managed.Create())
            {
                byte[] hash = sha256.ComputeHash(bytes);
                return ByteArrayToString(hash);
            }
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        #endregion Helpers
    }
}
