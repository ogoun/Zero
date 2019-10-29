using System;
using System.Runtime.Serialization;
using ZeroLevel.Specification;

namespace ZeroLevel.SqlServer
{
    [DataContract]
    [Serializable]
    public class IdentitySpecification<T> : BaseSpecification<T>
        where T : IEntity
    {
        [DataMember]
        protected Guid _id;

        public IdentitySpecification(Guid id)
        {
            _id = id;
        }

        public override bool IsSatisfiedBy(T o)
        {
            return o.Id == _id;
        }

        public static ISpecification<T> Create(Guid id) { return new IdentitySpecification<T>(id); }
        public static ISpecification<T> Create(IEntity entity) { return new IdentitySpecification<T>(entity.Id); }
    }

    [DataContract]
    [Serializable]
    public class IdentitySpecification<T, TKey> : BaseSpecification<T>
    {
        [DataMember]
        private TKey _id;
        private readonly IDbMapper<T> _mapper;

        public IdentitySpecification(TKey id, bool mapOnlyMarkedMembers)
        {
            _id = id;
            _mapper = DbMapperFactory.Create<T>(mapOnlyMarkedMembers);
        }

        public override bool IsSatisfiedBy(T o)
        {
            return _mapper.Id(o).Equals(_id);
        }

        public static ISpecification<T> Create(TKey id, bool mapOnlyMarkedMembers) { return new IdentitySpecification<T, TKey>(id, mapOnlyMarkedMembers); }
    }
}
