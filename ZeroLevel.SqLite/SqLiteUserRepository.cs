using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using ZeroLevel.Models;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.SqLite
{
    public class SqLiteUserRepository
        : BaseSqLiteDB
    {
        #region Fields

        private readonly SQLiteConnection _db;
        private readonly string _table_name = "users";
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        #region Ctor

        public SqLiteUserRepository()
        {
            var path =PrepareDb("users.db");
            _db = new SQLiteConnection($"Data Source={path};Version=3;");
            _db.Open();
            Execute($"CREATE TABLE IF NOT EXISTS {_table_name} (id INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT, displayname TEXT, hash BLOB, timestamp INTEGER, creator INTEGER, role INTEGER)", _db);
            Execute($"CREATE INDEX IF NOT EXISTS username_index ON {_table_name} (username)", _db);
            Execute($"CREATE INDEX IF NOT EXISTS hash_index ON {_table_name} (hash)", _db);
        }

        #endregion Ctor

        public IEnumerable<User> GetAll()
        {
            var list = new List<User>();
            SQLiteDataReader reader;
            _rwLock.EnterReadLock();
            try
            {
                reader = Read($"SELECT id, username, displayname, hash, timestamp, creator, role FROM {_table_name}", _db);
                while (reader.Read())
                {
                    list.Add(new User
                    {
                        Id = reader.GetInt64(0),
                        UserName = reader.GetString(1),
                        DisplayName = Read<string>(reader, 2),
                        PasswordHash = (byte[])reader.GetValue(3),
                        Timestamp = reader.GetInt64(4),
                        Creator = reader.GetInt64(5),
                        Role = (UserRole)reader.GetInt32(6)
                    });
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLiteUserRepository] Fault get all users");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            reader = null;
            return list;
        }

        public User Get(long id)
        {
            User user = null;
            SQLiteDataReader reader;
            _rwLock.EnterReadLock();
            try
            {
                reader = Read($"SELECT id, username, displayname, hash, timestamp, creator, role FROM {_table_name} WHERE id = @id", _db,
                    new SQLiteParameter[] { new SQLiteParameter("id", id) });
                if (reader.Read())
                {
                    var body = (byte[])reader.GetValue(1);
                    user = new User
                    {
                        Id = reader.GetInt64(0),
                        UserName = reader.GetString(1),
                        DisplayName = reader.GetString(2),
                        PasswordHash = (byte[])reader.GetValue(3),
                        Timestamp = reader.GetInt64(4),
                        Creator = reader.GetInt64(5),
                        Role = (UserRole)reader.GetInt32(6)
                    };
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault get user by id '{id}'");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            reader = null;
            return user;
        }

        public User Get(string username, byte[] hash)
        {
            User user = null;
            SQLiteDataReader reader;
            _rwLock.EnterReadLock();
            try
            {
                reader = Read($"SELECT id, username, displayname, hash, timestamp, creator, role FROM {_table_name} WHERE username = @username AND hash = @hash", _db,
                    new SQLiteParameter[]
                    {
                        new SQLiteParameter("username", username),
                        new SQLiteParameter("hash", hash)
                    });
                if (reader.Read())
                {
                    user = new User
                    {
                        Id = reader.GetInt64(0),
                        UserName = reader.GetString(1),
                        DisplayName = reader.GetString(2),
                        PasswordHash = (byte[])reader.GetValue(3),
                        Timestamp = reader.GetInt64(4),
                        Creator = reader.GetInt64(5),
                        Role = (UserRole)reader.GetInt32(6)
                    };
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault get user by username '{username}' and pwdhash");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            reader = null;
            return user;
        }

        public InvokeResult<long> SaveUser(User user)
        {
            long id = -1;
            _rwLock.EnterWriteLock();
            var creationTime = DateTime.UtcNow.Ticks;
            try
            {
                var count_obj = ExecuteScalar($"SELECT COUNT(*) FROM {_table_name} WHERE username=@username", _db, new SQLiteParameter[] { new SQLiteParameter("username", user.UserName) });
                if (count_obj != null && (long)count_obj > 0)
                {
                    return InvokeResult<long>.Fault<long>("Пользователь уже существует");
                }
                Execute($"INSERT INTO {_table_name} ('username', 'displayname', 'hash', 'timestamp', 'creator', 'role') values (@username, @displayname, @hash, @timestamp, @creator, @role)", _db,
                    new SQLiteParameter[]
                    {
                        new SQLiteParameter("username", user.UserName),
                        new SQLiteParameter("displayname", user.DisplayName),
                        new SQLiteParameter("hash", user.PasswordHash),
                        new SQLiteParameter("timestamp", creationTime),
                        new SQLiteParameter("creator", user.Creator),
                        new SQLiteParameter("role", user.Role)
                    });
                id = (long)ExecuteScalar("select last_insert_rowid();", _db);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault insert user in storage.");
                InvokeResult<long>.Fault(ex.Message);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            return InvokeResult<long>.Succeeding(id);
        }

        public InvokeResult RemoveUser(string login)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Execute($"DELETE FROM {_table_name} WHERE username = @username", _db,
                    new SQLiteParameter[] { new SQLiteParameter("username", login) });
                return InvokeResult.Succeeding();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault remove user '{login}'");
                return InvokeResult.Fault(ex.Message);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
    }
}
