using System.Data.Common;

namespace ZeroLevel.SqlServer
{
    public class ZSqlCommand
    {
        public string Query;
        public DbParameter[] Parameters;
    }
}