using System.Runtime.Serialization;

namespace ZeroLevel.Specification
{
    [DataContract]
    public class NotSpecification<T> : BaseSpecification<T>
    {
        [DataMember]
        private ISpecification<T> _specification;

        public NotSpecification(ISpecification<T> specification)
        {
            this._specification = specification;
        }

        public override bool IsSatisfiedBy(T o)
        {
            return !this._specification.IsSatisfiedBy(o);
        }
    }
}