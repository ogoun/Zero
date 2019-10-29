using System;
using System.Data;
using System.Data.Common;

namespace ZeroLevel.SqlServer
{
    public class DbMapper<T> : DbMapper, IDbMapper<T>
    {
        public DbMapper(bool mapOnlyMarkedMembers) : base(typeof(T), mapOnlyMarkedMembers)
        {
        }

        public new T Deserialize(DataRow row)
        {
            if (null == row) throw new ArgumentNullException(nameof(row));
            var result = Activator.CreateInstance<T>();
            foreach (var field in _fields)
            {
                field.Value.SetValue(result, row[field.Key], typeConverter);
            }
            return result;
        }

        public new T Deserialize(DbDataReader reader)
        {
            if (null == reader) throw new ArgumentNullException(nameof(reader));
            var result = Activator.CreateInstance<T>();
            foreach (var field in _fields)
            {
                field.Value.SetValue(result, reader[field.Key], typeConverter);
            }
            return result;
        }
    }
}
