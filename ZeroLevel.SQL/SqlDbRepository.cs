using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using ZeroLevel.Specification;

namespace ZeroLevel.SqlServer
{
    public class SqlDbRepository<T>
    {
        #region Fields
        private readonly SqlDbMapper<T> _mapper;
        private readonly SqlDbProvider _dbProvider;
        #endregion

        #region Ctors
        public SqlDbRepository(SqlDbConnectionFactory connectionFactory, bool entity_is_poco = false)
        {
            _mapper = new SqlDbMapper<T>(entity_is_poco);
            _dbProvider = new SqlDbProvider(connectionFactory);
            if (false == SqlDbProvider.CheckDatabaseExists(connectionFactory.Server, connectionFactory.Base))
            {
                SqlDbProvider.CreateDatabase(connectionFactory.Server, connectionFactory.Base, null, null);
                Thread.Sleep(5000);
            }
            VerifyDb();
            Prebuilt();
        }

        public SqlDbRepository(string connectionString)
            : this(new SqlDbConnectionFactory(connectionString))
        {
        }

        public SqlDbRepository(string server, string database)
            : this(new SqlDbConnectionFactory(server, database, null, null))
        {
        }
        #endregion

        #region Simple queries
        public IEnumerable<T> Get()
        {
            using (var table = _dbProvider.ExecuteQueryDataTable(_getAllQuery))
            {
                return ConvertToEntitySet(table);
            }
        }

        public T GetById<TId>(TId id)
        {
            if (null == id)
                throw new ArgumentNullException(nameof(id));
            using (var table = _dbProvider.ExecuteQueryDataTable(_getByIdQuery, new SqlParameter[]
            {
                new SqlParameter(_mapper.IdentityName, id)
            }))
            {
                if (null != table && table.Rows.Count > 0)
                {
                    return _mapper.Deserialize(table.Rows[0]);
                }
            }
            throw new KeyNotFoundException(string.Format("Not found db record by identity field '{0}' with value {1}",
                _mapper.IdentityName, id));
        }

        public IEnumerable<T> Get<TField>(string fieldName, TField value)
        {
            if (null == value)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentNullException(nameof(fieldName));
            var query = string.Format(_getByFieldNameQuery, fieldName, fieldName);
            using (var table = _dbProvider.ExecuteQueryDataTable(query, new SqlParameter[]
            {
                new SqlParameter(fieldName, value)
            }))
            {
                return ConvertToEntitySet(table);
            }
        }

        public long Count()
        {
            object count = _dbProvider.ExecuteScalar(_countQuery);
            if (null == count)
                throw new InvalidOperationException(String.Format("Fault execute count query {0}", _countQuery));
            return Convert.ToInt64(count);
        }

        public long Count<TField>(string fieldName, TField value)
        {
            if (null == value)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentNullException(nameof(fieldName));
            var query = string.Format(_countByFieldNameQuery, fieldName, fieldName);
            object count = _dbProvider.ExecuteScalar(query, new SqlParameter[]
            {
                new SqlParameter(fieldName, value)
            });
            if (null == count) throw new InvalidOperationException(String.Format("Fault execute count query {0}", _countQuery));
            return Convert.ToInt64(count);
        }

        public bool Contains(T entity)
        {
            if (null == entity)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            var count = _dbProvider.ExecuteScalar(_containsQuery, _mapper.CreateSqlDbParameters(entity));
            if (null == count) throw new InvalidOperationException(String.Format("Fault execute query {0}", _containsQuery));
            return Convert.ToInt64(count) > 0;
        }

        public bool ContainsId<TId>(TId id)
        {
            return Count<TId>(_mapper.IdentityName, id) > 0;
        }

        public bool Contains<TField>(string fieldName, TField value)
        {
            return Count<TField>(fieldName, value) > 0;
        }

        public void Update(T entity)
        {
            if (null == entity)
                throw new ArgumentNullException(nameof(entity));
            _dbProvider.ExecuteNonResult(_updateQuery, _mapper.CreateSqlDbParameters(entity));
        }

