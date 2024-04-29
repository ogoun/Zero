using System;

namespace ZeroLevel.MsSql
{
    public sealed class SqlDbForeignKeyInfo : IEquatable<SqlDbForeignKeyInfo>
    {
        public const string ForeignKeySelectQuery = "SELECT K_Table = FK.TABLE_NAME, FK_Column = CU.COLUMN_NAME, PK_Table = PK.TABLE_NAME, PK_Column = PT.COLUMN_NAME, Constraint_Name = C.CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS FK ON C.CONSTRAINT_NAME = FK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS PK ON C.UNIQUE_CONSTRAINT_NAME = PK.CONSTRAINT_NAME INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON C.CONSTRAINT_NAME = CU.CONSTRAINT_NAME INNER JOIN ( SELECT i1.TABLE_NAME, i2.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1 INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE i2 ON i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME WHERE i1.CONSTRAINT_TYPE = 'PRIMARY KEY' ) PT ON PT.TABLE_NAME = PK.TABLE_NAME";

        public string ForeignKeyName;
        public string ForeignKeyTable;
        public string PrimaryKeyTable;
        public string ForeignKeyColumn;
        public string PrimaryKeyColumn;

        public bool Equals(SqlDbForeignKeyInfo other)
        {
            return String.Compare(ForeignKeyName, other.ForeignKeyName, StringComparison.OrdinalIgnoreCase) == 0 &&
                String.Compare(ForeignKeyTable, other.ForeignKeyTable, StringComparison.OrdinalIgnoreCase) == 0 &&
                String.Compare(PrimaryKeyTable, other.PrimaryKeyTable, StringComparison.OrdinalIgnoreCase) == 0 &&
                String.Compare(ForeignKeyColumn, other.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase) == 0 &&
                String.Compare(PrimaryKeyColumn, other.PrimaryKeyColumn, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            return ForeignKeyName.GetHashCode() ^
                ForeignKeyTable.GetHashCode() ^
                PrimaryKeyTable.GetHashCode() ^
                ForeignKeyColumn.GetHashCode() ^
                PrimaryKeyColumn.GetHashCode();
        }
    }
}
