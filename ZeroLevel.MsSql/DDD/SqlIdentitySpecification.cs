using System;
using System.Data.SqlClient;
using ZeroLevel.Specification;

namespace ZeroLevel.MsSql
{
    public class SqlIdentitySpecification<T> 
        : IdentitySpecification<T>, ISqlServerSpecification
        where T : IEntity
    {
        public SqlIdentitySpecification(Guid id) : base(id)
        {
        }

        public SqlParameter[] Parameters
        {
            get
            {
                return new SqlParameter[]
                {
                    new SqlParameter(DbMapperFactory.Create<T>().IdentityField.Name, _id)
                };
            }
        }

        public string Query
        {
            get
            {
                return string.Empty;
            }
        }

        public static ISpecification<Te> Create<Te>(Guid id) where Te: IEntity
        { return new SqlIdentitySpecification<Te>(id); }
    }
}
