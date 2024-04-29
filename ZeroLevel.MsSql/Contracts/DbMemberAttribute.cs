using System;

namespace ZeroLevel.MsSql
{
    public class DbMemberAttribute : Attribute
    {
        #region Properties
        public bool AllowNull { get; }
        public bool AutoIncrement { get; }
        public bool IsIdentity { get; }
        public long Size { get; }
        #endregion

        #region Ctors
        public DbMemberAttribute(bool allowNull)
            : this(allowNull, -1, false, false) { }

        public DbMemberAttribute(bool allowNull, long size)
            : this(allowNull, size, false, false) { }

        public DbMemberAttribute(bool allowNull, bool isIdentity)
            : this(allowNull, -1, isIdentity, false) { }

        public DbMemberAttribute(bool allowNull, bool isIdentity, bool autoIncrement)
            : this(allowNull, -1, isIdentity, autoIncrement) { }

        public DbMemberAttribute(bool allowNull, long size, bool isIdentity)
            : this(allowNull, size, isIdentity, false) { }

        public DbMemberAttribute(bool allowNull, long size, bool isIdentity, bool autoIncrement)
        {
            AllowNull = allowNull;
            AutoIncrement = autoIncrement;
            IsIdentity = isIdentity;
            Size = size;
        }
        #endregion
    }
}
