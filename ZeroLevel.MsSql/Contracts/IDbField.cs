using System.Data;
using ZeroLevel.Services.ObjectMapping;

namespace ZeroLevel.MsSql
{
    public interface IDbField:
        IMemberInfo
    {
        bool AutoIncrement { get; }
        bool IsIdentity { get; }
        bool IsIndexed { get; }
        bool AllowNull { get; }
        long Size { get; }
        DbType DbType { get; }
    }
}
