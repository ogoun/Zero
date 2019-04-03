using System.Runtime.Serialization;

namespace ZeroLevel.Specification
{
    [DataContract]
    public class TrueSpecification<T> : BaseSpecification<T>
    {
        public override bool IsSatisfiedBy(T o)
        {
            return true;
        }
    }
}