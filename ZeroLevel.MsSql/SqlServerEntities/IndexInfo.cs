using System;
using System.Collections.Generic;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.MsSql
{
    public class IndexInfo : IEquatable<IndexInfo>
    {
        public string Name;
        public List<string> Columns = new List<string>();
        public bool IsUnique;
        public bool IsPrimaryKey;

        public bool Equals(IndexInfo other)
        {
            bool eq = true;
            eq &= String.Compare(Name, other.Name, StringComparison.Ordinal) == 0;
            eq &= Columns.NoOrderingEquals(other.Columns);
            eq &= IsUnique == other.IsUnique;
            eq &= IsPrimaryKey == other.IsPrimaryKey;
            return eq;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
