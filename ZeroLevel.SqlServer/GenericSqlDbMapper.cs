using System;
using System.Data;
using System.Data.Common;

namespace ZeroLevel.SqlServer
{
    public class SqlDbMapper<T> : BaseSqlDbMapper
    {
        protected readonly IDbMapper<T> _mapper;

        protected override IDbMapper Mapper
        {
            get
            {
                return _mapper;
            }
        }

        public IDbField IdentityField
        {
            get
            {
                return _mapper.IdentityField;
            }
        }

        public SqlDbMapper(bool entity_is_poco) : base(typeof(T).Name)
        {
            _mapper = DbMapperFactory.Create<T>(entity_is_poco);
        }

        public T Deserialize(DataRow row)
        {
            return _mapper.Deserialize(row);
        }

        public T Deserialize(DbDataReader reader)
        {
            return _mapper.Deserialize(reader);
        }

        public void TraversalFields(Action<IDbField> callback)
        {
            _mapper.TraversalFields(callback);
        }
    }
}