        public void Insert(T entity)
        {
            if (null == entity)
                throw new ArgumentNullException(nameof(entity));
            _dbProvider.ExecuteNonResult(_insertQuery, _mapper.CreateSqlDbParameters(entity));
        }

        public void Insert(IEnumerable<T> entities)
        {
            if (null == entities)
                throw new ArgumentNullException(nameof(entities));
            var commandList =
                entities.
                Select(e => new ZSqlCommand { Query = _insertQuery, Parameters = _mapper.CreateSqlDbParameters(e) });
            _dbProvider.ExecuteNonResult(commandList);
        }

        public void Update(IEnumerable<T> entities)
        {
            if (null == entities)
                throw new ArgumentNullException(nameof(entities));
            var commandList =
                entities.
                Select(e => new ZSqlCommand { Query = _updateQuery, Parameters = _mapper.CreateSqlDbParameters(e) });
            _dbProvider.ExecuteNonResult(commandList);
        }

        public void Remove(T entity)
        {
            if (null == entity)
                throw new ArgumentNullException(nameof(entity));
            _dbProvider.ExecuteNonResult(_removeByIdQuery, new SqlParameter[]
            {
                new SqlParameter(_mapper.IdentityName, _mapper.GetIdentity(entity))
            });
        }

        public void RemoveById<TId>(TId id)
        {
            if (null == id)
                throw new ArgumentNullException(nameof(id));
            _dbProvider.ExecuteNonResult(_removeByIdQuery, new SqlParameter[]
            {
                new SqlParameter(_mapper.IdentityName, id)
            });
        }

        public void Remove<TField>(string fieldName, TField value)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentNullException(nameof(fieldName));
            if (null == value)
                throw new ArgumentNullException(nameof(value));
            var query = string.Format(_removeByFieldQuery, fieldName, fieldName);
            _dbProvider.ExecuteNonResult(query, new SqlParameter[]
            {
                new SqlParameter(fieldName, value)
            });
        }
        #endregion

        #region Specification queries
        public T SingleOrDefault(ISpecification<T> specification)
        {
            if (null == specification)
                throw new ArgumentNullException(nameof(specification));
            ISqlServerSpecification sqlSpecification;
            if (true == TryGetWhere(specification, out sqlSpecification))
            {
                string query = null;
                var where = BuildWherePart(sqlSpecification);
                if (false == string.IsNullOrWhiteSpace(where))
                {
                    query = _getTopOneQuery + " WHERE " + where;
                    using (var table = _dbProvider.ExecuteQueryDataTable(query, sqlSpecification.Parameters))
                    {
                        if (null != table && table.Rows.Count > 0)
                        {
                            return _mapper.Deserialize(table.Rows[0]);
                        }
                        return default(T);
                    }
                }
            }
            // No sql specification
            T result = default(T);
            _dbProvider.LazySelect(_getAllQuery, null, reader =>
            {
                var entity = _mapper.Deserialize(reader);
                if (specification.IsSatisfiedBy(entity))
                {
                    result = entity;
                    return false;
                }
                return true;
            });
            return result;
        }

        public IEnumerable<T> Get(ISpecification<T> specification)
        {
            if (null == specification)
                throw new ArgumentNullException(nameof(specification));
            ISqlServerSpecification sqlSpecification;
            if (true == TryGetWhere(specification, out sqlSpecification))
            {
                string query = null;
                var where = BuildWherePart(sqlSpecification);
                if (false == string.IsNullOrWhiteSpace(where))
                {
                    query = _getAllQuery + " WHERE " + where;
                    using (var ds = _dbProvider.ExecuteQuerySqlDataSet(query, sqlSpecification.Parameters))
                    {
                        return ConvertToEntitySet(ds.Tables[0]);
                    }
                }
            }
            // No sql specification
            var result = new List<T>();
            _dbProvider.LazySelect(_getAllQuery, null, reader =>
            {
                var entity = _mapper.Deserialize(reader);
                if (specification.IsSatisfiedBy(entity))
                {
                    result.Add(entity);
                }
                return true;
            });
            return result;
        }

