using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace ZeroLevel.MsSql
{
    public sealed class SqlDbInfo
    {
        #region Ctor
        public SqlDbInfo(SqlDbProvider provider)
        {
            _provider = provider;
        }
        #endregion

        private static string FixTableName(string tableName)
        {
            return tableName.Trim().ToLower();
        }

        public void CollectDatabaseInfo(bool tables, bool views, bool storedProcedures)
        {
            if (tables)
                CollectTableInformation();
        }

        public SqlDbTableInfo this[string tableName]
        {
            get
            {
                tableName = FixTableName(tableName);
                if (_tables.ContainsKey(tableName))
                {
                    return _tables[tableName];
                }
                throw new KeyNotFoundException("Таблица " + tableName + " отсутствует в базе " + _provider.Server + "\\" + _provider.Base);
            }
        }

        #region Private Fields
        private readonly SqlDbProvider _provider;
        private readonly Dictionary<string, SqlDbTableInfo> _tables = new Dictionary<string, SqlDbTableInfo>();
        private readonly List<SqlDbForeignKeyInfo> _foreignKeys = new List<SqlDbForeignKeyInfo>();
        private readonly List<SqlDbPrimaryKeyInfo> _primaryKeys = new List<SqlDbPrimaryKeyInfo>();
        private readonly List<SqlDbObjectInfo> _storedProcedures = new List<SqlDbObjectInfo>();
        private readonly List<SqlDbObjectInfo> _views = new List<SqlDbObjectInfo>();
        #endregion

        #region Public database info
        public IEnumerable<string> Tables
        {
            get
            {
                return _tables.Keys;
            }
        }

        public IEnumerable<SqlDbTableInfo> TablesInfo
        {
            get
            {
                return _tables.Values;
            }
        }

        public IEnumerable<SqlDbPrimaryKeyInfo> PrimaryKeys
        {
            get
            {
                return _primaryKeys;
            }
        }

        public IEnumerable<SqlDbForeignKeyInfo> ForeignKeys
        {
            get
            {
                return _foreignKeys;
            }
        }

        public IEnumerable<SqlDbObjectInfo> StoredProcedures
        {
            get
            {
                return _storedProcedures;
            }
        }

        public IEnumerable<SqlDbObjectInfo> Views
        {
            get
            {
                return _views;
            }
        }
        #endregion

        #region Public methods
        public bool ContainTable(string tableName)
        {
            tableName = FixTableName(tableName);
            return _tables.ContainsKey(tableName);
        }

        public bool ContainPrimaryKey(SqlDbPrimaryKeyInfo pk)
        {
            return _primaryKeys.Contains(pk);
        }

        public bool ContainForeignKey(SqlDbForeignKeyInfo fk)
        {
            return _foreignKeys.Contains(fk);
        }

        public SqlDbTableInfo TableInfo(string tableName)
        {
            tableName = FixTableName(tableName);
            if (ContainTable(tableName))
            {
                return _tables[tableName];
            }
            return null;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Сбор информации о таблицах, перчиных и внешних ключах
        /// </summary>
        private void CollectTableInformation()
        {
            // Таблицы
            foreach (string table in GetTables())
            {
                SqlDbTableInfo info = GetTableInfo(table);
                if (info != null)
                {
                    string tableName = FixTableName(info.Name);
                    _tables.Add(tableName, info);
                    if (info.PrimaryKey != null)
                    {
                        _primaryKeys.Add(new SqlDbPrimaryKeyInfo { PrimaryKeyTable = tableName, PrimaryKeyColumn = info.PrimaryKey.Name });
                    }
                }
            }
            // Внешние ключи
            DataSet fkSet = _provider.ExecuteQuerySqlDataSet(SqlDbForeignKeyInfo.ForeignKeySelectQuery);
            if (fkSet != null && fkSet.Tables.Count > 0)
            {
                foreach (DataRow row in fkSet.Tables[0].Rows)
                {
                    _foreignKeys.Add(new SqlDbForeignKeyInfo
                    {
                        ForeignKeyName = Convert.ToString(row["Constraint_Name"], CultureInfo.CurrentCulture),
                        ForeignKeyTable = FixTableName(Convert.ToString(row["K_Table"], CultureInfo.CurrentCulture)),
                        ForeignKeyColumn = Convert.ToString(row["FK_Column"], CultureInfo.CurrentCulture),
                        PrimaryKeyTable = FixTableName(Convert.ToString(row["PK_Table"], CultureInfo.CurrentCulture)),
                        PrimaryKeyColumn = Convert.ToString(row["PK_Column"], CultureInfo.CurrentCulture)
                    });
                }
            }
        }
        #region Private
        /// <summary>
        /// Получение списка таблиц из базы данных
        /// </summary>
        private List<string> GetTables()
        {
            var tables = new List<string>();
            using (DataSet ds = _provider.ExecuteQuerySqlDataSet("exec sp_tables"))
            {
                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        if (String.Equals(row.ItemArray[3].ToString(), "TABLE", StringComparison.OrdinalIgnoreCase) &&
                            (false == String.Equals(row.ItemArray[1].ToString(), "sys", StringComparison.OrdinalIgnoreCase)))
                            tables.Add(FixTableName(row.ItemArray[2].ToString()));
                    }
                }
            }
            return tables;
        }
        /// <summary>
        /// Получение информации о таблице по ее имени
        /// </summary>
        public SqlDbTableInfo GetTableInfo(string table)
        {
            if (String.IsNullOrEmpty(table))
            {
                throw new ArgumentNullException("table");
            }
            var info = new SqlDbTableInfo(FixTableName(table));
            info.FillTableInfo(_provider);
            return info;
        }
        #endregion

        #endregion
    }
}
