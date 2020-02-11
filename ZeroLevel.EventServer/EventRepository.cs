using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;
using System.Threading;
using ZeroLevel.SqLite;

namespace ZeroLevel.EventServer
{
    public class EventRepository
        :BaseSqLiteDB
    {
        private readonly SQLiteConnection _db;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private readonly string _tableName;

        public EventRepository()
        {
            _tableName = "events";

            var path = PrepareDb($"{_tableName}.db");
            _db = new SQLiteConnection($"Data Source={path};Version=3;");
            _db.Open();

            Execute($"CREATE TABLE IF NOT EXISTS {_tableName} (id INTEGER PRIMARY KEY AUTOINCREMENT, key TEXT, body BLOB)", _db);
            Execute($"CREATE INDEX IF NOT EXISTS key_index ON {_tableName} (key)", _db);
        }
    }
}