        public bool Contains(ISpecification<T> specification)
        {
            if (null == specification)
                throw new ArgumentNullException(nameof(specification));
            ISqlServerSpecification sqlSpecification;
            if (true == TryGetWhere(specification, out sqlSpecification))
            {
                var where = BuildWherePart(sqlSpecification);
                if (false == string.IsNullOrWhiteSpace(where))
                {
                    string query = String.Format("SELECT COUNT(*) FROM [{0}] WHERE {1}", _mapper.TableName, where);
                    object count = _dbProvider.ExecuteScalar(query, sqlSpecification.Parameters);
                    if (null == count) throw new InvalidOperationException(String.Format("Fault execute query {0}", query));
                    return Convert.ToInt64(count) > 0;
                }
            }
            bool result = false;
            _dbProvider.LazySelect(_getAllQuery, null, reader =>
            {
                var entity = _mapper.Deserialize(reader);
                if (specification.IsSatisfiedBy(entity))
                {
                    result = true;
                    return false;
                }
                return true;
            });
            return result;
        }

        public long Count(ISpecification<T> specification)
        {
            if (null == specification)
                throw new ArgumentNullException(nameof(specification));
            ISqlServerSpecification sqlSpecification;
            if (true == TryGetWhere(specification, out sqlSpecification))
            {
                var where = BuildWherePart(sqlSpecification);
                object count = null;
                if (false == string.IsNullOrWhiteSpace(where))
                {
                    count = _dbProvider.ExecuteScalar(String.Format("{0} WHERE {1}", _countQuery, where), sqlSpecification.Parameters);
                }
                if (null == count) throw new InvalidOperationException("Fault execute query");
                if (DBNull.Value == count) return 0;
                return Convert.ToInt64(count);
            }
            long result = 0;
            _dbProvider.LazySelect(_getAllQuery, null, reader =>
            {
                var entity = _mapper.Deserialize(reader);
                if (specification.IsSatisfiedBy(entity))
                {
                    result++;
                }
                return true;
            });
            return result;
        }

        public void Remove(ISpecification<T> specification)
        {
            if (null == specification)
                throw new ArgumentNullException(nameof(specification));
            ISqlServerSpecification sqlSpecification;
            if (true == TryGetWhere(specification, out sqlSpecification))
            {
                var where = BuildWherePart(sqlSpecification);
                if (false == string.IsNullOrWhiteSpace(where))
                {
                    string query = string.Format("DELETE FROM [{0}] WHERE {1}", _mapper.TableName, where);
                    _dbProvider.ExecuteNonResult(query, sqlSpecification.Parameters);
                    return;
                }
            }
            _dbProvider.LazySelect(_getAllQuery, null, reader =>
            {
                var entity = _mapper.Deserialize(reader);
                if (specification.IsSatisfiedBy(entity))
                {
                    Remove(entity);
                }
                return true;
            });
        }
        #endregion

        #region Helpers
        private void VerifyDb()
        {
            if (false == _dbProvider.ExistsTable(_mapper.TableName))
            {
                _dbProvider.ExecuteNonResult(_mapper.GetCreateQuery());
            }
            _mapper.TraversalFields(f =>
            {
                if (f.IsIndexed)
                {
                    var existsQuery = _mapper.GetIndexExistsQuery(f);
                    if ((int)_dbProvider.ExecuteScalar(existsQuery) == 0)
                    {
                        var createQuery = _mapper.GetCreateIndexQuery(f);
                        _dbProvider.ExecuteNonResult(createQuery);
                    }
                }
            });
        }

