using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace ZeroLevel.SqlServer
{
    public interface IDbProvider
    {
        bool ExistsTable(string tableName);
        DataTable ExecuteQueryDataTable(string query);
        DataTable ExecuteQueryDataTable(string query, DbParameter[] par);
        DataSet ExecuteQuerySqlDataSet(string query);
        DataSet ExecuteQuerySqlDataSet(string query, DbParameter[] par);
        object ExecuteScalar(string query);
        object ExecuteScalar(string query, DbParameter[] par);
        void ExecuteNonResult(IEnumerable<ZSqlCommand> commands);
        int ExecuteNonResult(string query);
        int ExecuteNonResult(string query, DbParameter[] par);
        void LazySelect(string query, DbParameter[] par, Func<DbDataReader, bool> readHandler);
    }
}
