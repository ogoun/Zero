using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ZeroLevel.SqlServer
{
    /// <summary>
    /// Описание таблицы в БД
    /// </summary>
    public sealed class SqlDbTableInfo : TableInfo, IEquatable<SqlDbTableInfo>
    {
        #region Ctor
        /// <summary>
        /// Конструктор по-умолчанию
        /// </summary>
        /// <param name="name"></param>
        public SqlDbTableInfo(string name) : base(name)
        {
        }
        /// <summary>
        /// Конструктор по-умолчанию
        /// </summary>
        /// <param name="name"></param>
        public SqlDbTableInfo(SqlDbTableInfo other) : base(other)
        {
        }
        #endregion

        #region IEquatable
        /// <summary>
        /// Сравнение с другой таблицей
        /// </summary>
        public bool Equals(SqlDbTableInfo other)
        {
            return base.Equals(other);
        }
        #endregion

        #region Fill table info
        protected override IEnumerable<IndexInfo> GetIndexes(IDbProvider db)
        {
            var indexes = new List<IndexInfo>();
            string select = "exec sp_indexes_rowset [{0}]";
            using (var ds = db.ExecuteQuerySqlDataSet(string.Format(select, _name)))
            {
                using (var indexInfo = ds.Tables[0])
                {
                    foreach (DataRow row in indexInfo.Rows)
                    {
                        var i = new IndexInfo
                        {
                            Name = (string)row["INDEX_NAME"],                            
                            IsPrimaryKey = (bool)row["PRIMARY_KEY"],
                            IsUnique = (bool)row["UNIQUE"]
                        };
                        i.Columns.Add((string)row["COLUMN_NAME"]);
                        indexes.Add(i);
                    }
                }
            }
            return indexes;
        }

        protected override IEnumerable<ColumnInfo> GetColumns(IDbProvider db)
        {
            // Для уменьшения количества обращения к базе все данные по таблице в одном запросе
            var columns = new List<ColumnInfo>();
            string select = @"exec sp_columns [{0}]
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME='{0}'
exec sp_pkeys @table_name = [{0}]";
            using (var ds = db.ExecuteQuerySqlDataSet(string.Format(select, _name)))
            {
                if (ds.Tables.Count != 3)
                {
                    throw new InvalidOperationException("Не удалось получить данные по таблице " + _name);
                }
                var columnTypes = new Dictionary<string, string>();
                var columnSize = new Dictionary<string, long>();
                using (var dataTypeInfo = ds.Tables[1])
                {
                    foreach (DataRow row in dataTypeInfo.Rows)
                    {
                        columnTypes.Add((string)row["COLUMN_NAME"], (string)row["DATA_TYPE"]);
                        var maximum = row["CHARACTER_MAXIMUM_LENGTH"];
                        columnSize.Add((string)row["COLUMN_NAME"], (maximum != DBNull.Value) ? Convert.ToInt64(row["CHARACTER_MAXIMUM_LENGTH"]) : 0);
                    }
                }
                using (var tableInfo = ds.Tables[0])
                {
                    foreach (DataRow row in tableInfo.Rows)
                    {
                        var column = new ColumnInfo();
                        column.Name = (string)row["COLUMN_NAME"];
                        column.Size = columnSize[column.Name];
                        column.DbType = columnTypes[column.Name];
                        column.AllowNull = (short)row["NULLABLE"] == 1;
                        column.DotNetType = DbTypeMapper.ToClrType(columnTypes[column.Name]);
                        column.AutoInc = ((string)row["TYPE_NAME"]).Contains("identity");
                        columns.Add(column);
                    }
                }
                using (var pkInfo = ds.Tables[2])
                {
                    if (pkInfo.Rows.Count > 0 && pkInfo.Rows[0].ItemArray.Length > 3)
                    {
                        var primaryKeyName = pkInfo.Rows[0][3].ToString();
                        var pc = columns.First(c => c.Name.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase));
                        pc.IsPrimaryKey = true;
                    }
                }
            }
            return columns;
        }
        #endregion

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