        private IEnumerable<T> ConvertToEntitySet(DataTable _dt)
        {
            var result = new List<T>();
            _dt.Do(dt =>
            {
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        result.Add(_mapper.Deserialize(row));
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidCastException("Repository convert entity from db record to object fault", ex);
                    }
                }
            });
            return result;
        }

        private bool TryGetWhere(ISpecification<T> originalSpecifiaction, out ISqlServerSpecification sqlSpecification)
        {
            sqlSpecification = (originalSpecifiaction as ISqlServerSpecification);
            return sqlSpecification != null;
        }

        private string BuildWherePart(ISqlServerSpecification specification)
        {
            if (false == string.IsNullOrWhiteSpace(specification.Query))
                return specification.Query;
            else if (specification.Parameters != null)
                return string.Join(" AND ", specification.Parameters.
                Select(p => string.Format("[{0}] = @{1}", p.ParameterName, p.ParameterName)));
            return null;
        }
        #endregion

        #region Prebuild queries
        private string _insertQuery;
        private string _updateQuery;

        private string _getTopOneQuery;
        private string _getAllQuery;
        private string _getByIdQuery;
        private string _getByFieldNameQuery;

        private string _countQuery;
        private string _countByFieldNameQuery;

        private string _containsQuery;

        private string _removeByFieldQuery;
        private string _removeByIdQuery;

        private void Prebuilt()
        {
            _insertQuery = BuildInsertQuery();
            _updateQuery = BuildUpdateQuery();

            _getAllQuery = string.Format("SELECT * FROM [{0}]", _mapper.TableName);
            _getTopOneQuery = string.Format("SELECT TOP (1) * FROM [{0}]", _mapper.TableName);
            _getByIdQuery = string.Format("SELECT * FROM [{0}] WHERE {1}=@{2}",
                _mapper.TableName, _mapper.IdentityName, _mapper.IdentityName);
            _getByFieldNameQuery = _getAllQuery + " WHERE {0}=@{1}";
            _countQuery = string.Format("SELECT COUNT(*) FROM [{0}]", _mapper.TableName);
            _countByFieldNameQuery = _countQuery + " WHERE {0}=@{1}";
            _containsQuery = BuildContainsQuery();
            _removeByIdQuery = String.Format("DELETE FROM [{0}] WHERE [{1}] = @{2}",
                            _mapper.TableName, _mapper.IdentityName, _mapper.IdentityName);
            _removeByFieldQuery = String.Format("DELETE FROM [{0}] WHERE ", _mapper.TableName) + " [{0}] = @{1}";
        }

        private string BuildContainsQuery()
        {
            var query = new StringBuilder(_countQuery);
            query.Append(" WHERE ");
            _mapper.TraversalFields(f =>
            {
                query.AppendFormat("[{0}] = @{1} AND ", f.Name, f.Name);
            });
            if (StringExtensions.EndsWith(query, "AND ")) query.Remove(query.Length - 4, 4);
            if (StringExtensions.EndsWith(query, "WHERE ")) query.Remove(query.Length - 6, 6);
            return query.ToString();
        }

        private string BuildInsertQuery()
        {
            var query = new StringBuilder();
            query.AppendFormat("INSERT INTO [{0}](", _mapper.TableName);
            var values = new StringBuilder(" VALUES(");
            _mapper.TraversalFields(f =>
            {
                if (f.AutoIncrement == false)
                {
                    query.Append("[" + f.Name + "],");
                    values.Append("@" + f.Name + ",");
                }
            });
            query.Remove(query.Length - 1, 1);
            query.Append(")");
            query.AppendFormat(" OUTPUT INSERTED.{0} ", _mapper.IdentityName);
            values.Remove(values.Length - 1, 1);
            values.Append(")");
            query.Append(values);
            return query.ToString();
        }

        private string BuildUpdateQuery()
        {
            var query = new StringBuilder();
            query.AppendFormat("UPDATE[{0}] SET ", _mapper.TableName);
            _mapper.TraversalFields(f =>
            {
                if (f.IsIdentity == false && f.AutoIncrement == false)
                    query.Append("[" + f.Name + "] = @" + f.Name + ",");
            });
            query.Remove(query.Length - 1, 1);
            query.AppendFormat(" OUTPUT INSERTED.{0} WHERE [{1}] = @{2}", _mapper.IdentityName, _mapper.IdentityName, _mapper.IdentityName);
            return query.ToString();
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
        }
        #endregion

        public void Execute(string query)
        {
            _dbProvider.ExecuteNonResult(query);
        }
    }
}
