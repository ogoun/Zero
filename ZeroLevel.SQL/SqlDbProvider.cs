using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace ZeroLevel.SqlServer
{
    public class DbReader
        : IDisposable
    {
        private readonly DbConnection _connection;
        private readonly IDbCommand _command;

        public DbReader(DbConnection connection, IDbCommand command)
        {
            _connection = connection;
            _command = command;
        }

        public IDataReader GetReader()
        {
            return _command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public void Dispose()
        {
            _command.Dispose();
            _connection.Close();
            _connection.Dispose();
        }
    }

    public class SqlDbProvider :
        IDbProvider
    {
        #region Fields
        private readonly SqlDbConnectionFactory _factory;
        private const int Timeout = 60000;

        public string ConnectionString
        {
            get { return _factory.ConnectionString; }
        }

        public string Server
        {
            get { return _factory.Server; }
        }

        public string Base
        {
            get { return _factory.Base; }
        }
        #endregion

        #region .Ctor
        /// <summary>
        /// Конструктор.
        /// </summary>
        public SqlDbProvider(SqlDbConnectionFactory factory)
        {
            _factory = factory;
        }
        #endregion

        #region ExecuteNonResult       
        public void ExecuteNonResult(IEnumerable<ZSqlCommand> commands)
        {
            using (DbConnection connection = _factory.CreateConnection())
            {
                foreach (var zcmd in commands)
                {
                    using (var cmd = CreateCommand(connection, zcmd.Query, zcmd.Parameters, Timeout))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                connection.Close();
            }
        }

        public int ExecuteNonResult(string query)
        {
            return ExecuteNonResult(query, null);
        }

        public int ExecuteNonResult(string query, DbParameter[] par)
        {
            using (DbConnection connection = _factory.CreateConnection())
            {
                try
                {
                    using (var cmd = CreateCommand(connection, query, par, Timeout))
                    {
                        return cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public T Insert<T>(string insert_query, SqlParameter[] par)
        {
            DbConnection connection = _factory.CreateConnection();
            try
            {
                using (var cmd = CreateCommand(connection, insert_query, par, Timeout))
                {
                    var result = cmd.ExecuteScalar();
                    return (T)result;
                }
            }
            finally
            {
                connection.Close();
                connection.Dispose();
            }
        }
        #endregion

        #region ExecuteQueryDataTable
        public DataTable ExecuteQueryDataTable(string query)
        {
            var ds = ExecuteQuerySqlDataSet(query);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            return null;
        }

        public DataTable ExecuteQueryDataTable(string query, DbParameter[] par)
        {
            var ds = ExecuteQuerySqlDataSet(query, par);
            if (ds != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            return null;
        }
        #endregion

        #region ExecuteQuerySqlDataSet
        public DataSet ExecuteQuerySqlDataSet(string query)
        {
            var ds = new DataSet("DataSet");
            using (var connection = _factory.CreateConnection())
            {
                using (var cmd = CreateCommand(connection, query, null, Timeout))
                {
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);
                    }
                }
                connection.Close();
            }
            return ds;
        }

        public DataSet ExecuteQuerySqlDataSet(string query, DbParameter[] par)
        {
            var ds = new DataSet("DataSet");
            using (var connection = _factory.CreateConnection())
            {
                using (var cmd = CreateCommand(connection, query, par, Timeout))
                {
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);
                    }
                }
                connection.Close();
            }
            return ds;
        }
        #endregion

        #region ExecuteScalar
        public object ExecuteScalar(string query)
        {
            using (var connection = _factory.CreateConnection())
            {
                try
                {
                    using (var cmd = CreateCommand(connection, query, null, Timeout))
                    {
                        return cmd.ExecuteScalar();
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public object ExecuteScalar(string query, DbParameter[] par)
        {
            using (var connection = _factory.CreateConnection())
            {
                try
                {
                    using (var cmd = CreateCommand(connection, query, par, Timeout))
                    {
                        return cmd.ExecuteScalar();
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        #endregion

        #region ExecuteStoredProcedure
        public int ExecProcedure(string name)
        {
            using (var connection = _factory.CreateConnection())
            {
                try
                {
                    using (var command = new SqlCommand(name, connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    })
                    {
                        command.CommandTimeout = 300000;
                        return command.ExecuteNonQuery();
                    }
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        #endregion

        public DbReader ExecuteReader(string query, DbParameter[] par)
        {
            var connection = _factory.CreateConnection();
            return new DbReader(connection, CreateCommand(connection, query, par, Timeout));

        }

        #region LazySelect
        public void LazySelect(string query, DbParameter[] par, Func<DbDataReader, bool> readHandler)
            => LazySelect(query, par, readHandler, Timeout);

        public void LazySelect(string query, DbParameter[] par, Func<DbDataReader, bool> readHandler, int timeout = Timeout)
        {
            using (var connection = _factory.CreateConnection())
            {
                using (var cmd = CreateCommand(connection, query, par, Timeout))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                if (false == readHandler(reader))
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error executing query {0}.", cmd.CommandText);
                        }
                        finally
                        {
                            // Always call Close when done reading.
                            reader.Close();
                        }
                    }
                }
            }
        }

        public void LazySelectWithParameters(string query, IEnumerable<KeyValuePair<string, object>> par, Func<DbDataReader, bool> readHandler, int timeout = Timeout)
        {
            using (var connection = _factory.CreateConnection())
            {
                using (var cmd = CreateCommand(connection, query, null, Timeout))
                {
                    if (par != null && par.Any())
                    {
                        foreach (var p in par)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }
                    }
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                if (false == readHandler(reader))
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error executing query {0}.", cmd.CommandText);
                        }
                        finally
                        {
                            // Always call Close when done reading.
                            reader.Close();
                        }
                    }
                }
            }
        }
        #endregion

        #region ExistsTable
        private const string QueryExistsTable = @"IF OBJECT_ID (N'[{0}]', N'U') IS NOT NULL SELECT 1 AS res ELSE SELECT 0 AS res";
        public bool ExistsTable(string tableName)
        {
            return Convert.ToInt32(ExecuteScalar(String.Format(QueryExistsTable, tableName))) == 1;
        }
        #endregion

        #region Commands
        private static SqlParameter[] ProcessParameters(DbParameter[] par)
        {
            if (par != null)
            {
                var result = new SqlParameter[par.Length];
                for (int i = 0; i < par.Length; i++)
                {
                    if (par[i] is SqlParameter)
                    {
                        result[i] = (SqlParameter)par[i];
                    }
                    else
                    {
                        result[i] = new SqlParameter(par[i].ParameterName,
                            par[i].Value ?? DBNull.Value);
                        result[i].Size = par[i].Size;
                    }
                }
                return result;
            }
            return new SqlParameter[0];
        }

        public static SqlCommand CreateCommand(DbConnection connection, string query, DbParameter[] parameters, int timeout)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            if (timeout > 0)
                command.CommandTimeout = timeout;
            if (parameters != null && parameters.Length > 0)
                command.Parameters.AddRange(ProcessParameters(parameters));
            return (SqlCommand)command;

        }
        #endregion

        #region SQL Server execute plan reset
        private const string CLEAN_PLAN_CACHEE_QUERY = "DBCC FREEPROCCACHE WITH NO_INFOMSGS;";
        /// <summary>
        /// Выполняет удаление всех элементов из кэша планов.
        /// Применимо для ускорения работы SQL Server, при очистке кэша создаются новые планы
        /// исполнения для новых значений запросов.
        /// </summary>
        public void CleanPlanCachee()
        {
            using (var connection = _factory.CreateConnection())
            {
                using (var cmd = CreateCommand(connection, CLEAN_PLAN_CACHEE_QUERY,
                    null, Timeout))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion

        #region Static methods        
        /// <summary>
        /// Создает базу данных
        /// </summary>
        /// <param name="server">Сервер</param>
        /// <param name="database">Название базы данных</param>
        public static void CreateDatabase(string server, string database, string login, string password)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new ArgumentException("Не указано имя сервера");
            }
            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentException("Не указано имя базы данных");
            }
            using (var connection = new SqlConnection(SqlDbConnectionFactory.BuildConnectionString(server, "master", login, password)))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = String.Format("CREATE DATABASE {0}", database);
                    command.ExecuteNonQuery();
                }
            }
        }
        /// <summary>
        /// Выполняет проверку существования базы данных с указанным именем
        /// </summary>
        public static bool CheckDatabaseExists(string serverName, string databaseName)
        {
            string sqlExistsDBQuery;
            bool result = false;
            try
            {
                using (var tmpConn = new SqlConnection(String.Format("server={0};Trusted_Connection=yes", serverName)))
                {
                    tmpConn.Open();
                    sqlExistsDBQuery = string.Format("SELECT database_id FROM sys.databases WHERE Name = '{0}'", databaseName);
                    using (SqlCommand sqlCmd = new SqlCommand(sqlExistsDBQuery, tmpConn))
                    {
                        object resultObj = sqlCmd.ExecuteScalar();
                        int databaseID = 0;
                        if (resultObj != null)
                        {
                            int.TryParse(resultObj.ToString(), out databaseID);
                        }
                        result = (databaseID > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Сбой при попытке подключения к серверу {0} и проверке наличия базы данных {1}",
                    serverName, databaseName);
                result = false;
            }
            return result;
        }
        /// <summary>
        /// Удаляет базу данных
        /// </summary>
        public static void DropDatabase(string server, string database, string login, string password)
        {
            if (string.IsNullOrEmpty(server))
            {
                throw new ArgumentException("Не указано имя сервера");
            }
            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentException("Не указано имя базы данных");
            }
            using (var connection = new SqlConnection(SqlDbConnectionFactory.BuildConnectionString(server, "master", login, password)))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = String.Format("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;\r\nDROP DATABASE [{1}]", database, database);
                    command.ExecuteNonQuery();
                }
            }
        }
        #endregion
    }
}
