using System;
using System.Data;
using System.Data.Common;

namespace ZeroLevel.MsSql
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

        public SqlDbMapper(bool mapOnlyMarkedMembers) : base(typeof(T).Name)
        {
            _mapper = DbMapperFactory.Create<T>(mapOnlyMarkedMembers);
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
