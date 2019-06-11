using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.SqlServer
{
    public abstract class TableInfo : IEquatable<TableInfo>
    {
        #region Private fields
        /// <summary>
        /// Имя таблицы
        /// </summary>
        protected readonly string _name;
        /// <summary>
        /// Поле-идентификатор
        /// </summary>
        private ColumnInfo _primaryKey;
        /// <summary>
        /// Все поля таблицы
        /// </summary>
        private readonly Dictionary<string, ColumnInfo> _columns = new Dictionary<string, ColumnInfo>();
        /// <summary>
        /// Индексы
        /// </summary>
        private readonly List<IndexInfo> _indexes = new List<IndexInfo>();
        #endregion

        #region Properties
        public ColumnInfo this[string columnName]
        {
            get
            {
                if (_columns.ContainsKey(columnName))
                {
                    return _columns[columnName];
                }
                return null;
            }
        }
        /// <summary>
        /// Первичный ключ
        /// </summary>
        public ColumnInfo PrimaryKey
        {
            get { return _primaryKey; }
        }
        /// <summary>
        /// Имя таблицы
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        /// <summary>
        /// Индексы
        /// </summary>
        public List<IndexInfo> Indexes
        {
            get
            {
                return _indexes;
            }
        }
        /// <summary>
        /// Поля таблицы
        /// </summary>
        public IEnumerable<ColumnInfo> Columns
        {
            get
            {
                return _columns.Values;
            }
        }
        #endregion

        #region Ctor
        /// <summary>
        /// Конструктор по-умолчанию
        /// </summary>
        /// <param name="name"></param>
        public TableInfo(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Конструктор по-умолчанию
        /// </summary>
        /// <param name="name"></param>
        public TableInfo(TableInfo other)
        {
            _name = other._name;
            _columns = new Dictionary<string, ColumnInfo>(other._columns);
            _indexes = new List<IndexInfo>(other._indexes);
            _primaryKey = other._primaryKey;
        }
        #endregion

        #region IEquatable
        public override bool Equals(object obj)
        {
            return this.Equals(obj as TableInfo);
        }
        /// <summary>
        /// Сравнение с другой таблицей
        /// </summary>
        public bool Equals(TableInfo other)
        {
            if (other == null || _name.Equals(other._name, StringComparison.OrdinalIgnoreCase) == false)
                return false;
            if (false == Columns.NoOrderingEquals(other.Columns))
            {
                return false;
            }
            return true;
        }
        #endregion

        #region Abstract
        protected abstract IEnumerable<IndexInfo> GetIndexes(IDbProvider db);
        protected abstract IEnumerable<ColumnInfo> GetColumns(IDbProvider db);
        #endregion

        #region Fill table info
        public void AppendNewColumn(ColumnInfo column)
        {
            _columns.Add(column.Name, column);
            if (column.IsPrimaryKey)
                _primaryKey = column;
        }
        /// <summary>
        /// Заполнение данных о таблице
        /// </summary>
        /// <param name="db">Подключение к базе данных</param>
        public void FillTableInfo(IDbProvider db)
        {
            foreach (var column in GetColumns(db))
            {
                _columns.Add(column.Name, column);
                if (column.IsPrimaryKey)
                {
                    _primaryKey = column;
                }
            }
            foreach (var index in GetIndexes(db))
            {
                _indexes.Add(index);
            }
        }
        #endregion

        /// <summary>
        /// Проверка наличия поля
        /// </summary>
        public bool ContainsColumns(ColumnInfo column)
        {
            return Columns.Any(r => r.Equals(column));
        }
        /// <summary>
        /// Проверка наличия поля
        /// </summary>
        public bool ContainsColumns(string columnName)
        {
            return Columns.Any(r => r.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }
    }
}
