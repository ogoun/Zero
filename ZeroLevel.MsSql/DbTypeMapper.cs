using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ZeroLevel.MsSql
{
    public static class DbTypeMapper
    {
        static Dictionary<Type, DbType> typeMap;

        static DbTypeMapper()
        {
            typeMap = new Dictionary<Type, DbType>
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = DbType.Time,
                [typeof(byte[])] = DbType.Binary,
                [typeof(byte?)] = DbType.Byte,
                [typeof(sbyte?)] = DbType.SByte,
                [typeof(short?)] = DbType.Int16,
                [typeof(ushort?)] = DbType.UInt16,
                [typeof(int?)] = DbType.Int32,
                [typeof(uint?)] = DbType.UInt32,
                [typeof(long?)] = DbType.Int64,
                [typeof(ulong?)] = DbType.UInt64,
                [typeof(float?)] = DbType.Single,
                [typeof(double?)] = DbType.Double,
                [typeof(decimal?)] = DbType.Decimal,
                [typeof(bool?)] = DbType.Boolean,
                [typeof(char?)] = DbType.StringFixedLength,
                [typeof(Guid?)] = DbType.Guid,
                [typeof(DateTime?)] = DbType.DateTime,
                [typeof(DateTimeOffset?)] = DbType.DateTimeOffset,
                [typeof(TimeSpan?)] = DbType.Time,
                [typeof(object)] = DbType.Object
            };
        }

        /// <summary>
        /// Для value типов помеченных как Nullable вытаскивает оригинальный value тип
        /// Не value и не nullable типы не преобразуются
        /// </summary>
        private static Type GetNonNullableType(Type t)
        {
            if (t.IsValueType)
            {
                // Detect Nullable<T>
                if (Nullable.GetUnderlyingType(t) != null)
                {
                    return t.GenericTypeArguments.Length > 0 ? t.GenericTypeArguments[0] : t;
                }
            }
            return t;
        }

        public static DbType ToDbType(this Type type)
        {
            DbType dbType;
            var theType = GetNonNullableType(type);
            if (theType.IsEnum && !typeMap.ContainsKey(type))
            {
                theType = Enum.GetUnderlyingType(theType);
            }
            if (typeMap.TryGetValue(theType, out dbType))
            {
                return dbType;
            }
            return DbType.Object;
        }

        public static SqlDbType ToSqlDbType(this Type testType)
        {
            var theType = GetNonNullableType(testType);
            if (theType.IsEnum)
            {
                return Enum.GetUnderlyingType(theType).ToSqlDbType();
            }
            if (theType == typeof(Byte[]) || theType == typeof(byte[])) return SqlDbType.Image;
            if (theType == typeof(UInt16) || theType == typeof(ushort)) return SqlDbType.Int;
            if (theType == typeof(UInt32) || theType == typeof(uint)) return SqlDbType.BigInt;
            if (theType == typeof(UInt64) || theType == typeof(ulong)) return SqlDbType.Decimal;
            if (theType == typeof(TimeSpan)) return SqlDbType.Time;
            return new SqlParameter() { DbType = (DbType)Enum.Parse(typeof(DbType), theType.Name) }.SqlDbType;
        }

        public static Type ToClrType(string sqlType)
        {
            switch (sqlType.Trim().ToLowerInvariant())
            {
                case "bigint":
                    return typeof(long);

                case "binary":
                case "image":
                case "timestamp":
                case "varbinary":
                    return typeof(byte[]);

                case "bit":
                    return typeof(bool);

                case "char":
                case "nchar":
                case "ntext":
                case "nvarchar":
                case "text":
                case "varchar":
                case "xml":
                    return typeof(string);

                case "datetime":
                case "smalldatetime":
                case "date":
                case "datetime2":
                    return typeof(DateTime);

                case "time":
                    return typeof(TimeSpan);

                case "decimal":
                case "money":
                case "smallmoney":
                    return typeof(decimal);

                case "float":
                    return typeof(double);

                case "int":
                    return typeof(int);

                case "real":
                    return typeof(float);

                case "uniqueidentifier":
                    return typeof(Guid);

                case "smallint":
                    return typeof(short);

                case "tinyint":
                    return typeof(byte);

                case "variant":
                case "udt":
                    return typeof(object);

                case "structured":
                    return typeof(DataTable);

                case "datetimeoffset":
                    return typeof(DateTimeOffset);

                default:
                    throw new ArgumentOutOfRangeException(sqlType);
            }
        }
    }
}
