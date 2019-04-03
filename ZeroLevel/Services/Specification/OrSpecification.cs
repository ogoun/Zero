using System.Runtime.Serialization;

namespace ZeroLevel.Specification
{
    [DataContract]
    public class OrSpecification<T> : BaseSpecification<T>
    {
        [DataMember]
        private ISpecification<T> leftSpecification;

        [DataMember]
        private ISpecification<T> rightSpecification;

        public OrSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            this.leftSpecification = left;
            this.rightSpecification = right;
        }

        public override bool IsSatisfiedBy(T o)
        {
            return this.leftSpecification.IsSatisfiedBy(o)
                || this.rightSpecification.IsSatisfiedBy(o);
        }
    }
}