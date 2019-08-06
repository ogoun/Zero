using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace ZeroLevel.SqlServer
{
    public abstract class BaseSqlDbMapper
    {
        protected abstract IDbMapper Mapper { get; }
        protected readonly string _tableName;

        public string TableName { get { return _tableName; } }

        protected BaseSqlDbMapper(string tableName)
        {
            _tableName = tableName;
        }

        public object GetIdentity(object entity)
        {
            if (Mapper.IdentityField != null)
                return Mapper.IdentityField.Getter(entity);
            return null;
        }

        public string IdentityName
        {
            get
            {
                return Mapper.IdentityField?.Name;
            }
        }

        #region QUERIES
        #region INDEXES
        public string GetIndexExistsQuery(IDbField field)
        {
            if (field.IsIndexed)
            {
                return string.Format(
                    "SELECT COUNT(*) FROM sys.indexes WHERE name = 'idx_{0}_{1}' AND object_id = OBJECT_ID('{2}')",
                    _tableName, field.Name, _tableName);
            }
            return null;
        }

        public string GetCreateIndexQuery(IDbField field)
        {
            if (field.IsIndexed)
            {
                return string.Format("CREATE INDEX idx_{0}_{1} ON [{2}]({3});", _tableName, field.Name, _tableName, field.Name);
            }
            return null;
        }
        #endregion

        #region CREATE
        public static HashSet<DbType> FieldsHasSize = new HashSet<DbType>
        {
            DbType.String, DbType.Decimal, DbType.AnsiString, DbType.AnsiStringFixedLength,
            DbType.StringFixedLength, DbType.VarNumeric
        };

        private string _createString = null;
        private readonly object _createStringBuildLocker = new object();
        public string GetCreateQuery(bool rebuild = false)
        {
            lock (_createStringBuildLocker)
            {
                if (_createString == null || rebuild)
                {
                    StringBuilder create = new StringBuilder("CREATE TABLE [" + _tableName + "]");
                    create.Append("(");
                    Mapper.TraversalFields(f =>
                    {
                        var sqlType = DbTypeMapper.ToSqlDbType(f.ClrType);
                        create.Append("[" + f.Name + "] " + sqlType);
                        if (FieldsHasSize.Contains(f.DbType) && f.Size != 0)
                        {
                            if (f.DbType == DbType.Decimal)
                            {
                                int p = 19, s = 4;
                                if (f.Size > 0)
                                {
                                    p = (int)f.Size;
                                    if (s >= p)
                                    {
                                        if (p <= 2) s = 0;
                                        else s = p - 1;
                                    }
                                }
                                create.AppendFormat("({0},{1})", p, s);
                            }
                            else
                            {
                                create.AppendFormat("({0})", ((f.Size == -1) ? "max" : f.Size.ToString()));
                            }
                        }
                        if (f.IsIdentity)
                        {
                            create.Append(" PRIMARY KEY");
                        }
                        if (f.AllowNull)
                        {
                            create.Append(" NULL");
                        }
                        else
                        {
                            create.Append(" NOT NULL");
                        }
                        if (f.AutoIncrement)
                        {
                            create.Append(" IDENTITY (0, 1)");
                        }
                        create.Append(",");
                    });
                    _createString = create.ToString().TrimEnd(',') + ")";
                }
            }
            return _createString;
        }
        #endregion
        #endregion

        public SqlParameter[] CreateSqlDbParameters(object entity)
        {
            if (entity.GetType() != Mapper.EntityType)
                throw new InvalidCastException("Entity type is different from serializer entity type");
            var list = new List<SqlParameter>();
            Mapper.TraversalFields(field => 
            {
                var par = new SqlParameter();
                par.Value = ValueToSqlServerObject(field.Getter(entity), field.ClrType);
                // ADO.NET bug
                // https://connect.microsoft.com/VisualStudio/feedback/details/381934/sqlparameter-dbtype-dbtype-time-sets-the-parameter-to-sqldbtype-datetime-instead-of-sqldbtype-time
                if (field.DbType == DbType.Time)
                {
                    par.SqlDbType = SqlDbType.Time;
                }
                else
                {
                    par.DbType = field.DbType; // Если задать в конструкторе, то тип может переопределиться при задании значения
                }
                par.ParameterName = field.Name;
                list.Add(par);
            });
            return list.ToArray();
        }

        #region Datetime helper
        private static DateTime MinSqlDbDateTimeValue = new DateTime(1753, 01, 01);

        protected object ValueToSqlServerObject(object obj, Type type)
        {
            if (type == typeof(DateTime))
            {
                return DateTimeToSqlDbValue((DateTime)obj);
            }
            return obj ?? DBNull.Value;
        }
        /// <summary>
        /// Подготовка даты к записи в SQLServer
        /// (минимальные значения даты в .NET и SQL Server отличаются)
        /// </summary>
        protected object DateTimeToSqlDbValue(DateTime dt)
        {
            if (DateTime.Compare(dt, MinSqlDbDateTimeValue) <= 0)
                return DBNull.Value;
            return dt;
        }
        /// <summary>
        /// Конвертер из элементов строки DataTable в DonNet тип
        /// </summary>
        /// <typeparam name="Tout">Тип на выходе</typeparam>
        /// <param name="value">Значение из БД</param>
        /// <returns>Результат</returns>
        protected Tout Convert<Tout>(object value)
        {
            if (null == value || DBNull.Value == value)
                return default(Tout);
            return (Tout)System.Convert.ChangeType(value, typeof(Tout));
        }
        #endregion
    }
}
