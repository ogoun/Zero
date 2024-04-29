using System;
using System.Data;

namespace ZeroLevel.MsSql
{
    public class SqlDbMapper : BaseSqlDbMapper
    {
        protected readonly IDbMapper _mapper;

        protected override IDbMapper Mapper
        {
            get
            {
                return _mapper;
            }
        }

        public SqlDbMapper(Type entityType, bool mapOnlyMarkedMembers = true) : base(entityType.Name)
        {
            _mapper = DbMapperFactory.Create(entityType, mapOnlyMarkedMembers);
        }

        public object Deserialize(DataRow row)
        {
            return _mapper.Deserialize(row);
        }
    }
}
