using System.Data.Common;

namespace ZeroLevel.MsSql
{
    public class ZSqlCommand
    {
        public string Query;
        public DbParameter[] Parameters;
    }
}