using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ZeroLevel.SqLite
{
    public sealed class DuplicateRecord
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        [Indexed]
        public string Hash { get; set; }
        public long Timestamp { get; set; }
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Хранит данные указанное число дней, и позволяет выполнить проверку наличия данных, для отбрасывания дубликатов
    /// </summary>
    public sealed class SqLiteDupStorage
        : BaseSqLiteDB<DuplicateRecord>
    {
        #region Fields

        private readonly long _removeOldRecordsTaskKey;
        private readonly int _countDays;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        #region Private members
        private void RemoveOldRecordsTask(long key)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Delete(r => r.Timestamp < DateTime.Now.AddDays(-_countDays).Ticks);
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

        public SqLiteDupStorage(string database_file_path, int period)
            : base(database_file_path)
        {
            CreateTable();
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
            _rwLock.EnterReadLock();
            var exists = new List<byte[]>();
            try
            {
                foreach (var record in SelectBy(r => r.Hash == hash))
                {
                    exists.Add(record.Data);
                }
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
                Append(new DuplicateRecord
                {
                    Data = body,
                    Hash = hash,
                    Timestamp = timestamp
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

        protected override void DisposeStorageData()
        {
            Sheduller.Remove(_removeOldRecordsTaskKey);
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
