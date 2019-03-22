using System.Runtime.Serialization;

namespace ZeroLevel.Specification
{
    [DataContract]
    public class OrSpecification<T> : BaseSpecification<T>
    {
        [DataMember]
        ISpecification<T> leftSpecification;
        [DataMember]
        ISpecification<T> rightSpecification;

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
