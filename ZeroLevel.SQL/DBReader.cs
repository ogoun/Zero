using System;
using System.Data.Common;

namespace ZeroLevel.SqlServer
{
    public static class DBReader
    {
        public static T Read<T>(this DbDataReader reader, int index)
        {
            if (reader[index] == DBNull.Value) return default(T);
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader[index], t);
            }
            return (T)Convert.ChangeType(reader[index], typeof(T));
        }
        public static T Read<T>(this DbDataReader reader, string name)
        {
            if (reader[name] == DBNull.Value) return default(T);
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(T))) != null)
            {
                return (T)Convert.ChangeType(reader[name], t);
            }
            return (T)Convert.ChangeType(reader[name], typeof(T));
        }
    }
}
