using System;
using System.Data;
using System.Data.Common;

namespace ZeroLevel.SqlServer
{
    public class DbReader
        : IDisposable
    {
        private readonly DbConnection _connection;
        private readonly IDbCommand _command;

        public DbReader(DbConnection connection, IDbCommand command)
        {
            _connection = connection;
            _command = command;
        }

        public IDataReader GetReader()
        {
            return _command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public void Dispose()
        {
            _command.Dispose();
            _connection.Close();
            _connection.Dispose();
        }
    }

    public static class DBReader
    {
        public static T Read<T>(this DbDataReader reader, int index)
        {
            if (reader == null) return default;
            if (reader[index] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader[index], t);
            }
            return (T)Convert.ChangeType(reader[index], typeof(T));
        }
        public static T Read<T>(this DbDataReader reader, string name)
        {
            if (reader == null) return default;
            if (reader[name] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader[name], t);
            }
            return (T)Convert.ChangeType(reader[name], typeof(T));
        }
        public static T Read<T>(this IDataReader reader, int index)
        {
            if (reader == null) return default;
            if (reader[index] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader[index], t);
            }
            return (T)Convert.ChangeType(reader[index], typeof(T));
        }
        public static T Read<T>(this IDataReader reader, string name)
        {
            if (reader == null) return default;
            if (reader[name] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader[name], t);
            }
            return (T)Convert.ChangeType(reader[name], typeof(T));
        }

        public static T Read<T>(this DataRow reader, int index)
        {
            if (reader == null) return default;
            if (reader.ItemArray[index] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader.ItemArray[index], t);
            }
            return (T)Convert.ChangeType(reader.ItemArray[index], typeof(T));
        }
        public static T Read<T>(this DataRow reader, string name)
        {
            if (reader == null) return default;
            if (reader[name] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader[name], t);
            }
            return (T)Convert.ChangeType(reader[name], typeof(T));
        }
    }
}
