using System.Data.SqlClient;

namespace ZeroLevel.MsSql
{
    public interface ISqlServerSpecification
    {
        string Query { get; } 
        SqlParameter[] Parameters { get; }
    }
}
