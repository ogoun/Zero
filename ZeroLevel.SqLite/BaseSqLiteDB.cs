using System;
using System.Data.SQLite;
using System.IO;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.SqLite
{
    public abstract class BaseSqLiteDB
    {
        #region Helpers
        protected static bool HasColumn(SQLiteDataReader dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
        protected static Tr Read<Tr>(SQLiteDataReader reader, int index)
        {
            if (reader == null || reader.FieldCount <= index || reader[index] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(Tr))) != null)
            {
                return (Tr)Convert.ChangeType(reader[index], t);
            }
            return (Tr)Convert.ChangeType(reader[index], typeof(Tr));
        }
        protected static Tr Read<Tr>(SQLiteDataReader reader, string name)
        {
            if (reader == null || HasColumn(reader, name) || reader[name] == DBNull.Value) return default;
            Type t;
            if ((t = Nullable.GetUnderlyingType(typeof(Tr))) != null)
            {
                return (Tr)Convert.ChangeType(reader[name], t);
            }
            return (Tr)Convert.ChangeType(reader[name], typeof(Tr));
        }

        protected static void Execute(string query, SQLiteConnection connection, SQLiteParameter[] parameters = null)
        {
            using (var cmd = new SQLiteCommand(query, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                cmd.ExecuteNonQuery();
            }
        }

        protected static object ExecuteScalar(string query, SQLiteConnection connection, SQLiteParameter[] parameters = null)
        {
            using (var cmd = new SQLiteCommand(query, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                return cmd.ExecuteScalar();
            }
        }

        protected static SQLiteDataReader Read(string query, SQLiteConnection connection, SQLiteParameter[] parameters = null)
        {
            using (var cmd = new SQLiteCommand(query, connection))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                return cmd.ExecuteReader();
            }
        }

        protected static string PrepareDb(string path)
        {
            if (Path.IsPathRooted(path) == false)
            {
                path = Path.Combine(FSUtils.GetAppLocalDbDirectory(), path);
            }
            if (!File.Exists(path))
            {
                SQLiteConnection.CreateFile(path);
            }
            return Path.GetFullPath(path);
        }

        #endregion Helpers
    }
}
