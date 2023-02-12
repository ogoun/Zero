using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Permissions;

namespace ZeroLevel.SqlServer
{
    public sealed class SqlDbConnectionFactory
    {
        public string ConnectionString
        {
            get { return dbConnectionString.ConnectionString; }
        }

        public string Server
        {
            get { return dbConnectionString.DataSource; }
        }

        public string Base
        {
            get { return dbConnectionString.InitialCatalog; }
        }

        #region Поля
        private SqlConnectionStringBuilder dbConnectionString;
        /// <summary>
        /// Текущая строка подключения
        /// </summary>
        private readonly string _currentConnectionString = String.Empty;
        #endregion

        public SqlDbConnectionFactory(SqlConnectionStringBuilder builder)
        {
            _currentConnectionString = builder.ConnectionString;
            Initialize();
        }

        public SqlDbConnectionFactory(string connectionString)
        {
            _currentConnectionString = connectionString;
            Initialize();
        }

        public SqlDbConnectionFactory(string server, string database, string login, string password)
        {
            _currentConnectionString = BuildConnectionString(server, database, login, password);
            Initialize();
        }

        private void Initialize()
        {
            dbConnectionString = new SqlConnectionStringBuilder(_currentConnectionString);
            dbConnectionString.Pooling = true;
        }

        public SqlConnection CreateConnection()
        {
            try
            {
                var connection = new SqlConnection(ConnectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqlDbConnectionFactory.CreateConnection] {ConnectionString}");
                throw;
            }
        }

        #region Helpers

        #region Строки подключения
        /// <summary>
        /// Стандартное подключение
        /// </summary>
        private const string StandartConnectionString = "Server={0};Database={1};User ID={2};Password=\"{3}\";";
        /// <summary>
        /// Доверенное подключение
        /// </summary>
        private const string TrustedConnectionString = "Data Source={0};Initial Catalog={1};Integrated Security=SSPI;";
        #endregion

        internal static string BuildConnectionString(string server, string dataBase, string user, string pwd)
        {
            if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(pwd))
            {
                return String.Format(CultureInfo.CurrentCulture, TrustedConnectionString, server, dataBase);
            }
            else
            {
                return String.Format(CultureInfo.CurrentCulture, StandartConnectionString, server, dataBase, user, pwd);
            }
        }
        #endregion

        public override int GetHashCode()
        {
            return this.ConnectionString.GetHashCode();
        }
    }
}
