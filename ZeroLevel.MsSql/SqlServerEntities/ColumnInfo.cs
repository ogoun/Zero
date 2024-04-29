using System;

namespace ZeroLevel.MsSql
{
    public class ColumnInfo: IEquatable<ColumnInfo>
    {
        /// <summary>
        /// Наименование поля
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Тип поля в рамках базы данных
        /// </summary>
        public string DbType;
        /// <summary>
        /// Тип поля в рамках .NET
        /// </summary>
        public Type DotNetType;
        /// <summary>
        /// Указывает что поле является ключом таблицы
        /// </summary>
        public bool IsPrimaryKey;
        /// <summary>
        /// Указывает, разрешены ли значения NULL в поле
        /// </summary>
        public bool AllowNull;
        /// <summary>
        /// Размер в байтах (если применимо)
        /// </summary>
        public long Size;
        /// <summary>
        /// Указывает что поле является автоинкрементируемым
        /// </summary>
        public bool AutoInc;

        public ColumnInfo() { }

        public ColumnInfo(ColumnInfo other)
        {
            Name = other.Name;
            DbType = other.DbType;
            DotNetType = other.DotNetType;
            IsPrimaryKey = other.IsPrimaryKey;
            AllowNull = other.AllowNull;
            Size = other.Size;
            AutoInc = other.AutoInc;
        }

        public bool Equals(ColumnInfo other)
        {
            bool eq = true;
            eq &= AutoInc == other.AutoInc;
            eq &= Size == other.Size;
            eq &= AllowNull == other.AllowNull;
            eq &= IsPrimaryKey == other.IsPrimaryKey;
            eq &= String.Compare(DbType, other.DbType, StringComparison.OrdinalIgnoreCase) == 0;
            eq &= String.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase) == 0;
            eq &= DotNetType.Equals(other.DotNetType);
            return eq;
        }
    }
}
