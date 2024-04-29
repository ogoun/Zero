using System;

namespace ZeroLevel.MsSql
{
    public sealed class SqlDbPrimaryKeyInfo : IEquatable<SqlDbPrimaryKeyInfo>
    {
        public string PrimaryKeyTable;
        public string PrimaryKeyColumn;

        public bool Equals(SqlDbPrimaryKeyInfo other)
        {
            return String.Compare(PrimaryKeyTable, other.PrimaryKeyTable, StringComparison.OrdinalIgnoreCase) == 0 &&
                String.Compare(PrimaryKeyColumn, other.PrimaryKeyColumn, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            return PrimaryKeyTable.GetHashCode() ^ PrimaryKeyColumn.GetHashCode();
        }
    }
}
