using System.Data.SqlClient;

namespace ZeroLevel.SqlServer
{
    public interface ISqlServerSpecification
    {
        string Query { get; } 
        SqlParameter[] Parameters { get; }
    }
}
